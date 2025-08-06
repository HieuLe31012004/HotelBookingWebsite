using DemoHotelBooking.ViewModels;

namespace DemoHotelBooking.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPaymentRequestModel model, string? url);
        VnPaymentResponseModel PaymentExecute(IQueryCollection collection);
    }
}
