using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.Models
{
    public class InvoiceDetail
    {
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public double Price { get; set; }

        public int SubFee { get; set; } //tỷ lệ phụ thu
    }
}
