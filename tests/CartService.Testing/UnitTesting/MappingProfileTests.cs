using AutoMapper;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Classes.Mappings;
using CartService.Transversal.Classes.Models.Request;
using CartService.Transversal.Classes.Models.Response;
using Xunit;

namespace CartService.Testing.UnitTesting
{
     public class MappingProfileTests
     {
         private readonly IMapper _mapper;

         public MappingProfileTests()
         {
             var cfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
             _mapper = cfg.CreateMapper();
         }

         [Fact]
         public void Map_CartDTO_To_CartResponse_MapsIdsAndItems()
         {
             var itemDto = new CartItemDTO
             {
                 ProductId = Guid.NewGuid(),
                 Name = "Item",
                 Price =12.5m,
                 Quantity =2,
                 ImageUrl = "http://img"
             };
             var cartDto = new CartDTO
             {
                 Id = Guid.NewGuid(),
                 Items = new List<CartItemDTO> { itemDto }
             };

             var response = _mapper.Map<CartResponse>(cartDto);
             Assert.Equal(cartDto.Id, response.CartId);
             Assert.Single(response.Items);
             Assert.Equal(itemDto.ProductId, response.Items[0].ProductId);
             Assert.Equal(itemDto.Name, response.Items[0].Name);
             Assert.Equal(itemDto.Price, response.Items[0].Price);
             Assert.Equal(itemDto.Quantity, response.Items[0].Quantity);
             // Ensure total matches computed
             Assert.Equal(itemDto.Price * itemDto.Quantity, response.Total);
         }

         [Fact]
         public void Map_CartResponse_To_CartDTO_MapsIdsAndItems()
         {
             var itemResp = new CartItemResponse
             {
                 ProductId = Guid.NewGuid(),
                 Name = "Item",
                 Price =5m,
                 Quantity =3
             };
             var cartResp = new CartResponse
             {
                 CartId = Guid.NewGuid(),
                 Items = new List<CartItemResponse> { itemResp },
                 Total = itemResp.Price * itemResp.Quantity
             };

             var dto = _mapper.Map<CartDTO>(cartResp);
             Assert.Equal(cartResp.CartId, dto.Id);
             Assert.Single(dto.Items);
             Assert.Equal(itemResp.ProductId, dto.Items[0].ProductId);
             Assert.Equal(itemResp.Name, dto.Items[0].Name);
             Assert.Equal(itemResp.Price, dto.Items[0].Price);
             Assert.Equal(itemResp.Quantity, dto.Items[0].Quantity);
             // Ensure total computed same
             Assert.Equal(cartResp.Total, dto.Total);
         }

         [Fact]
         public void Map_CartItemDTO_To_Response_And_Back()
         {
             var dto = new CartItemDTO
             {
                 ProductId = Guid.NewGuid(),
                 Name = "ItemX",
                 Price =7m,
                 Quantity =4,
                 ImageUrl = null
             };
             var resp = _mapper.Map<CartItemResponse>(dto);
             Assert.Equal(dto.ProductId, resp.ProductId);
             Assert.Equal(dto.Name, resp.Name);
             Assert.Equal(dto.Price, resp.Price);
             Assert.Equal(dto.Quantity, resp.Quantity);

             var dtoBack = _mapper.Map<CartItemDTO>(resp);
             Assert.Equal(resp.ProductId, dtoBack.ProductId);
             Assert.Equal(resp.Name, dtoBack.Name);
             Assert.Equal(resp.Price, dtoBack.Price);
             Assert.Equal(resp.Quantity, dtoBack.Quantity);
         }

         [Fact]
         public void Map_CartItemRequest_To_DTO_And_Back()
         {
             var req = new CartItemRequest
             {
                 ProductId = Guid.NewGuid(),
                 Name = "ReqItem",
                 imageUrl = "http://image",
                 Quantity =2,
                 Price =3.5m
             };

             var dto = _mapper.Map<CartItemDTO>(req);
             Assert.Equal(req.ProductId, dto.ProductId);
             Assert.Equal(req.Name, dto.Name);
             Assert.Equal(req.imageUrl, dto.ImageUrl);
             Assert.Equal(req.Quantity, dto.Quantity);
             Assert.Equal(req.Price, dto.Price);

             var reqBack = _mapper.Map<CartItemRequest>(dto);
             Assert.Equal(dto.ProductId, reqBack.ProductId);
             Assert.Equal(dto.Name, reqBack.Name);
             Assert.Equal(dto.ImageUrl, reqBack.imageUrl);
             Assert.Equal(dto.Quantity, reqBack.Quantity);
             Assert.Equal(dto.Price, reqBack.Price);
         }
     }
}
