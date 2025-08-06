using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.Models
{
    public class Feedback
    {
        public string CusId { get; set; }
        public AppUser User { get; set; }

        public int Stars { get; set; }

        public string? Comment { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? EditDate { get; set; }

        public bool Status { get; set; }
    }
}
