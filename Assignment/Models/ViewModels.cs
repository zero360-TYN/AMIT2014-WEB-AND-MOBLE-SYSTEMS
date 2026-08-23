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
    // ViewModel for displaying a table dynamically in a view
    public class TableListingViewModel
    {
        public List<string> Headers { get; set; } = [];
        public List<List<string>> Rows { get; set; } = [];
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
}
