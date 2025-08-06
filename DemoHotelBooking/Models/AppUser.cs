using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DemoHotelBooking.Models
{
    public class AppUser : IdentityUser
    {
        [Display(Name ="Họ và tên")]
        public string FullName { get; set; }

        public string? CCCD { get; set; }

        public bool? Gender { get; set; }

        public string? Address { get; set; }

        public DateTime? DoB { get; set; }

        public bool IsRegisted { get; set; } //đã đăng ký hay chưa (để đánh dấu khách vãng lai)
    }
}
