using Microsoft.AspNetCore.Identity;

namespace BlazorEcommerce.Shared
{
    public class User : IdentityUser<int>
    {
        public new byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public Address Address { get; set; } = null!;
        public string Role { get; set; } = "Customer";

        public Guid? VerificationKey { get; set; }

        public DateTimeOffset? VerificationExpiry { get; set; }
    }
}