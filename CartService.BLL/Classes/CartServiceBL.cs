using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Interfaces.BLL;
using CartService.Transversal.Interfaces.DAL;
using YairUtilities.CommonUtilities.Classes.Exceptions;

namespace CartService.BLL.Classes
{
    public class CartServiceBL : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartServiceBL(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public CartDTO AddItemToCart(Guid cartId, CartItemDTO cartItem)
        {
            var cart = _cartRepository.GetCartById(cartId) ?? new CartDTO { Id = cartId, CreatedAt = DateTime.UtcNow };

            cart.Items.Add(cartItem);

            return _cartRepository.SaveCart(cart);
        }

        public CartDTO GetCartById(Guid id)
        {
            var cart = _cartRepository.GetCartById(id);

            return cart;
        }

        public void RemoveItemFromCart(Guid id, Guid itemId)
        {
            var cart = _cartRepository.GetCartById(id);
            if (cart == null)
            {
                throw new NotFoundException("Cart", id);
            }
            var itemToRemove = cart.Items.FirstOrDefault(i => i.ProductId == itemId);
            if (itemToRemove == null)
            {
                throw new NotFoundException("CartItem", itemId);
            }
            cart.Items.Remove(itemToRemove);
            _cartRepository.SaveCart(cart);
        }
    }
}
