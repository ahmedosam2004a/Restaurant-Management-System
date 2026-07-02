namespace RestaurantManagementApp.Models;

public class Reservation
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int GuestsCount { get; set; }

    public DateTime ReservedAt { get; set; } = DateTime.Now;

    public string Status { get; set; } = "مؤكدة";

    public string Notes { get; set; } = string.Empty;
}
