using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoHotelBooking.ViewModels
{
    public class RoomViewModel : IValidatableObject

    {
        public int? Id { get; set; }

        [Display(Name = "Mã phòng")]
        public string Name { get; set; } //mã phòng (STD..., SUP..., DLX..., SUT) 

        [Display(Name = "Loại phòng")]
        public string Type { get; set; } //loại phòng (Standart, Superio, Deluxe, Suite)

        [Range(1, 100)]
        [Display(Name = "Lầu")]
        public int FloorNumber { get; set; } //Số lầu

        [Required]
        [Display(Name = "Giá phòng")]
        public double Price { get; set; }

        public string? ImagePath { get; set; } //đường dẫn ảnh

        [Display(Name = "Giới thiệu")]
        public string? Introduce { get; set; } //nội dung giới thiệu
        [Display(Name = "Chi tiết phòng")]
        public string? Description { get; set; } //Mô tả

        [Display(Name = "Khung cảnh")]
        public string? Visio { get; set; } // View núi, view biển

        [Required]
        [Display(Name = "Số người qui định")]
        public int DAP { get; set; } // Default Amount of people (Số người mặc định)
        [Required]
        [Display(Name = "Số người tối đa")]
        public int MAP { get; set; } // Maximum Amount of people (Số người tối đa)
        [Display(Name = "Tiện ích phòng")]
        public string Extension { get; set; }
        public string? Status { get; set; }
        public string? StatusColor { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? CheckinDate { get; set; }
        public DateTime? CheckoutDate { get; set; }
        public int BookingStatus { get; set; }
        public List<IFormFile> NewImages { get; set; }
        public List<int> DeletedImageIds { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DAP < 0)
            {
                yield return new ValidationResult(
                    "Số người quy định (DAP) không được là số âm.",
                    new[] { nameof(DAP) }
                );
            }
            if (MAP < 0)
            {
                yield return new ValidationResult(
                    "Số người tối đa (MAP) không được là số âm.",
                    new[] { nameof(MAP) }
                );
            }
            if (DAP >= MAP)
            {
                yield return new ValidationResult(
                    "Số người quy định (DAP) phải nhỏ hơn hoặc bằng số người tối đa (MAP).",
                    new[] { nameof(DAP), nameof(MAP) }
                );
            }
        }
    }
}

