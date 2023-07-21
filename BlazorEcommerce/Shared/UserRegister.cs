using System.ComponentModel.DataAnnotations;

namespace BlazorEcommerce.Shared
{
    public class UserRegister
    {
        [Required, StringLength(100, MinimumLength = 6)]
        public string UserName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 7)]
        [RegularExpression(@"^(?=.*\d.*)(?=.*[a-z].*)(?=.*[A-Z].*)(?=.*\W.*).{7,}$", ErrorMessage = "Password must contain at least: 1 symbol, 1 numerical, 1 upper case and 1 lower case character")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
