using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Interfaces.Base;

namespace CartService.Transversal.Interfaces.DAL
{
    public interface ICartRepository : IBase
    {
        CartDTO SaveCart(CartDTO cart);
        bool CartExists(Guid id);
        int UpdateProductInfo(Guid productId, string? name, decimal? price, Guid? categoryId);
        int RemoveProduct(Guid productId);
    }
}
