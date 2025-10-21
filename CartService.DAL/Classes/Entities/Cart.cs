using CartService.Transversal.Classes.DTOs;

namespace CartService.DAL.Classes.Entities
{
    public class Cart
    {

        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<CartItem> Items { get; set; } = new();

    }
}
