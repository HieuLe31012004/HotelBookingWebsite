using DemoHotelBooking.Models.Momo;
using DemoHotelBooking.Models.Order;

namespace DemoHotelBooking.Services
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model,string action);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
}
