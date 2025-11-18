namespace CartService.Transversal.Classes.Messages
{
 public class CategoryUpdatedMessage
 {
 public Guid CategoryId { get; set; }
 public string? Name { get; set; }
 public Guid EventId { get; set; }
 public DateTime OccurredOnUtc { get; set; }
 public string? EventType { get; set; }
 public int Version { get; set; }
 }
}
