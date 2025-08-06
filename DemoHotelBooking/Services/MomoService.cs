using DemoHotelBooking.Models.Momo;
using DemoHotelBooking.Models.Order;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Security.Cryptography;
using System.Text;

namespace DemoHotelBooking.Services
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly ILogger<MomoService> _logger;

        public MomoService(IOptions<MomoOptionModel> options, ILogger<MomoService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model, string action)
        {
            try
            {
                // Validate input
                if (model == null)
                {
                    _logger.LogError("OrderInfoModel is null");
                    return new MomoCreatePaymentResponseModel
                    {
                        ErrorCode = 99,
                        Message = "Thông tin đơn hàng không hợp lệ"
                    };
                }

                if (model.Amount <= 0)
                {
                    _logger.LogError("Invalid amount: {Amount}", model.Amount);
                    return new MomoCreatePaymentResponseModel
                    {
                        ErrorCode = 99,
                        Message = "Số tiền thanh toán không hợp lệ"
                    };
                }

                // Validate Momo configuration
                if (string.IsNullOrEmpty(_options.Value.PartnerCode) || 
                    string.IsNullOrEmpty(_options.Value.AccessKey) || 
                    string.IsNullOrEmpty(_options.Value.SecretKey))
                {
                    _logger.LogError("Momo configuration is incomplete");
                    return new MomoCreatePaymentResponseModel
                    {
                        ErrorCode = 99,
                        Message = "Cấu hình thanh toán Momo chưa được thiết lập"
                    };
                }

                model.OrderId = DateTime.UtcNow.Ticks.ToString();
                model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;
                
                var returnUrl = action == "invoice" ? _options.Value.ReturnUrlInvoice : _options.Value.ReturnUrl;
                
                // Convert amount to long for Momo API
                long amountLong = Convert.ToInt64(model.Amount);
                
                var rawData = $"accessKey={_options.Value.AccessKey}&amount={amountLong}&extraData=&orderId={model.OrderId}&orderInfo={model.OrderInfo}&partnerCode={_options.Value.PartnerCode}&redirectUrl={returnUrl}&requestId={model.OrderId}&requestType={_options.Value.RequestType}";
                
                var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

                _logger.LogInformation("Creating Momo payment for order: {OrderId}, Amount: {Amount}", model.OrderId, amountLong);

                var client = new RestClient(_options.Value.MomoApiUrl);
                var request = new RestRequest() { Method = Method.Post };
                request.AddHeader("Content-Type", "application/json; charset=UTF-8");

                // Create an object representing the request data
                var requestData = new
                {
                    partnerCode = _options.Value.PartnerCode,
                    accessKey = _options.Value.AccessKey,
                    requestId = model.OrderId,
                    amount = amountLong.ToString(),
                    orderId = model.OrderId,
                    orderInfo = model.OrderInfo,
                    redirectUrl = returnUrl,
                    ipnUrl = _options.Value.NotifyUrl,
                    extraData = "",
                    requestType = _options.Value.RequestType,
                    signature = signature,
                    lang = "vi"
                };

                _logger.LogInformation("Momo request data: {RequestData}", JsonConvert.SerializeObject(requestData, Formatting.Indented));

                request.AddParameter("application/json", JsonConvert.SerializeObject(requestData), ParameterType.RequestBody);

                var response = await client.ExecuteAsync(request);
                
                _logger.LogInformation("Momo response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Momo response content: {Content}", response.Content);

                if (!response.IsSuccessful)
                {
                    _logger.LogError("Momo API call failed: {ErrorMessage}", response.ErrorMessage);
                    return new MomoCreatePaymentResponseModel
                    {
                        ErrorCode = 99,
                        Message = "Không thể kết nối đến hệ thống thanh toán Momo. Vui lòng thử lại sau."
                    };
                }

                if (string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogError("Momo API returned empty response");
                    return new MomoCreatePaymentResponseModel
                    {
                        ErrorCode = 99,
                        Message = "Phản hồi từ Momo không hợp lệ"
                    };
                }

                var result = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
                
                if (result == null)
                {
                    _logger.LogError("Failed to deserialize Momo response");
                    return new MomoCreatePaymentResponseModel
                    {
                        ErrorCode = 99,
                        Message = "Không thể xử lý phản hồi từ Momo"
                    };
                }

                _logger.LogInformation("Momo payment creation result: ErrorCode={ErrorCode}, Message={Message}, PayUrl={PayUrl}", 
                    result.ErrorCode, result.Message, !string.IsNullOrEmpty(result.PayUrl) ? "Available" : "Empty");
                
                // Check if Momo returned an error
                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Momo returned error code: {ErrorCode}, Message: {Message}", result.ErrorCode, result.Message);
                    result.Message = GetFriendlyErrorMessage(result.ErrorCode, result.Message);
                }
                
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON serialization error while creating Momo payment");
                return new MomoCreatePaymentResponseModel
                {
                    ErrorCode = 99,
                    Message = "Lỗi xử lý dữ liệu thanh toán"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating Momo payment");
                return new MomoCreatePaymentResponseModel
                {
                    ErrorCode = 99,
                    Message = "Có lỗi xảy ra trong quá trình tạo thanh toán. Vui lòng thử lại sau."
                };
            }
        }

        public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
        {
            try
            {
                _logger.LogInformation("Processing Momo payment callback with {Count} parameters", collection.Count);
                
                foreach (var item in collection)
                {
                    _logger.LogInformation("Callback parameter: {Key} = {Value}", item.Key, item.Value);
                }

                var amount = collection.FirstOrDefault(s => s.Key == "amount").Value;
                var orderInfo = collection.FirstOrDefault(s => s.Key == "orderInfo").Value;
                var orderId = collection.FirstOrDefault(s => s.Key == "orderId").Value;
                var resultCode = collection.FirstOrDefault(s => s.Key == "resultCode").Value;
                var message = collection.FirstOrDefault(s => s.Key == "message").Value;
                var signature = collection.FirstOrDefault(s => s.Key == "signature").Value;

                // Verify signature for security
                var rawData = $"accessKey={_options.Value.AccessKey}&amount={amount}&extraData=&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType=momo_wallet&partnerCode={_options.Value.PartnerCode}&payType=qr&requestId={orderId}&responseTime={collection.FirstOrDefault(s => s.Key == "responseTime").Value}&resultCode={resultCode}&transId={collection.FirstOrDefault(s => s.Key == "transId").Value}";
                var computedSignature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

                _logger.LogInformation("Payment result: OrderId={OrderId}, ResultCode={ResultCode}, Message={Message}", orderId, resultCode, message);
                
                return new MomoExecuteResponseModel()
                {
                    Amount = amount,
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    ResultCode = resultCode,
                    Message = message,
                    IsValidSignature = signature == computedSignature
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Momo payment callback");
                return null;
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hashString;
        }

        private string GetFriendlyErrorMessage(int errorCode, string originalMessage)
        {
            return errorCode switch
            {
                0 => "Thành công",
                9 => "Merchant không hợp lệ",
                10 => "Dữ liệu yêu cầu không hợp lệ",
                11 => "Số tiền không hợp lệ",
                12 => "Mã tiền tệ không hợp lệ",
                13 => "Chữ ký không hợp lệ",
                20 => "Merchant không có quyền truy cập",
                21 => "Số tiền vượt quá giới hạn",
                99 => "Có lỗi không xác định xảy ra",
                _ => $"Lỗi thanh toán: {originalMessage}"
            };
        }
    }
}
