using System.ComponentModel;

namespace RestaurantManagementApp.Models;

public class RestaurantTable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Seats { get; set; }

    [Browsable(false)]
    public bool IsOccupied { get; set; }

    [Browsable(false)]
    public int? ActiveOrderId { get; set; }

    public string Status => IsOccupied ? "مشغولة" : "فارغة";
}
