using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.Models
{
    public class RoomImage
    {
        [Key]
        public string Path { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public bool IsDefault { get; set; } //Ảnh mặc định
    }
}
