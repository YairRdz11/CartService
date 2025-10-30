namespace CartService.Transversal.Classes.DTOs
{
    public class CartDTO
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<CartItemDTO> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Price * i.Quantity);

        public void Build(Guid cartId)
        {
            Id = cartId;
        }
    }
}
