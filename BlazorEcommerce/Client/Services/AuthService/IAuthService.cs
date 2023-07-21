namespace BlazorEcommerce.Client.Services.AuthService
{
    public interface IAuthService
    {
        Task<ServiceResponse<int>> Register(UserRegister request);
        Task<ServiceResponse<string>> Login(UserLogin request);
        Task<ServiceResponse<bool>> ChangePassword(UserChangePassword request);
        Task<ServiceResponse<bool>> PasswordReset(UserChangePassword request);
        Task<ServiceResponse<bool>> ForgotPassword(UserLogin request);
        Task<ServiceResponse<bool>> ConfrimEmail(string confirmation);
        Task<bool> IsUserAuthenticated();
    }
}
