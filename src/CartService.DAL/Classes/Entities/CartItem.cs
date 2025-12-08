namespace CartService.DAL.Classes.Entities
{
    public class CartItem
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
