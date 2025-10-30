using CartService.DAL.Classes.Entities;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Interfaces.DAL;
using Common.Utilities.Classes.Exceptions;
using LiteDB;

namespace CartService.DAL.Classes
{
    public class CartRepository : ICartRepository
    {
        private readonly LiteDatabase _db;

        public CartRepository(LiteDatabase db)
        {
            _db = db;
        }

        public bool CartExists(Guid id)
        {
            var cart = _db.GetCollection<Cart>("carts").FindById(id);

            return cart != null;
        }

        public CartDTO GetCartById(Guid id)
        {
            var cart =  _db.GetCollection<Cart>("carts").FindById(id);

            if (cart == null)
            {
                throw new NotFoundException("Cart", id);
            }

            var cartDto = new CartDTO
            {
                Id = cart.Id,
                CreatedAt = cart.CreatedAt,
                Items = cart.Items.Select(item => new CartItemDTO
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList()
            };
            return cartDto;
        }

        public CartDTO SaveCart(CartDTO cartDto)
        {
            var entity = new Cart
            {
                Id = cartDto.Id,
                CreatedAt = cartDto.CreatedAt ?? DateTime.UtcNow,
                Items = cartDto.Items.Select(item => new CartItem
                {
                    Name = item.Name,
                    ProductId = item.ProductId,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList()
            };

            var carts = _db.GetCollection<Cart>("carts");
            carts.Upsert(entity);

            return new CartDTO
            {
                Id = entity.Id,
                CreatedAt = entity.CreatedAt,
                Items = entity.Items.Select(item => new CartItemDTO
                {
                    ProductId = item.ProductId,
                    Name = item.Name,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList()
            };
        }
    }
}
