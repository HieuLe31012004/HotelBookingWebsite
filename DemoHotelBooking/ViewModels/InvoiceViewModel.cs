using DemoHotelBooking.Models;
using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.ViewModels
{
    public class InvoiceViewModel
    {
        [Display(Name = "Mã hóa đơn")]
        public int? Id { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime? CreateDate { get; set; }

        [Display(Name = "Tổng cộng")]
        public double? Amount { get; set; }

        [Display(Name = "Hình thức thanh toán")]
        public int? PaymentMethod { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; }

        [Display(Name = "Mã đặt sân")]
        public Booking Booking { get; set; }

        public AppUser? Receptionist { get; set; }

        public List<Room>? Rooms { get; set; }

        public InvoiceViewModel() { }
        public InvoiceViewModel(Invoice invoice)
        {
            Id = invoice.Id;
            CreateDate = invoice.CreateDate;
            Status= invoice.Status;
            Booking = invoice.Booking;
            Receptionist = invoice.Receptionist;            
        }
    }
}
