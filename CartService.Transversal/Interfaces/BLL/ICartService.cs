using CartService.Transversal.Classes.DTOs;

namespace CartService.Transversal.Interfaces.BLL
{
    public interface ICartService
    {
        CartDTO GetCartById(Guid id);
        CartDTO AddItemToCart(Guid cartId, CartItemDTO cartItem);
        void RemoveItemFromCart(Guid id, Guid itemId);
    }
}
