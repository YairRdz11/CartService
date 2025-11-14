namespace CartService.Transversal.Classes.Messages
{
    public class ProductUpdatedMessage
    {

        public Guid ProductId { get; set; }
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public Guid? CategoryId { get; set; }
    }
}
