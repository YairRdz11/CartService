using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Interfaces.Base;

namespace CartService.Transversal.Interfaces.BLL
{
    public interface ICartService : IBase
    {
        CartDTO AddItemToCart(Guid cartId, CartItemDTO cartItem);
        void RemoveItemFromCart(Guid id, Guid itemId);
    }
}
