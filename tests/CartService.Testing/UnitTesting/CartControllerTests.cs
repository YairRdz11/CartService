using AutoMapper;
using CartService.API.Controllers.v1;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Classes.Models.Request;
using CartService.Transversal.Classes.Models.Response;
using CartService.Transversal.Interfaces.BLL;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CartService.Testing.UnitTesting
{
     public class CartControllerTests
     {
         private readonly Mock<ICartService> _serviceMock = new();
         private readonly Mock<IMapper> _mapperMock = new();

         private CartController CreateController() => new CartController(_serviceMock.Object, _mapperMock.Object);

         [Fact]
         public void GetCart_ReturnsOk_WithMappedResponse()
         {
             var cartId = Guid.NewGuid();
             var dto = new CartDTO
             {
                Id = cartId,
                Items = new List<CartItemDTO>
                {
                    new CartItemDTO { ProductId = Guid.NewGuid(), Name="Item", Quantity=1, Price=10m }
                }
             };
             var mapped = new CartResponse
             {
                CartId = cartId,
                Items = new List<CartItemResponse>
                {
                    new CartItemResponse { ProductId = dto.Items[0].ProductId, Name="Item", Quantity=1, Price=10m }
                }
             };

             _serviceMock.Setup(s => s.GetCartById(cartId)).Returns(dto);
             _mapperMock.Setup(m => m.Map<CartResponse>(dto)).Returns(mapped);

             var controller = CreateController();
             var action = controller.GetCart(cartId);
             var ok = Assert.IsType<OkObjectResult>(action.Result);

             Assert.NotNull(ok.Value);
             var payload = Assert.IsType<CartResponse>(GetResultPayload(ok.Value));
             Assert.Equal(cartId, payload.CartId);
             Assert.Single(payload.Items);
             _serviceMock.Verify(s => s.GetCartById(cartId), Times.Once);
             _mapperMock.Verify(m => m.Map<CartResponse>(dto), Times.Once);
         }

         [Fact]
         public void AddItem_MapsRequest_CallsService_ReturnsOk()
         {
             var cartId = Guid.NewGuid();
             var request = new CartItemRequest
             {
                 ProductId = Guid.NewGuid(),
                 Name = "NewItem",
                 imageUrl = "http://img",
                 Quantity =2,
                 Price =5m
             };

             var mappedItem = new CartItemDTO
             {
                 ProductId = request.ProductId,
                 Name = request.Name,
                 ImageUrl = request.imageUrl,
                 Quantity = request.Quantity,
                 Price = request.Price
             };

             var updatedDto = new CartDTO
             {
                 Id = cartId,
                 Items = new List<CartItemDTO> { mappedItem }
             };

             var updatedResponse = new CartResponse
             {
                 CartId = cartId,
                 Items = new List<CartItemResponse>
                 {
                     new CartItemResponse
                     {
                         ProductId = mappedItem.ProductId,
                         Name = mappedItem.Name,
                         Quantity = mappedItem.Quantity,
                         Price = mappedItem.Price
                     }
                 }
             };

             _mapperMock.Setup(m => m.Map<CartItemDTO>(request)).Returns(mappedItem);
             _serviceMock.Setup(s => s.AddItemToCart(cartId, mappedItem)).Returns(updatedDto);
             _mapperMock.Setup(m => m.Map<CartResponse>(updatedDto)).Returns(updatedResponse);

             var controller = CreateController();
             var action = controller.AddItem(cartId, request);
             var ok = Assert.IsType<OkObjectResult>(action.Result);

             var payload = Assert.IsType<CartResponse>(GetResultPayload(ok.Value));
             Assert.Equal(cartId, payload.CartId);
             Assert.Single(payload.Items);
             Assert.Equal(request.ProductId, payload.Items[0].ProductId);

             _mapperMock.Verify(m => m.Map<CartItemDTO>(request), Times.Once);
             _serviceMock.Verify(s => s.AddItemToCart(cartId, mappedItem), Times.Once);
             _mapperMock.Verify(m => m.Map<CartResponse>(updatedDto), Times.Once);
         }

         [Fact]
         public void RemoveItem_CallsService_ReturnsOk_WithMessage()
         {
             var cartId = Guid.NewGuid();
             var itemId = Guid.NewGuid();
             _serviceMock.Setup(s => s.RemoveItemFromCart(cartId, itemId));

             var controller = CreateController();
             var action = controller.RemoveItem(cartId, itemId);
             var ok = Assert.IsType<OkObjectResult>(action);

             Assert.NotNull(ok.Value);
             var message = Assert.IsType<string>(GetResultPayload(ok.Value));
             Assert.Contains(itemId.ToString(), message);
             Assert.Contains(cartId.ToString(), message);
             _serviceMock.Verify(s => s.RemoveItemFromCart(cartId, itemId), Times.Once);
         }

         // Helper to extract controller "Result" payload pattern used in actions
         private static object GetResultPayload(object value)
         {
             // The controller wraps responses into an ApiResponse object with Result property.
             // To avoid referencing the ApiResponse type directly, use reflection to read the Result property.
             var resultProp = value.GetType().GetProperty("Result");
             return resultProp != null ? resultProp.GetValue(value)! : value;
         }
     }
}
