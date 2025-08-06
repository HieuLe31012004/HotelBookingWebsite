using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int BookingId { get; set; } // mã đặt phòng
        public Booking Booking { get; set; }

        public DateTime CreateDate { get; set; } //thời gian tạo hóa đơn (Lập phiếu nhận)

        public DateTime PaymentDate { get; set; } //thời gian thanh toán

        public double Amount { get; set; } //tổng thanh toán

        public int PayMethod { get; set; } // hình thức thanh toán

        public int Status { get; set; } //Trạng thái

        public string ReceptionistId { get; set; }

        public AppUser Receptionist { get; set; }

        // Additional property for compatibility
        public decimal TotalPrice => (decimal)Amount;

        //0: moi tao
        //1: da checkin
        //2: da checkout
        //3: da thanh toan

    }
}
