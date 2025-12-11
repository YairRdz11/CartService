using System;
using System.IO;
using System.Collections.Generic;
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

        [Fact]
        public void UpdateProductInfo_UpdatesMatchingItems_ReturnsAffectedCount()
        {
            var productId = Guid.NewGuid();
            var otherProductId = Guid.NewGuid();

            var carts = _db.GetCollection<Cart>("carts");
            // Cart1: has matching and non-matching items
            carts.Upsert(new Cart
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = productId, Name="Old", Quantity=1, Price=1m },
                    new CartItem { ProductId = otherProductId, Name="Other", Quantity=2, Price=3m }
                }
            });
            // Cart2: has matching item
            var cart2Id = Guid.NewGuid();
            carts.Upsert(new Cart
            {
                Id = cart2Id,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = productId, Name="Old2", Quantity=5, Price=10m }
                }
            });
            // Cart3: no matching items
            carts.Upsert(new Cart
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = otherProductId, Name="OtherOnly", Quantity=1, Price=2m }
                }
            });

            var affected = _repo.UpdateProductInfo(productId, name: "NewName", price:9.99m, categoryId: null);
            Assert.Equal(2, affected); // Only carts1 and2 should be affected

            // Verify items updated in affected carts
            var updatedCart2 = carts.FindById(cart2Id);
            Assert.NotNull(updatedCart2);
            Assert.Equal("NewName", updatedCart2.Items[0].Name);
            Assert.Equal(9.99m, updatedCart2.Items[0].Price);
        }

        [Fact]
        public void UpdateProductInfo_NoChanges_ReturnsZero()
        {
            var productId = Guid.NewGuid();
            var carts = _db.GetCollection<Cart>("carts");
            carts.Upsert(new Cart
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = productId, Name="Same", Quantity=1, Price=5m }
                }
            });

            // Provide same values, should not mark as changed
            var affected = _repo.UpdateProductInfo(productId, name: "Same", price:5m, categoryId: null);
            Assert.Equal(0, affected);
        }

        [Fact]
        public void RemoveProduct_RemovesItemsAcrossCarts_ReturnsAffectedCount()
        {
            var productId = Guid.NewGuid();
            var otherProductId = Guid.NewGuid();
            var carts = _db.GetCollection<Cart>("carts");

            var cart1Id = Guid.NewGuid();
            carts.Upsert(new Cart
            {
                Id = cart1Id,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = productId, Name="ToRemove", Quantity=1, Price=1m },
                    new CartItem { ProductId = otherProductId, Name="Keep", Quantity=1, Price=2m }
                }
            });

            var cart2Id = Guid.NewGuid();
            carts.Upsert(new Cart
            {
                Id = cart2Id,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = productId, Name="ToRemove2", Quantity=1, Price=1m }
                }
            });

            var cart3Id = Guid.NewGuid();
            carts.Upsert(new Cart
            {
                Id = cart3Id,
                CreatedAt = DateTime.UtcNow,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = otherProductId, Name="OnlyOther", Quantity=1, Price=1m }
                }
            });

            var affected = _repo.RemoveProduct(productId);
            Assert.Equal(2, affected); // cart1 and cart2 affected

            var updated1 = carts.FindById(cart1Id);
            Assert.Single(updated1.Items); // Only "Keep" remains
            Assert.Equal(otherProductId, updated1.Items[0].ProductId);

            var updated2 = carts.FindById(cart2Id);
            Assert.Empty(updated2.Items);

            var updated3 = carts.FindById(cart3Id);
            Assert.Single(updated3.Items); // unchanged
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
