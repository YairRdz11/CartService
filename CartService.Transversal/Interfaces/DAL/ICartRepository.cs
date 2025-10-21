using CartService.Transversal.Classes.DTOs;

namespace CartService.Transversal.Interfaces.DAL
{
    public interface ICartRepository
    {
        CartDTO? GetCartById(Guid id);
        CartDTO SaveCart(CartDTO cart);
    }
}
