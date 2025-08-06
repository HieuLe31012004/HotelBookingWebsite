using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Models
{
    public static class BookingStatus
    {
        public const int Deposited = 0;     // Đã đặt cọc
        public const int CancelRequested = 1; // Yêu cầu hủy (chờ hoàn tiền)
        public const int CheckedIn = 2;     // Đã nhận phòng
        public const int CheckedOut = 3;    // Đã trả phòng
        public const int Cancelled = 4;     // Hủy và hoàn tất hoàn tiền
        public const int Done = 5;     // Hoan tất
    }
    public static class RoomStatusHelper
    {
        public static void UpdateRoomStatus(Room room, List<Booking> bookings)
        {
            var now = DateTime.Now;

            var futureBookings = bookings;

            var currentBooking = futureBookings.FirstOrDefault(b => b.Status == BookingStatus.CheckedIn);

            if (currentBooking != null)
            {
                room.Status = "Occupied";
            }
            else if (futureBookings.Any(b => b.Status == BookingStatus.CancelRequested))
            {
                room.Status = "PendingCancel";
            }
            else if (futureBookings.Any(b => b.Status == BookingStatus.Deposited))
            {
                room.Status = "Reserved";
            }
            else
            {
                room.Status = "Available";
            }
        }

        public static void UpdateAllRoomStatuses(List<Room> rooms, List<Booking> bookings)
        {
            foreach (var room in rooms)
            {
                RoomStatusHelper.UpdateRoomStatus(room, bookings);
            }
        }

        public static string GetRoomStatusName(string status)
        {
            return status switch
            {
                "Available" => "Có sẵn",
                "Reserved" => "Đã có người đặt trước",
                "Occupied" => "Phòng kín",
                "PendingCancel" => "Phòng chuẩn bị sẵn sàng",
                "Maintenance" => "Phòng đang bảo trì",
                _ => "Không xác định"
            };
        }
    }

    public class RoomStatusUpdateService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RoomStatusUpdateService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Cứ 6 giờ chạy 1 lần

        public RoomStatusUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<RoomStatusUpdateService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateRoomStatuses();
                await Task.Delay(_interval, stoppingToken); // đợi 6 tiếng
            }
        }

        private async Task UpdateRoomStatuses()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.Today;

            // Lấy danh sách booking đang hoạt động trong ngày hôm nay
            var bookingsToday = await context.Bookings
                .Where(b =>
                    (b.Status == BookingStatus.Deposited ||
                     b.Status == BookingStatus.CheckedIn ||
                     b.Status == BookingStatus.CancelRequested) &&
                    b.CheckinDate.Date <= today &&
                    b.CheckoutDate.Date >= today)
                .Include(b => b.BookingDetails)
                .ToListAsync();

            // Lấy các ID phòng có trong bookings
            var roomIds = bookingsToday
                .SelectMany(b => b.BookingDetails)
                .Select(d => d.RoomId)
                .Distinct()
                .ToList();

            // Lấy các phòng tương ứng
            var rooms = await context.Rooms
                .Where(r => roomIds.Contains(r.Id))
                .ToListAsync();

            // Cập nhật trạng thái phòng
            RoomStatusHelper.UpdateAllRoomStatuses(rooms, bookingsToday);

            // Lưu vào database
            await context.SaveChangesAsync();

            _logger.LogInformation($"Room statuses updated at {DateTime.Now}");
        }
    }
}
