using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Interfaces.Base;

namespace CartService.Transversal.Interfaces.DAL
{
    public interface ICartRepository : IBase
    {
        CartDTO SaveCart(CartDTO cart);
        bool CartExists(Guid id);
    }
}
