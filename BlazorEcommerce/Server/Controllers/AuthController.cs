using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlazorEcommerce.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ServiceResponse<int>>> Register(UserRegister request)
        {
            var response = await _authService.Register(
                new User
                {
                    Email = request.Email,
                    UserName = request.UserName,
                },
                request.Password);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ServiceResponse<string>>> Login(UserLogin request)
        {
            var response = await _authService.Login(request.Email, request.Password);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ServiceResponse<bool>>> ForgotPassword(UserLogin user)
        {
            var response = await _authService.ForgotPassword(user);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ServiceResponse<bool>>> PasswordReset([FromBody] UserChangePassword userChangePassword)
        {
            var response = await _authService.PasswordReset(userChangePassword);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPost("confirm-email")]
        public async Task<ActionResult<ServiceResponse<bool>>> EmailConfirmation([FromBody] string validate)
        {
            if (!string.IsNullOrWhiteSpace(validate))
            {
                EmailConfirmation? emailConfirmation = null;
                if (Json.TryParseJson<EmailConfirmation>(validate, out emailConfirmation))
                {
                    var response = await _authService.EmailConfirmation(emailConfirmation);
                    if (!response.Success)
                    {
                        return BadRequest(response);
                    }
                    return Ok(response);
                }
                return BadRequest("Invaid request data");

            }
            return BadRequest("Missing request parameter");
        }


        [HttpPost("change-password"), Authorize]
        public async Task<ActionResult<ServiceResponse<bool>>> ChangePassword([FromBody] string newPassword)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var response = await _authService.ChangePassword(int.Parse(userId), newPassword);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}