namespace DemoHotelBooking.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string? CusID { get; set; } // Id khách hàng

        public AppUser Customer { get; set; }

        public int PaymentMethod { get; set; } //phương thức đặt cọc

        public double Deposit { get; set; } //tiền cọc

        public DateTime CreateDate { get; set; } //ngày tạo đơn

        public DateTime CheckinDate { get; set; } //ngày nhận dự kiến

        public DateTime CheckoutDate { get; set; } // ngày trả dự kiến

        public int Status { get; set; } // trạng thái đặt phòng

        // Navigation properties
        public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        
        // Additional properties
        public string? Note { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}