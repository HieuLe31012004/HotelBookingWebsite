using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Models
{
    public static class SeedData
    {
        public static async Task Initialize(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IApplicationBuilder app)
        {
            // Tạo vai trò mặc định nếu chưa tồn tại
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Customer"));
            }
            // Tạo tài khoản admin mặc định nếu chưa tồn tại
            if (userManager.Users.All(u => u.UserName != "admin"))
            {
                var admin = new AppUser { UserName = "admin", Email = "admin@example.com", FullName = "Quản trị viên", PhoneNumber = "0123456789" };
                var result = await userManager.CreateAsync(admin, "admin");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Danh sách các khách hàng cần thêm
            var customers = new List<AppUser>
            {
                 new AppUser { UserName = "an",FullName = "An", Email = "customer1@example.com", PhoneNumber="0933912012" },
                 new AppUser { UserName = "hao",FullName = "Hào", Email = "customer2@example.com",PhoneNumber="0271444444" },
                 new AppUser { UserName = "manh",FullName = "Mạnh", Email = "customer3@example.com",PhoneNumber="0369872113" },
                 new AppUser { UserName = "tuan",FullName = "Tuấn", Email = "customer5@example.com",PhoneNumber="0909090909" },
             };
            var password = "123123";

            foreach (var customer in customers)
            {
                if (userManager.Users.All(u => u.UserName != customer.UserName))
                {
                    var result = await userManager.CreateAsync(customer, password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(customer, "Customer");
                    }
                }
            }

            AppDbContext context = app.ApplicationServices.
                CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
            if (!context.Rooms.Any())
            {
                context.AddRange(
                new Room
                {
                    Name = "STD101",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 500000,
                    Introduce = "Phòng tiêu chuẩn với giường đơn.",
                    Description = "Phòng tiêu chuẩn với một giường đơn, phòng tắm riêng và view núi.",
                    DAP = 1,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD102",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                                new Room
                                {
                                    Name = "STD103",
                                    Type = "Standard",
                                    FloorNumber = 1,
                                    Price = 550000,
                                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                                    DAP = 2,
                                    MAP = 2
                                },
                new Room
                {
                    Name = "STD104",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD105",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD106",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD107",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD108",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD109",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "STD110",
                    Type = "Standard",
                    FloorNumber = 1,
                    Price = 550000,
                    Introduce = "Phòng tiêu chuẩn với giường đôi.",
                    Description = "Phòng tiêu chuẩn với một giường đôi, phòng tắm riêng và view biển.",
                    DAP = 2,
                    MAP = 2
                },
                new Room
                {
                    Name = "SUP201",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 700000,
                    Introduce = "Phòng superior với giường đôi và tiện nghi hiện đại.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm rộng rãi và view biển.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP202",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP203",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP204",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP205",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP206",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 700000,
                    Introduce = "Phòng superior với giường đôi và tiện nghi hiện đại.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm rộng rãi và view biển.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP207",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP208",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP209",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "SUP210",
                    Type = "Superior",
                    FloorNumber = 2,
                    Price = 750000,
                    Introduce = "Phòng superior với giường đôi và view núi.",
                    Description = "Phòng superior với một giường đôi lớn, phòng tắm riêng và view núi.",
                    DAP = 2,
                    MAP = 3
                },
                new Room
                {
                    Name = "DLX301",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view biển.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view biển.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX302",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX303",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX304",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX305",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX306",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view biển.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view biển.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX307",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX308",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX309",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "DLX310",
                    Type = "Deluxe",
                    FloorNumber = 3,
                    Price = 1000000,
                    Introduce = "Phòng deluxe với giường đôi và view núi.",
                    Description = "Phòng deluxe với một giường đôi lớn, phòng tắm riêng và ban công rộng view núi.",
                    DAP = 2,
                    MAP = 4
                },
                new Room
                {
                    Name = "SUT401",
                    Type = "Suite",
                    FloorNumber = 4,
                    Price = 1200000,
                    Introduce = "Phòng suite với phòng khách riêng và view biển.",
                    Description = "Phòng suite với một giường đôi lớn, phòng tắm riêng, phòng khách và ban công view biển.",
                    DAP = 2,
                    MAP = 5
                },
                new Room
                {
                    Name = "SUT402",
                    Type = "Suite",
                    FloorNumber = 4,
                    Price = 1250000,
                    Introduce = "Phòng suite với phòng khách riêng và view núi.",
                    Description = "Phòng suite với một giường đôi lớn, phòng tắm riêng, phòng khách và ban công view núi.",
                    DAP = 2,
                    MAP = 5
                },
                new Room
                {
                    Name = "SUT403",
                    Type = "Suite",
                    FloorNumber = 4,
                    Price = 1200000,
                    Introduce = "Phòng suite với phòng khách riêng và view biển.",
                    Description = "Phòng suite với một giường đôi lớn, phòng tắm riêng, phòng khách và ban công view biển.",
                    DAP = 2,
                    MAP = 5
                },
                new Room
                {
                    Name = "SUT404",
                    Type = "Suite",
                    FloorNumber = 4,
                    Price = 1250000,
                    Introduce = "Phòng suite với phòng khách riêng và view núi.",
                    Description = "Phòng suite với một giường đôi lớn, phòng tắm riêng, phòng khách và ban công view núi.",
                    DAP = 2,
                    MAP = 5
                },
                new Room
                {
                    Name = "SUT405",
                    Type = "Suite",
                    FloorNumber = 4,
                    Price = 1200000,
                    Introduce = "Phòng suite với phòng khách riêng và view biển.",
                    Description = "Phòng suite với một giường đôi lớn, phòng tắm riêng, phòng khách và ban công view biển.",
                    DAP = 2,
                    MAP = 5
                },
                new Room
                {
                    Name = "SUT406",
                    Type = "Suite",
                    FloorNumber = 4,
                    Price = 1250000,
                    Introduce = "Phòng suite với phòng khách riêng và view núi.",
                    Description = "Phòng suite với một giường đôi lớn, phòng tắm riêng, phòng khách và ban công view núi.",
                    DAP = 2,
                    MAP = 5
                }
            );
                context.SaveChanges();
            }

            // Seed RoomImages if none exist
            if (!context.RoomImages.Any())
            {
                var rooms = context.Rooms.ToList();
                var roomImages = new List<RoomImage>();

                foreach (var room in rooms.Take(10)) // Add images for first 10 rooms
                {
                    // Add default image for each room type
                    string defaultImage = room.Type switch
                    {
                        "Standard" => "/img/room-1.jpg",
                        "Superior" => "/img/room-2.jpg", 
                        "Deluxe" => "/img/room-3.jpg",
                        "Suite" => "/img/room-1.jpg",
                        _ => "/img/room-1.jpg"
                    };

                    roomImages.Add(new RoomImage
                    {
                        Path = defaultImage,
                        RoomId = room.Id,
                        IsDefault = true
                    });

                    // Add additional images
                    roomImages.Add(new RoomImage
                    {
                        Path = "/img/about-1.jpg",
                        RoomId = room.Id,
                        IsDefault = false
                    });

                    roomImages.Add(new RoomImage
                    {
                        Path = "/img/about-2.jpg",
                        RoomId = room.Id,
                        IsDefault = false
                    });
                }

                context.RoomImages.AddRange(roomImages);
                context.SaveChanges();
            }
        }
    }
}
