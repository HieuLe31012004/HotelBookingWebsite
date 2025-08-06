using DemoHotelBooking.Models;

namespace DemoHotelBooking.ViewModels
{
    public class InvoiceView
    {
        public Invoice Invoice { get; set; }
        public List<InvoiceDetail> InvoiceDetail { get; set; }
        public string Status
        {
            get
            {
                switch (Invoice.Status)
                {
                    case 1: return "Đã nhận phòng";
                    case 2: return "Đã trả phòng";
                    case 3: return "Đã thanh toán";
                    default: return "";
                }
            }
        }
        public string PayMethod
        {
            get
            {
                if (Invoice.PayMethod == 0) return "Tiền mặt";
                else return "Momo";
            }
        }
        public double SubFee { get; set; }
        public double Final { get; set; }
    }
}
