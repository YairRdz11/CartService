using Asp.Versioning;
using AutoMapper;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Classes.Models.Request;
using CartService.Transversal.Classes.Models.Response;
using CartService.Transversal.Interfaces.BLL;
using Common.ApiUtilities.Classes.Common;
using Common.Utilities.Classes.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CartService.API.Controllers.v1
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    //[Authorize(Roles = "Manager,StoreCustomer")]
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
            var cartResponse = _mapper.Map<CartResponse>(cart);
            return Ok(new ApiResponse
            {
                Result = cartResponse,
                Status = 200
            });
        }

        [HttpPost]
        [Route("{id:guid}/items")]
        public ActionResult<CartDTO> AddItem([FromRoute] Guid id, [FromBody] CartItemRequest cartItem)
        {
            var cartItemDto = _mapper.Map<CartItemDTO>(cartItem);
            var updatedCart = _cartService.AddItemToCart(id, cartItemDto);
            var updatedCartResponse = _mapper.Map<CartResponse>(updatedCart);
            return Ok(new ApiResponse
            {
                Result = updatedCartResponse,
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
