using CartService.Transversal.Classes.DTOs;

namespace CartService.Transversal.Interfaces.Base
{
    public interface IBase
    {
        CartDTO GetCartById(Guid id);
    }
}
