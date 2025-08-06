using DemoHotelBooking.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DemoHotelBooking.ViewModels
{
    public class BookingViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Số điện thoại phải có từ 10-15 ký tự")]
        public string? Phone { get; set; } // sdt khách hàng

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Tên phải có từ 2-50 ký tự")]
        public string? Name { get; set; }

        public double? Deposit { get; set; } //tiền cọc

        public DateTime CheckinDate { get; set; } //ngày nhận dự kiến

        public DateTime CheckoutDate { get; set; } // ngày trả dự kiến

        public double? Amount { get; set; } //chi phí tổng

        public List<Room> SelectedRooms { get; set; }

        public List<Room> AvailbleRooms { get; set; }

        public AppUser? Customer { get; set; }
        
        // Thêm các thuộc tính cho tính toán giá chi tiết
        public double BasePrice { get; set; } // Giá gốc trước giảm giá
        public double MemberDiscount { get; set; } // Ưu đãi thành viên 
        public double VAT { get; set; } // Thuế VAT
        public double FinalPrice { get; set; } // Giá cuối cùng
        public int NumberOfNights { get; set; } // Số đêm
        public int NumberOfRooms { get; set; } // Số phòng

        public BookingViewModel()
        {
            SelectedRooms = new List<Room>();
            AvailbleRooms = new List<Room>();
            CheckinDate = DateTime.Now;
            CheckoutDate = DateTime.Now;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate Name
            if (!string.IsNullOrEmpty(Name))
            {
                // Chỉ cho phép chữ cái, dấu cách và các ký tự tiếng Việt
                var nameRegex = @"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵýỷỹ\s]+$";
                
                if (!Regex.IsMatch(Name.Trim(), nameRegex))
                {
                    results.Add(new ValidationResult("Tên chỉ được chứa chữ cái và dấu cách", new[] { nameof(Name) }));
                }
                
                if (Name.Trim().Length < 2)
                {
                    results.Add(new ValidationResult("Tên phải có ít nhất 2 ký tự", new[] { nameof(Name) }));
                }
            }

            // Validate Phone
            if (!string.IsNullOrEmpty(Phone))
            {
                // Regex cho số điện thoại Việt Nam
                var phoneRegex = @"^(0|84|\+84)([3-9])([0-9]{8})$";
                var cleanPhone = Regex.Replace(Phone, @"[\s\-\(\)]", ""); // Loại bỏ dấu cách, gạch ngang, ngoặc
                
                // Kiểm tra chỉ chứa số và các ký tự cho phép
                if (!Regex.IsMatch(Phone, @"^[0-9+\s\-\(\)]+$"))
                {
                    results.Add(new ValidationResult("Số điện thoại chỉ được chứa số, dấu +, dấu cách, gạch ngang và dấu ngoặc đơn", new[] { nameof(Phone) }));
                }
                else if (!Regex.IsMatch(cleanPhone, phoneRegex))
                {
                    results.Add(new ValidationResult("Số điện thoại không đúng định dạng (VD: 0901234567 hoặc +84901234567)", new[] { nameof(Phone) }));
                }
            }

            // Validate dates
            if (CheckinDate < DateTime.Now.Date)
            {
                results.Add(new ValidationResult("Ngày nhận phòng không được trong quá khứ", new[] { nameof(CheckinDate) }));
            }

            if (CheckoutDate <= CheckinDate)
            {
                results.Add(new ValidationResult("Ngày trả phòng phải sau ngày nhận phòng", new[] { nameof(CheckoutDate) }));
            }

            return results;
        }
    }
}
