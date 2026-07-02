using System.ComponentModel;

namespace RestaurantManagementApp.Models;

public class OrderItem
{
    [Browsable(false)]
    public int MenuItemId { get; set; }

    public string MenuItemName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
