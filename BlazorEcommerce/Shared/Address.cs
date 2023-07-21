using System.ComponentModel.DataAnnotations;

namespace BlazorEcommerce.Shared
{
    public class Address
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        [Required]
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;
    }
}
