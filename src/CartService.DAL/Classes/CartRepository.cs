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

        public CartDTO SaveCart(CartDTO cart)
        {
            var entity = new Cart
            {
                Id = cart.Id,
                CreatedAt = cart.CreatedAt ?? DateTime.UtcNow,
                Items = cart.Items.Select(item => new CartItem
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

        public int UpdateProductInfo(Guid productId, string? name, decimal? price, Guid? categoryId)
        {
            var cartsCol = _db.GetCollection<Cart>("carts");
            var affected =0;

            foreach (var cart in cartsCol.FindAll())
            {
                if (cart.Items == null || cart.Items.Count ==0)
                {
                    continue;
                }

                var changed = false;

                foreach (var item in cart.Items)
                {
                    if (item.ProductId != productId)
                    {
                        continue;
                    }

                    changed |= UpdateItemFields(item, name, price);
                }

                if (!changed)
                {
                    continue;
                }

                cartsCol.Upsert(cart);
                affected++;
            }

            return affected;
        }

        private static bool UpdateItemFields(CartItem item, string? name, decimal? price)
        {
            var changed = false;

            if (!string.IsNullOrEmpty(name) && item.Name != name)
            {
                item.Name = name!;
                changed = true;
            }

            if (price.HasValue && item.Price != price.Value)
            {
                item.Price = price.Value;
                changed = true;
            }

            return changed;
        }

        public int RemoveProduct(Guid productId)
        {
            var cartsCol = _db.GetCollection<Cart>("carts");
            var affected =0;

            foreach (var cart in cartsCol.FindAll())
            {
                var before = cart.Items.Count;
                cart.Items = cart.Items.Where(i => i.ProductId != productId).ToList();
                if (cart.Items.Count != before)
                {
                    cartsCol.Upsert(cart);
                    affected++;
                }
            }

            return affected;
        }
    }
}
