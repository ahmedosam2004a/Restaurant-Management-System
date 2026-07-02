namespace RestaurantManagementApp.Models;

public class RestaurantData
{
    public List<Category> Categories { get; set; } = new();

    public List<MenuItem> MenuItems { get; set; } = new();

    public List<RestaurantTable> Tables { get; set; } = new();

    public List<RestaurantOrder> Orders { get; set; } = new();

    public List<Reservation> Reservations { get; set; } = new();

    public int NextMenuItemId { get; set; } = 1;

    public int NextCategoryId { get; set; } = 1;

    public int NextTableId { get; set; } = 1;

    public int NextOrderId { get; set; } = 1;
}
