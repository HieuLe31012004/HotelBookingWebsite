using DemoHotelBooking.Models;
using DemoHotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace DemoHotelBooking.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomManagerController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AppDbContext _context { get; set; }
        private readonly UserManager<AppUser> _userManager;

        public RoomManagerController(AppDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;

        }
        public IActionResult Create()
        {
            var roomVm = new RoomViewModel();

            // Danh sách tiện ích mẫu
            ViewBag.ExtensionsList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                    new SelectListItem { Text = "TV", Value = "TV" },
                    new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                    new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                    new SelectListItem { Text = "Wifi", Value = "Wifi" }
                };

            return View(roomVm);
        }
        [HttpPost]
        public async Task<IActionResult> Create(RoomViewModel model, List<IFormFile> images, List<string> SelectedExtensions)
        {

            Room room = _context.Rooms.FirstOrDefault(i => i.Name == model.Name);
            if (room == null)
            {
                room = new Room();
                room.Name = model.Name;
                room.Type = model.Type;
                room.FloorNumber = model.FloorNumber;
                room.Introduce = model.Introduce;
                room.Description = model.Description;
                room.MAP = model.MAP;
                room.DAP = model.DAP;
                room.Price = model.Price;
                room.Extension = SelectedExtensions != null ? string.Join(", ", SelectedExtensions) : "";


                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                // Xử lý upload nhiều ảnh
                if (images != null && images.Count > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "rooms");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    foreach (var image in images.Take(10)) // Giới hạn 10 ảnh
                    {
                        if (image.Length > 0)
                        {
                            var fileName = $"room_{room.Id}_{Guid.NewGuid().ToString()}{Path.GetExtension(image.FileName)}";
                            var filePath = Path.Combine(uploadsPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            // Lưu thông tin ảnh vào database
                            var roomImage = new RoomImage
                            {
                                Path = $"/img/rooms/{fileName}",
                                RoomId = room.Id,
                                IsDefault = _context.RoomImages.Count(ri => ri.RoomId == room.Id) == 0 // Ảnh đầu tiên là default
                            };

                            _context.RoomImages.Add(roomImage);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Tạo phòng thành công!";
                return RedirectToAction("AllRoomList", "RoomManager", new { area = "Admin" });
            }
            {
                if (!ModelState.IsValid)

                    // Danh sách tiện ích mẫu
                    ViewBag.ExtensionsList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                    new SelectListItem { Text = "TV", Value = "TV" },
                    new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                    new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                    new SelectListItem { Text = "Wifi", Value = "Wifi" }
                };
                ViewBag.Error = "Phòng đã tồn ";
            }
            return View(model);
        }
        public IActionResult AllRoomList()
        {
            var list = _context.Rooms.ToList();
            return View(list);
        }
        public IActionResult Update(int Id)
        {
            var room = _context.Rooms.FirstOrDefault(r => r.Id == Id);
            ViewBag.ExtensionsList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                    new SelectListItem { Text = "TV", Value = "TV" },
                    new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                    new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                    new SelectListItem { Text = "Wifi", Value = "Wifi" }
                };
            if (room == null)
                return NotFound();
            // Map entity Room sang RoomViewModel
            var vm = new RoomViewModel
            {
                Id = room.Id,
                Name = room.Name,
                Type = room.Type,
                FloorNumber = room.FloorNumber,
                Introduce = room.Introduce,
                Description = room.Description,
                MAP = room.MAP,
                DAP = room.DAP,
                Price = room.Price,
                Extension = room.Extension
            };
            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> Update(RoomViewModel model, List<string> SelectedExtensions)
        {
            if (ModelState.IsValid)
            {
                // Map loại phòng sang prefix
                var typeToPrefix = new Dictionary<string, string> {
                    {"Standard", "STD"},
                    {"Superior", "SUP"},
                    {"Deluxe", "DLX"},
                    {"Suite", "SUT"}
                };
                var prefix = typeToPrefix.ContainsKey(model.Type) ? typeToPrefix[model.Type] : null;
                if (string.IsNullOrWhiteSpace(model.Name) || prefix == null)
                {
                    ViewBag.ExtensionsList = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                        new SelectListItem { Text = "TV", Value = "TV" },
                        new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                        new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                        new SelectListItem { Text = "Wifi", Value = "Wifi" }
                    };
                    ViewBag.Error = "Loại phòng hoặc mã phòng không hợp lệ.";
                    return View(model);
                }
                // Validate định dạng mã phòng
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Name, "^(STD|SUP|DLX|SUT)\\d+$"))
                {
                    ViewBag.ExtensionsList = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                        new SelectListItem { Text = "TV", Value = "TV" },
                        new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                        new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                        new SelectListItem { Text = "Wifi", Value = "Wifi" }
                    };
                    ViewBag.Error = "Mã phòng phải đúng định dạng: Tiền tố in hoa (STD, SUP, DLX, SUT) + số.";
                    return View(model);
                }
                // Validate tiền tố khớp loại phòng
                if (!model.Name.StartsWith(prefix))
                {
                    ViewBag.ExtensionsList = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                        new SelectListItem { Text = "TV", Value = "TV" },
                        new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                        new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                        new SelectListItem { Text = "Wifi", Value = "Wifi" }
                    };
                    ViewBag.Error = $"Tiền tố mã phòng phải khớp với loại phòng đã chọn ({prefix})!";
                    return View(model);
                }

                var roomNameExists = _context.Rooms.FirstOrDefault(i => i.Name == model.Name);
                var room = _context.Rooms.FirstOrDefault(i => i.Id == model.Id);
                if (roomNameExists != null && roomNameExists.Name != room.Name)
                {
                    ViewBag.ExtensionsList = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "Máy lạnh", Value = "Máy lạnh" },
                        new SelectListItem { Text = "TV", Value = "TV" },
                        new SelectListItem { Text = "Tủ lạnh", Value = "Tủ lạnh" },
                        new SelectListItem { Text = "Bồn tắm", Value = "Bồn tắm" },
                        new SelectListItem { Text = "Wifi", Value = "Wifi" }
                    };
                    ViewBag.Error = "Có Mã Phòng đã tồn tại không thể sửa!";
                    return View(room);
                }

                if (room != null)
                {
                    room.Name = model.Name;
                    room.Type = model.Type;
                    room.FloorNumber = model.FloorNumber;
                    room.Introduce = model.Introduce;
                    room.Description = model.Description;
                    room.MAP = model.MAP;
                    room.DAP = model.DAP;
                    room.Price = model.Price;
                    room.Extension = SelectedExtensions != null ? string.Join(",", SelectedExtensions) : "";
                    _context.Rooms.Update(room);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("AllRoomList", "RoomManager", new { area = "Admin" });
                }
            }
            ViewBag.Error = "Thông tin không hợp lệ";
            return RedirectToAction("AllRoomList", "RoomManager", new { area = "Admin" });
        }
        public async Task<IActionResult> Delete(int id)
        {
            var room = _context.Rooms.FirstOrDefault(i => i.Id == id);
            if (room != null)
            {
                // Chỉ cho phép xóa nếu phòng đang Trống (hoặc Available)
                if (room.Status == "Trống" || room.Status == "Available")
                {
                    _context.Rooms.Remove(room);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("AllRoomList", "RoomManager", new { area = "Admin" });
                }
                else
                {
                    TempData["Error"] = "Chỉ có thể xóa phòng khi phòng đang ở trạng thái Trống.";
                    return RedirectToAction("AllRoomList", "RoomManager", new { area = "Admin" });
                }
            }
            return NotFound();
        }
        public IActionResult RoomStatus(DateTime? begin, DateTime? end)
        {
            DateTime b = DateTime.Now.Date;
            DateTime e = DateTime.Now.Date.AddDays(1);
            var list = _context.Rooms.Include(r => r.RoomImages).ToList();
            var bookings = _context.Bookings.Where(i =>
                   (i.Status == BookingStatus.Deposited
                         || i.Status == BookingStatus.CancelRequested
                         || i.Status == BookingStatus.CheckedIn) && // Loại bỏ booking đã hủy
                    ((i.CheckinDate.Date <= b && i.CheckoutDate.Date >= b)))
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)
                    .ToList();


            var roomBookingInfo = new Dictionary<int, (Booking booking, BookingDetail detail)>();

            foreach (var booking in bookings)
            {
                foreach (var detail in booking.BookingDetails)
                {
                    if (!roomBookingInfo.ContainsKey(detail.RoomId))
                    {
                        roomBookingInfo[detail.RoomId] = (booking, detail);
                    }
                }
            }

            var models = new List<DetailedRoomStatusViewModel>();
            foreach (var room in list)
            {
                var model = new DetailedRoomStatusViewModel
                {
                    Room = room,
                    CheckInDate = b,
                    CheckOutDate = e
                };
                model.Room = room;

                if (roomBookingInfo.ContainsKey(room.Id))
                {
                    var (booking, detail) = roomBookingInfo[room.Id];

                    model.IsAvailable = false;
                    model.CustomerName = booking.Customer?.FullName;
                    model.CustomerPhone = booking.Customer?.PhoneNumber;
                    model.CustomerEmail = booking.Customer?.Email;
                    model.BookingDate = booking.CreateDate;
                    model.BookedCheckInDate = booking.CheckinDate;
                    model.BookedCheckOutDate = booking.CheckoutDate;
                    model.TotalAmount = detail.Price * ((booking.CheckoutDate - booking.CheckinDate).Days);
                    model.BookingId = booking.Id;

                    model.BookingStatus = booking.Status switch
                    {
                        0 => "Deposited",
                        1 => "CancelRequested",
                        2 => "CheckedIn",
                        3 => "CheckedOut",
                        4 => "Cancelled",
                        _ => "Pending"
                    };

                    model.PaymentStatus = booking.Status switch
                    {
                        0 => "Đã cọc",
                        1 => "Đang chờ hoàn tiền",
                        2 => "Đã thanh toán",
                        3 => "Đã thanh toán",
                        4 => "Đã hoàn tiền",
                        _ => "Chưa thanh toán"
                    };

                    //model.IsDeposited = booking.Status >= 1;
                    model.DepositAmount = model.TotalAmount * 0.3; // 30% deposit
                }
                else
                {
                    model.IsAvailable = true;
                }
                models.Add(model);
            }

            ViewBag.BeginDate = b;
            ViewBag.EndDate = e;
            return View(models);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRoomStatus(int roomId, int action)
        {
            try
            {
                // cập nhật trạng thái phòng chỉ cho 3 trạng thái: Available, Occupied, Maintenance, khi thay đổi trạng thái đặt phòng thì đã đổi trạng thái phòng rồi, chức năng này chỉ để cập nhật trạng thái phòng thủ công
                var today = DateTime.Now.Date;

                var room = await _context.Rooms.FindAsync(roomId);
                switch (action)
                {
                    case 1:
                        room.Status = "Available";
                        break;
                    case 2:
                        room.Status = "Occupied";
                        break;
                    case 3:
                        room.Status = "Maintenance";
                        break;
                }

                _context.Rooms.Update(room);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật trạng thái thành công!";
            }
            catch (Exception ex)
            {
                TempData["Success"] = "Cập nhật trạng thái không thành công!";
                return RedirectToAction("Details", new { id = roomId });
            }

            return RedirectToAction("Details", new { id = roomId });
        }
        [HttpPost]
        public IActionResult RoomStatus(int time)
        {
            DateTime begin, end;
            switch (time)
            {
                case 2:
                    begin = DateTime.Now.Date.AddHours(6);
                    end = DateTime.Now.Date.AddHours(12);
                    break;
                case 3:
                    begin = DateTime.Now.Date.AddHours(12);
                    end = DateTime.Now.Date.AddHours(18);
                    break;
                case 4:
                    begin = DateTime.Now.Date.AddHours(18);
                    end = DateTime.Now.Date.AddHours(24);
                    break;
                default: begin = DateTime.Now; end = DateTime.Now; break;
            }
            return RedirectToAction("RoomStatus", new { begin = begin, end = end });
        }
        public IActionResult RoomDetail(int id)
        {
            var model = _context.Rooms.Find(id);
            return View(model);
        }

        // Details action for Admin area
        public IActionResult Details(int id)
        {
            var room = _context.Rooms
                .Include(r => r.RoomImages)
                .FirstOrDefault(r => r.Id == id);

            if (room == null)
                return NotFound();

            return View(room);
        }

        // UpdateStatus action for changing room status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int roomId, string newStatus, string notes)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null)
                {
                    TempData["Error"] = "Không tìm thấy phòng";
                    return RedirectToAction("AllRoomList");
                }

                // Note: Since Status is NotMapped, we'll handle status changes through booking management
                // For now, we'll just show a success message
                TempData["Success"] = $"Đã ghi nhận yêu cầu cập nhật trạng thái phòng {room.Name} thành '{newStatus}'";

                // TODO: Implement actual status management through booking system
                // This could involve creating maintenance records, updating booking status, etc.

                return RedirectToAction("Details", new { id = roomId });
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái phòng";
                return RedirectToAction("Details", new { id = roomId });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteImage([FromBody] string id)
        {
            var img = await _context.RoomImages.FindAsync(id);
            if (img == null) return Json(new { success = false, message = "Không tìm thấy ảnh." });

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, img.Path.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _context.RoomImages.Remove(img);
            await _context.SaveChangesAsync();

            return Json(new { success = true, imageId = id, message = "Xoá ảnh thành công." });
        }


        [HttpPost]
        public async Task<IActionResult> EditImages(int id, List<IFormFile> NewImages, List<string> DeletedImageIds)
        {
            var room = await _context.Rooms.Include(r => r.RoomImages).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();

            if (DeletedImageIds != null)
            {
                foreach (var path in DeletedImageIds)
                {
                    var img = await _context.RoomImages.FirstOrDefaultAsync(i => i.Path == path);
                    if (img != null)
                    {
                        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, img.Path.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                            System.IO.File.Delete(fullPath);

                        _context.RoomImages.Remove(img);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Thêm ảnh mới
            if (NewImages != null && NewImages.Count > 0)
            {
                foreach (var file in NewImages)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    string filePath = Path.Combine("img", fileName);
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    room.RoomImages.Add(new RoomImage
                    {
                        Path = "/" + filePath.Replace("\\", "/")
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công!" });
        }

    }
}
