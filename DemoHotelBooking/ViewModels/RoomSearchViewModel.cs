using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.ViewModels
{
    public class RoomSearchViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public double Price { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public bool IsAvailable { get; set; }
        public int FloorNumber { get; set; }
        public string? Introduce { get; set; }
        public string Extension { get; set; }
        public int DAP { get; set; }
        public int MAP { get; set; }
    }
}
