namespace CartService.Transversal.Classes.Models.Response
{
    public class CartResponse
    {
        public Guid CartId { get; set; }
        public decimal Total { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
    }
}
