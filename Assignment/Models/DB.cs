using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Assignment.Models;
//enum------------------------------------------------------------------------------------------
public enum Provider { Google, Local, Both }
public enum AccountStatusType { blocked, active, deleted }
public enum BookingStatus { pending, confirmed, completed, cancelled }
public enum PaymentStatus { Succeeded, Processing, Failed, Cancelled }

#nullable disable warnings
public class DB(DbContextOptions options) : DbContext(options)
{
    //ACCOUNT--------------------------------------------------
    public DbSet<Role> Roles { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<AccountDetail> AccountDetails { get; set; }
    public DbSet<AccountStatus> AccountStatuses { get; set; }
    public DbSet<Staff> Staffs { get; set; }

    //SERVICE-------------------------------------------------------
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<Service> Services { get; set; }
    //ROOM----------------------------------------------------------
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    //BOOKING----------------------------------------------------------
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingDetail> BookingDetails { get; set; }
    //PAYMENT----------------------------------------------------------
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentDetail> PaymentDetails { get; set; }
    //this is to prevent accidental deletion of related data when a parent entity is deleted
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Staff)
            .WithMany(s => s.HandledBookings)
            .HasForeignKey(b => b.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Account)
            .WithMany(a => a.Bookings)
            .HasForeignKey(b => b.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Service)
            .WithMany()
            .HasForeignKey(b => b.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany()
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//entity class------------------------------------------------------------------------------------------
public class Role
{
    //columns
    public int Id { get; set; }
    [MaxLength(50)]
    public string RoleName { get; set; }

    //navigational properties
    public List<AccountDetail> AccountDetails { get; set; } = [];
}
public class Account
{
    //columns
    public int Id { get; set; }
    public Provider Provider { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [MaxLength(255)]
    public string Email { get; set; }
    [MaxLength(255)]
    public string PasswordHash { get; set; }
    [MaxLength(255)]
    public string GoogleId { get; set; }

    //navigational properties
    public AccountDetail AccountDetail { get; set; }
    public AccountStatus AccountStatus { get; set; }
    public Staff Staff { get; set; }
    public List<Booking> Bookings { get; set; } = [];
}
public class AccountDetail
{
    //columns
    [Key]
    public int AccountId { get; set; }
    public int RoleId { get; set; }
    [MaxLength(100)]
    public string Username { get; set; }
    [MaxLength(255)]
    public string AvatarIcon { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //navigational properties
    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; }
    public Role Role { get; set; }
}

public class AccountStatus
{
    //columns
    [Key]
    public int AccountId { get; set; }
    public AccountStatusType Status { get; set; }
    public string? BlockingReason { get; set; }
    [MaxLength(50)]
    public string? BlockBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    //navigational properties
    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; }
}
public class Staff
{
    //columns
    public int Id { get; set; }
    public int AccountId { get; set; }

    //navigational properties
    public Account Account { get; set; }
    public List<Booking> HandledBookings { get; set; } = [];
}
public class ServiceCategory
{
    //columns
    public int Id { get; set; }
    [MaxLength(50)]
    public string Name { get; set; }
    public string Description { get; set; }

    //navigational properties
    public List<Service> Services { get; set; } = [];
    public List<RoomType> RoomTypes { get; set; } = [];
}
public class Service
{
    //columns
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    public string Description { get; set; }
    [Precision(10, 2)]
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //navigational properties
    public ServiceCategory ServiceCategory { get; set; }
}
public class RoomType
{
    //columns
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; }
    public string Description { get; set; }
    [Precision(10, 2)]
    public decimal BasePrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //navigational properties
    public ServiceCategory ServiceCategory { get; set; }
    public List<Room> Rooms { get; set; } = [];
}
public class Room
{
    //columns
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    [MaxLength(20)]
    public string RoomNumber { get; set; }
    public bool IsDeleted { get; set; }

    //navigational properties
    public RoomType RoomType { get; set; }
}
public class Booking
{
    //columns
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int ServiceId { get; set; }
    public int AccountId { get; set; }
    public int StaffId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.pending;
    [Precision(10, 2)]
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //navigational properties
    public Room Room { get; set; }
    public Service Service { get; set; }
    public Account Account { get; set; }
    public Staff Staff { get; set; }
    public BookingDetail BookingDetail { get; set; }
    public List<Payment> Payments { get; set; } = [];
}
public class BookingDetail
{
    //columns
    [Key]
    public int BookingId { get; set; }
    [MaxLength(50)]
    public string PokemonName { get; set; }
    public string? Notes { get; set; }

    //navigational properties
    [ForeignKey(nameof(BookingId))]
    public Booking Booking { get; set; }
}
public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    [MaxLength(255)]
    public string? PaymentMethodId { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    [Precision(10, 2)]
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    //navigational properties
    public Booking Booking { get; set; }
    public PaymentDetail PaymentDetail { get; set; }
}
public class PaymentDetail
{
    //columns
    [Key]
    public int PaymentId { get; set; }
    [MaxLength(100)]
    public string CardHolder { get; set; }
    [MaxLength(50)]
    public string CardBrand { get; set; }
    [StringLength(4, MinimumLength = 4)]
    public string CardLast4 { get; set; }

    //navigational properties
    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; }
}