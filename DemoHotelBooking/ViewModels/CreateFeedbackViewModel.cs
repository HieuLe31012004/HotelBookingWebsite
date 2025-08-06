using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.ViewModels
{
    public class CreateFeedbackViewModel
    {
        public string CusId { get; set; }
        public int Stars { get; set; }
        public string Comment { get; set; }
    }
}
