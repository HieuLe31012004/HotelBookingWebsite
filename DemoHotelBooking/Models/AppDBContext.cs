using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DemoHotelBooking.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<ReportRevenue> ReportRevenues { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //Bỏ tiền tố aspnet các bảng Identity
            builder.Entity<AppUser>(entity => { entity.ToTable(name: "Users"); });
            builder.Entity<IdentityRole>(entity => { entity.ToTable(name: "Roles"); });
            builder.Entity<IdentityUserRole<string>>(entity => { entity.ToTable("UserRoles"); });

            builder.Entity<BookingDetail>().HasKey(i => new { i.BookingId, i.RoomId });
            builder.Entity<InvoiceDetail>().HasKey(i => new { i.InvoiceId, i.RoomId });
            builder.Entity<Feedback>().HasKey(i => new { i.CusId});

            builder.Entity<RoomImage>()
                .HasOne(i => i.Room)
                .WithMany()
                .HasForeignKey(i => i.RoomId);

            builder.Entity<Booking>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CusID);

            builder.Entity<BookingDetail>()
                .HasOne(i => i.Booking)
                .WithMany(b => b.BookingDetails)
                .HasForeignKey(i => i.BookingId);

            builder.Entity<BookingDetail>()
                .HasOne(i => i.Room)
                .WithMany()
                .HasForeignKey(i => i.RoomId);

            builder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithMany(b => b.Invoices)
                .HasForeignKey(i => i.BookingId);

            builder.Entity<Invoice>()
                .HasOne(i => i.Receptionist)
                .WithMany()
                .HasForeignKey(i => i.ReceptionistId);

            builder.Entity<InvoiceDetail>()
                .HasOne(i => i.Invoice)
                .WithMany()
                .HasForeignKey(i => i.InvoiceId);

            builder.Entity<InvoiceDetail>()
                .HasOne(i => i.Room)
                .WithMany()
                .HasForeignKey(i => i.RoomId);

            builder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithOne()
                .HasForeignKey<Feedback>(f => f.CusId);

            builder.Entity<RoomImage>()
                .HasOne(i => i.Room)
                .WithMany(x=> x.RoomImages)
                .HasForeignKey(i => i.RoomId);

            builder.Entity<ReportRevenue>()
                .HasOne(i => i.Accountant)
                .WithMany()
                .HasForeignKey(i => i.AccId);
        }
    }
}
