using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Assignment.Models
{
    public class SignUpVM   
    {
        [Key]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
        [MaxLength(100)]
        public string Username { get; set; }
        [StringLength(12, MinimumLength = 8, ErrorMessage = "The password must be between 8 and 12 characters long.")]
        public string Password { get; set; }
        [Compare("Password", ErrorMessage = "The password is not matching.")]
        [DisplayName("Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
    // ViewModel for displaying a table dynamically in a view with optional/automatic pagination
    public class TableListingViewModel
    {
        public List<string> Headers { get; set; } = [];
        public List<List<string>> Rows { get; set; } = [];
        public X.PagedList.IPagedList<List<string>>? PagedRows { get; set; }
        public int PageSize { get; set; } = 10;
    }

    // ViewModel for the reusable AJAX search component
    // Fields: Key = Entity Column Name (from DB.cs), Value = Display Name (shown in UI dropdown)
    public class AjaxSearchViewModel
    {
        public Dictionary<string, string> Fields { get; set; } = [];
        public string? Url { get; set; }
        public string Target { get; set; } = "#table-container";
    }

    //staff details/edit view model
    public class StaffDetailsViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string AvatarIcon { get; set; }
        public AccountStatusType Status { get; set; }
        public string? BlockingReason { get; set; }
        public string? BlockBy { get; set; }
    }

    public class StaffEditViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }
        public string AvatarIcon { get; set; }
        public bool RemoveAvatar { get; set; }

        [Required(ErrorMessage = "Please select an account status.")]
        [EnumDataType(typeof(AccountStatusType), ErrorMessage = "Invalid status value.")]
        public AccountStatusType Status { get; set; }
        public string? BlockingReason { get; set; }
    }

    public class RoomCreateViewModel
    {
        [Required(ErrorMessage = "Room Number is required.")]
        [MaxLength(20)]
        [DisplayName("Room Number")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Room Type is required.")]
        [DisplayName("Room Type")]
        public int RoomTypeId { get; set; }
    }

    public class RoomEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Room Number is required.")]
        [MaxLength(20)]
        [DisplayName("Room Number")]
        public string RoomNumber { get; set; }

        [Required(ErrorMessage = "Room Type is required.")]
        [DisplayName("Room Type")]
        public int RoomTypeId { get; set; }
    }

    public class RoomDetailsViewModel
    {
        public int Id { get; set; }
        [DisplayName("Room Number")]
        public string RoomNumber { get; set; }
        [DisplayName("Room Type")]
        public string RoomTypeName { get; set; }
        [DisplayName("Base Price")]
        public decimal BasePrice { get; set; }
    }

    public class RoomTypeCreateViewModel
    {
        [Required(ErrorMessage = "Room Type Name is required.")]
        [MaxLength(50)]
        [DisplayName("Room Type Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Base Price is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Base Price must be greater than 0.")]
        [DisplayName("Base Price")]
        public decimal BasePrice { get; set; }

        [Required(ErrorMessage = "Service Category is required.")]
        [DisplayName("Service Category")]
        public int ServiceCategoryId { get; set; }
    }

    public class RoomTypeEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Room Type Name is required.")]
        [MaxLength(50)]
        [DisplayName("Room Type Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Base Price is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Base Price must be greater than 0.")]
        [DisplayName("Base Price")]
        public decimal BasePrice { get; set; }

        [DisplayName("Service Category")]
        public string? ServiceCategoryName { get; set; }
    }

    public class ServiceCreateViewModel
    {
        [Required(ErrorMessage = "Service Name is required.")]
        [MaxLength(100)]
        [DisplayName("Service Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be greater than 0.")]
        [DisplayName("Price (MYR)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        [DisplayName("Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Service Category is required.")]
        [DisplayName("Service Category")]
        public int ServiceCategoryId { get; set; }
    }

    public class ServiceEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Service Name is required.")]
        [MaxLength(100)]
        [DisplayName("Service Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [DisplayName("Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be greater than 0.")]
        [DisplayName("Price (MYR)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        [DisplayName("Duration (Minutes)")]
        public int DurationMinutes { get; set; }

        [DisplayName("Service Category")]
        public string? ServiceCategoryName { get; set; }
    }

    public class ServiceDetailsViewModel
    {
        public int Id { get; set; }
        [DisplayName("Service Name")]
        public string Name { get; set; }
        [DisplayName("Description")]
        public string Description { get; set; }
        [DisplayName("Price")]
        public decimal Price { get; set; }
        [DisplayName("Duration (Minutes)")]
        public int DurationMinutes { get; set; }
        [DisplayName("Service Category")]
        public string ServiceCategoryName { get; set; }
        [DisplayName("Created At")]
        public DateTime CreatedAt { get; set; }
    }

    public class BookingCreateViewModel
    {
        [Required(ErrorMessage = "Customer is required.")]
        [DisplayName("Customer")]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Staff is required.")]
        [DisplayName("Assigned Staff")]
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Service is required.")]
        [DisplayName("Service")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Room is required.")]
        [DisplayName("Room")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Start Time is required.")]
        [DisplayName("Start Time")]
        public DateTime StartTime { get; set; } = DateTime.Now.AddHours(1);

        [Required(ErrorMessage = "Pokemon Name is required.")]
        [MaxLength(50)]
        [DisplayName("Pokemon Name")]
        public string PokemonName { get; set; }

        [MaxLength(255)]
        [DisplayName("Notes / Special Requests")]
        public string? Notes { get; set; }
    }

    public class BookingDetailsViewModel
    {
        public int Id { get; set; }
        [DisplayName("Customer Name")]
        public string CustomerName { get; set; }
        [DisplayName("Customer Email")]
        public string CustomerEmail { get; set; }
        [DisplayName("Pokemon Name")]
        public string PokemonName { get; set; }
        [DisplayName("Special Notes")]
        public string? Notes { get; set; }
        [DisplayName("Service Name")]
        public string ServiceName { get; set; }
        [DisplayName("Service Category")]
        public string ServiceCategoryName { get; set; }
        [DisplayName("Room Number")]
        public string RoomNumber { get; set; }
        [DisplayName("Room Type")]
        public string RoomTypeName { get; set; }
        [DisplayName("Assigned Staff")]
        public string StaffName { get; set; }
        [DisplayName("Start Time")]
        public DateTime StartTime { get; set; }
        [DisplayName("End Time")]
        public DateTime EndTime { get; set; }
        [DisplayName("Status")]
        public BookingStatus Status { get; set; }
        [DisplayName("Total Price")]
        public decimal TotalPrice { get; set; }
        [DisplayName("Created At")]
        public DateTime CreatedAt { get; set; }
    }
}
