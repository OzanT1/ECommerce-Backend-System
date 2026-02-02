public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderPaidEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public decimal TotalAmount { get; set; }
}