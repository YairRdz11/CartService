using System;
using System.IO;
using LiteDB;
using Xunit;
using CartService.DAL.Classes;
using CartService.DAL.Classes.Entities;
using CartService.Transversal.Classes.DTOs;
using Common.Utilities.Classes.Exceptions;

namespace CartService.Testing.UnitTesting
{
    public class CartRepositoryTests : IDisposable
    {
        private readonly LiteDatabase _db;
        private readonly CartRepository _repo;

        public CartRepositoryTests()
        {
            // Use in-memory database
            _db = new LiteDatabase(new MemoryStream());
            _repo = new CartRepository(_db);
        }

        [Fact]
        public void CartExists_ReturnsFalse_WhenCartMissing()
        {
            var id = Guid.NewGuid();
            Assert.False(_repo.CartExists(id));
        }

        [Fact]
        public void SaveCart_PersistsCart_AndCartExistsReturnsTrue()
        {
            var id = Guid.NewGuid();
            var cartDto = new CartDTO
            {
                Id = id,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItemDTO>
                {
                    new CartItemDTO { ProductId = Guid.NewGuid(), Name="Item", Quantity=2, Price=5m }
                }
            };
            var saved = _repo.SaveCart(cartDto);
            Assert.True(_repo.CartExists(id));
            Assert.Equal(id, saved.Id);
            Assert.Single(saved.Items);
        }

        [Fact]
        public void GetCartById_ReturnsStoredCart()
        {
            var id = Guid.NewGuid();
            // Seed raw entity to collection
            var carts = _db.GetCollection<Cart>("carts");
            carts.Upsert(new Cart
            {
                Id = id,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = Guid.NewGuid(), Name="X", Quantity=1, Price=3m }
                }
            });
            var dto = _repo.GetCartById(id);
            Assert.Equal(id, dto.Id);
            Assert.Single(dto.Items);
            Assert.Equal("X", dto.Items[0].Name);
        }

        [Fact]
        public void GetCartById_NotFound_ThrowsNotFoundException()
        {
            var id = Guid.NewGuid();
            Assert.Throws<NotFoundException>(() => _repo.GetCartById(id));
        }

        [Fact]
        public void SaveCart_Upsert_UpdatesExisting()
        {
            var id = Guid.NewGuid();
            var first = new CartDTO { Id = id, CreatedAt = DateTime.UtcNow, Items = new List<CartItemDTO>() };
            _repo.SaveCart(first);
            var second = new CartDTO { Id = id, CreatedAt = DateTime.UtcNow, Items = new List<CartItemDTO> { new CartItemDTO { ProductId=Guid.NewGuid(), Name="Updated", Quantity=1, Price=2m } } };
            _repo.SaveCart(second);
            var fetched = _repo.GetCartById(id);
            Assert.Single(fetched.Items);
            Assert.Equal("Updated", fetched.Items[0].Name);
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
