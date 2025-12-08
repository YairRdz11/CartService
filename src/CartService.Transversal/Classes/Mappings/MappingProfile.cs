using AutoMapper;
using CartService.Transversal.Classes.DTOs;
using CartService.Transversal.Classes.Models.Request;
using CartService.Transversal.Classes.Models.Response;

namespace CartService.Transversal.Classes.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Cart <-> CartResponse
            CreateMap<CartDTO, CartResponse>()
                .ForMember(d => d.CartId, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

            CreateMap<CartResponse, CartDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.CartId))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

            // Nested items
            CreateMap<CartItemDTO, CartItemResponse>();
            CreateMap<CartItemResponse, CartItemDTO>();

            CreateMap<CartItemRequest, CartItemDTO>().ReverseMap();
        }
    }
}
