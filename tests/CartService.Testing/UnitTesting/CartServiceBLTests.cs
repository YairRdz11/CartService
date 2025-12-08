using CartService.BLL.Classes;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Interfaces.BLL;
using CartService.Transversal.Interfaces.DAL;
using Common.Utilities.Classes.Exceptions;
using Moq;
using Xunit;

namespace CartService.Testing.UnitTesting
{
    public class CartServiceBLTests
    {
        private readonly Mock<ICartRepository> _cartRepositoryMock;
        private readonly ICartService _cartService;
        
        public CartServiceBLTests()
        {
            _cartRepositoryMock = new Mock<ICartRepository>();
            _cartService = new CartServiceBL(_cartRepositoryMock.Object);
        }

        private static CartItemDTO NewItem(decimal price =10m, int qty =1) => new CartItemDTO
        {
            ProductId = Guid.NewGuid(),
            Name = "TestItem",
            Price = price,
            Quantity = qty,
            ImageUrl = null
        };

        [Fact]
        public void AddItemToCart_NewCart_CreatesAndAddsItem()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var item = NewItem();
            // Cart does NOT exist -> Build path
            _cartRepositoryMock
                .Setup(r => r.CartExists(cartId))
                .Returns(false);
            _cartRepositoryMock
                .Setup(r => r.SaveCart(It.IsAny<CartDTO>()))
                .Returns((CartDTO c) => c);

            // Act
            var result = _cartService.AddItemToCart(cartId, item);

            // Assert
            Assert.Equal(cartId, result.Id); // Build should set Id
            Assert.Single(result.Items);
            Assert.Equal(item.ProductId, result.Items[0].ProductId);
            _cartRepositoryMock.Verify(r => r.CartExists(cartId), Times.Once);
            _cartRepositoryMock.Verify(r => r.SaveCart(It.Is<CartDTO>(c => c.Id == cartId && c.Items.Count ==1)), Times.Once);
            _cartRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddItemToCart_ExistingCart_AppendsItem()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var first = NewItem();
            var second = NewItem();
            var cart = new CartDTO { Id = cartId, Items = new List<CartItemDTO>() { first} };

            _cartRepositoryMock
                .Setup(r => r.CartExists(cartId)) 
                .Returns(true);
            _cartRepositoryMock
                .Setup(r => r.GetCartById(cartId))
                .Returns(cart);
            _cartRepositoryMock
                .Setup(r => r.SaveCart(It.IsAny<CartDTO>()))
                .Returns((CartDTO c) => c);

            // Act
            var updated = _cartService.AddItemToCart(cartId, second);

            // Assert
            Assert.Equal(2, updated.Items.Count);
            Assert.Contains(updated.Items, i => i.ProductId == first.ProductId);
            Assert.Contains(updated.Items, i => i.ProductId == second.ProductId);
            _cartRepositoryMock.Verify(r => r.GetCartById(cartId), Times.Once);
            _cartRepositoryMock.Verify(r => r.SaveCart(It.Is<CartDTO>(c => c.Items.Count ==2)), Times.Once);
        }

        [Fact]
        public void AddItemToCart_MultipleItems_TotalUpdatesCorrectly()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var first = NewItem(price:5m, qty:2); //10
            var second = NewItem(price:3m, qty:4); //12
            var third = NewItem(price:2m, qty:5); //10
            var cart = new CartDTO { Id = cartId, Items = new List<CartItemDTO> { first } };

            _cartRepositoryMock.Setup(r => r.CartExists(cartId)).Returns(true);
            _cartRepositoryMock.SetupSequence(r => r.GetCartById(cartId))
                .Returns(cart) // first call for adding second item
                .Returns(cart); // second call for adding third item
            _cartRepositoryMock.Setup(r => r.SaveCart(It.IsAny<CartDTO>())).Returns<CartDTO>(c => c);

            // Act
            _cartService.AddItemToCart(cartId, second);
            _cartService.AddItemToCart(cartId, third);

            // Assert
            Assert.Equal(3, cart.Items.Count);
            Assert.Equal(10m +12m +10m, cart.Total);
        }

        [Fact]
        public void GetCartById_ReturnsCart()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var item = NewItem();
            _cartRepositoryMock
                .Setup(r => r.GetCartById(cartId))
                .Returns(new CartDTO { Id = cartId, Items = new List<CartItemDTO>() { item } });

            // Act
            var fetched = _cartService.GetCartById(cartId);

            Assert.Equal(cartId, fetched.Id);
            Assert.Single(fetched.Items);
        }

        [Fact]
        public void GetCartById_NotFound_PropagatesException()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            _cartRepositoryMock
                .Setup(r => r.GetCartById(cartId))
                .Throws(new NotFoundException("Cart", cartId));

            // Act & Assert
            Assert.Throws<NotFoundException>(() => _cartService.GetCartById(cartId));
        }

        [Fact]
        public void RemoveItemFromCart_RemovesItem_AndCallsSave()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var item1 = NewItem();
            var item2 = NewItem();
            var cart = new CartDTO { Id = cartId, Items = new List<CartItemDTO> { item1, item2 } };
            _cartRepositoryMock.Setup(r => r.GetCartById(cartId)).Returns(cart);
            _cartRepositoryMock.Setup(r => r.SaveCart(It.IsAny<CartDTO>())).Returns<CartDTO>(c => c);

            // Act
            _cartService.RemoveItemFromCart(cartId, item1.ProductId);

            // Assert
            Assert.Single(cart.Items);
            Assert.DoesNotContain(cart.Items, i => i.ProductId == item1.ProductId);
            _cartRepositoryMock.Verify(r => r.SaveCart(It.Is<CartDTO>(c => c.Items.Count ==1)), Times.Once);
        }

        [Fact]
        public void RemoveItemFromCart_ItemNotFound_Throws()
        {
            // Arrange
            var cartId = Guid.NewGuid();
            var item = NewItem();
            var missingId = Guid.NewGuid();
            var cart = new CartDTO { Id = cartId, Items = new List<CartItemDTO> { item } };
            _cartRepositoryMock.Setup(r => r.GetCartById(cartId)).Returns(cart);

            // Act & Assert
            Assert.Throws<NotFoundException>(() => _cartService.RemoveItemFromCart(cartId, missingId));
            _cartRepositoryMock.Verify(r => r.SaveCart(It.IsAny<CartDTO>()), Times.Never);
        }
    }
}
