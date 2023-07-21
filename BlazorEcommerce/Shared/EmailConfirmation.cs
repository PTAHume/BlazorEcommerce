namespace BlazorEcommerce.Shared
{
    public class EmailConfirmation
    {
        public Guid VerificationKey { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }
    }
}