using System.ComponentModel.DataAnnotations;

namespace CartService.Transversal.Classes.Models.Request
{
    public class CartItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }
        [Required]
        public required string Name { get; set; }
        public string? imageUrl { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        [Required]
        public decimal Price { get; set; }
    }
}
