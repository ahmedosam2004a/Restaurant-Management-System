using System.ComponentModel;

namespace RestaurantManagementApp.Models;

public class RestaurantOrder
{
    public int Id { get; set; }

    [Browsable(false)]
    public int TableId { get; set; }

    public string TableName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ClosedAt { get; set; }

    [Browsable(false)]
    public List<OrderItem> Items { get; set; } = new();

    public bool IsClosed => ClosedAt.HasValue;

    public string Status => IsClosed ? "مغلقة" : "مفتوحة";

    public int ItemsCount => Items.Count;

    public decimal Total => Items.Sum(item => item.LineTotal);
}
