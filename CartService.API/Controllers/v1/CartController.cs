using Asp.Versioning;
using AutoMapper;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Classes.Models;
using CartService.Transversal.Interfaces.BLL;
using Microsoft.AspNetCore.Mvc;
using YairUtilities.ApiUtilities.Classes.Common;
using YairUtilities.CommonUtilities.Classes.Common;

namespace CartService.API.Controllers.v1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class CartController : ApiControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public CartController(ICartService cartService, IMapper mapper)
        {
            _cartService = cartService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("{id:guid}")]
        public ActionResult<CartDTO> GetCart([FromRoute] Guid id)
        {
            var cart = _cartService.GetCartById(id);
            return Ok(new ApiResponse
            {
                Result = cart,
                Status = 200
            });
        }

        [HttpPost]
        [Route("{id:guid}/items")]
        public ActionResult<CartDTO> AddItem([FromRoute] Guid id, [FromBody] CartItemModel cartItem)
        {
            var cartItemDto = _mapper.Map<CartItemDTO>(cartItem);
            var updatedCart = _cartService.AddItemToCart(id, cartItemDto);

            return Ok(new ApiResponse
            {
                Result = updatedCart,
                Status = 201
            });
        }

        [HttpDelete]
        [Route("{id:guid}/items/{itemId:guid}")]
        public ActionResult RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId)
        {
            _cartService.RemoveItemFromCart(id, itemId);
            return Ok(new ApiResponse
            {
                Result = $"Item {itemId} removed from cart {id}.",
                Status = 204
            });
        }
    }
}
