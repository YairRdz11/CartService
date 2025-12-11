namespace CartService.Transversal.Classes.Models.Response
{
    public class CartItemResponse
    {
        public Guid ProductId { get; set; }
        public required string Name { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
