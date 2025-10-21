using AutoMapper;

namespace CartService.Transversal.Classes.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Classes.Models.CartItemModel, Classes.DTOs.CartItemDTO>().ReverseMap();
        }
    }
}
