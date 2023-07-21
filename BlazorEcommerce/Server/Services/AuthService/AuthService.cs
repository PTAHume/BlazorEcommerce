using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BlazorEcommerce.Server.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(DataContext context,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetUserId() => int.Parse(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));

        public string GetUserEmail() => _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);

        public async Task<ServiceResponse<string>> Login(string email, string password)
        {
            var response = new ServiceResponse<string>();
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email.ToLower().Equals(email.ToLower()));
            if (user == null)
            {
                response.Success = false;
                response.Message = "User not found.";
            }
            else if (!user.EmailConfirmed)
            {
                response.Success = false;
                response.Message = "You have not confirmed your e-mail address, please check your emails for the email verification.";
            }
            else if (user.LockoutEnabled && DateTimeOffset.UtcNow < user.LockoutEnd && user.AccessFailedCount > 2)
            {
                response.Success = false;
                response.Message = "Account has been locked due to too many failed log in attempts please try again later.";
            }
            else if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                response.Success = false;
                response.Message = "Wrong password.";
                if (user.LockoutEnabled && DateTimeOffset.UtcNow > user.LockoutEnd)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
                    user.AccessFailedCount = 1;
                    await _context.SaveChangesAsync();
                    response.Message = "Wrong password.";
                }
                else if (user.AccessFailedCount > 2)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(7);
                    response.Message = "Account has been locked due to too many failed log in attempts please try again later.";
                    user.LockoutEnabled = true;
                }
                else
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
                    user.AccessFailedCount++;
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                if (user.LockoutEnd == null || user.AccessFailedCount > 0)
                {
                    user.LockoutEnd = null;
                    user.AccessFailedCount = 0;
                    await _context.SaveChangesAsync();
                }
                response.Data = CreateToken(user);
            }

            return response;
        }

        public async Task<ServiceResponse<int>> Register(User user, string password)
        {
            if (await UserExists(user.Email))
            {
                return new ServiceResponse<int>
                {
                    Success = false,
                    Message = "User already exists."
                };
            }

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.EmailConfirmed = false;
            user.VerificationKey = Guid.NewGuid();
            user.VerificationExpiry = DateTimeOffset.Now.AddDays(1);

            var emailConfirmation = new EmailConfirmation()
            {
                VerificationKey = user?.VerificationKey ?? Guid.NewGuid(),
                Email = user.Email
            };
            string templateData = await System.IO.File.ReadAllTextAsync($"{System.IO.Directory.GetParent(Directory.GetCurrentDirectory())}{@"\Client\wwwroot\templates\confirm-email.html"}");
            string resetURl = $"https://localhost:7226/email-confirmation?DataVerification={WebUtility.UrlEncode(Newtonsoft.Json.JsonConvert.SerializeObject(emailConfirmation))}";
            templateData = templateData.Replace("{{LINK}}", resetURl);

            // string body = $"please click the link <a target=\"_blank\" rel=\"noopener noreferrer\"  href='https://localhost:7226/email-confirmation?DataVerification={WebUtility.UrlEncode(Newtonsoft.Json.JsonConvert.SerializeObject(emailConfirmation))}'>click this link</a>";

            await sendEmail(user, templateData);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new ServiceResponse<int> { Data = user.Id, Message = "Registration successful!" };
        }

        public async Task<bool> UserExists(string email)
        {
            if (await _context.Users.AnyAsync(user => user.Email.ToLower()
                 .Equals(email.ToLower())))
            {
                return true;
            }
            return false;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac
                    .ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash =
                    hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8
                .GetBytes(_configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        public async Task<ServiceResponse<bool>> ChangePassword(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            return await UpdateUserPassword(newPassword, user);
        }

        private async Task<ServiceResponse<bool>> UpdateUserPassword(string newPassword, User? user)
        {
            if (user == null)
            {
                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.SaveChangesAsync();

            return new ServiceResponse<bool> { Data = true, Message = "Password has been changed." };
        }

        public async Task<ServiceResponse<bool>> EmailConfirmation(EmailConfirmation emailConfirmation)
        {
            User? user = await _context.Users
                .Where(user => user!.Email == emailConfirmation.Email)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new ServiceResponse<bool> { Success = false, Message = "User not found." };
            }
            else if (user.EmailConfirmed)
            {
                return new ServiceResponse<bool> { Success = false, Message = "User email has already been validated, you can now log into your account." };
            }
            else if (user.LockoutEnabled)
            {
                return new ServiceResponse<bool> { Success = false, Message = "User account has been locked out, try again later." };
            }
            else
            {
                if (user?.VerificationKey == emailConfirmation.VerificationKey && DateTimeOffset.Now < user.VerificationExpiry)
                {
                    user.VerificationExpiry = null;
                    user.EmailConfirmed = true;
                    user.VerificationKey = null;
                    await _context.SaveChangesAsync();
                    return new ServiceResponse<bool> { Success = true, Message = "You have successfully verified your e-mail address, you can now log into your account." };
                }
                return new ServiceResponse<bool> { Success = false, Message = DateTimeOffset.Now > user.VerificationExpiry ? "The validation key has expired." : "Validation key is not valid for this user." };
            }
        }

        public async Task<ServiceResponse<bool>> ForgotPassword(UserLogin userLogin)
        {
            User? user = await _context.Users
              .Where(x => x.Email.ToLower().Equals(userLogin.Email.ToLower())).FirstOrDefaultAsync();
            if (user != null)
            {
                var emailConfirmation = new EmailConfirmation()
                {
                    VerificationKey = Guid.NewGuid(),
                    Email = user.Email,
                    UserName = user.UserName
                };

                user.VerificationExpiry = DateTimeOffset.Now.AddHours(7);
                user.VerificationKey = emailConfirmation.VerificationKey;

                await _context.SaveChangesAsync();

                string templateData = await System.IO.File.ReadAllTextAsync($"{System.IO.Directory.GetParent(Directory.GetCurrentDirectory())}{@"\Client\wwwroot\templates\password-reset.html"}");
                string resetURl = $"https://localhost:7226/password-reset?DataVerification={WebUtility.UrlEncode(Newtonsoft.Json.JsonConvert.SerializeObject(emailConfirmation))}";
                templateData = templateData.Replace("{{LINK}}", resetURl);

                // string body = $"please click the link <a target=\"_blank\" rel=\"noopener noreferrer\" href='https://localhost:7226/password-reset?DataVerification={WebUtility.UrlEncode(Newtonsoft.Json.JsonConvert.SerializeObject(emailConfirmation))}'>click this link</a>";

                await sendEmail(user, templateData);

                return new ServiceResponse<bool> { Success = true, Message = "A password reset email was sent, please check your emails." };
            }
            return new ServiceResponse<bool> { Success = false, Message = "No user found with that e-mail address, please check and try again." };
        }

        public async Task<ServiceResponse<bool>> PasswordReset(UserChangePassword userChangePassword)
        {
            EmailConfirmation emailConfirmation = null;
            if (Json.TryParseJson<EmailConfirmation>(userChangePassword.ConfirmData, out emailConfirmation))
            {
                User? user = await _context.Users
               .Where(user => user!.Email == emailConfirmation.Email)
               .FirstOrDefaultAsync();

                if (user != null)
                {
                    if (user?.VerificationKey == emailConfirmation!.VerificationKey && DateTimeOffset.Now < user!.VerificationExpiry)
                    {
                        user.VerificationExpiry = null;
                        user.EmailConfirmed = true;
                        user.VerificationKey = null;
                        user.LockoutEnabled = false;
                        user.LockoutEnd = null;
                        return await UpdateUserPassword(userChangePassword.Password, user);
                    }
                    return new ServiceResponse<bool> { Success = false, Message = DateTimeOffset.Now > user!.VerificationExpiry ? "The validation key has expired, please try resetting you password again." : "Validation key is not valid for this user, please try resetting you password again." };
                }
                return new ServiceResponse<bool> { Success = false, Message = "User not found." };
            }
            return new ServiceResponse<bool> { Success = false, Message = "Invalid e-mail validation key, please try resetting you password again." };
        }

        public async Task<User> GetUserByEmail(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));
        }

        private async Task sendEmail(User user, string body)
        {
            try
            {
                const string fromEmail = "Blazor@Ecommerce.com";
                MailMessage mailMessage = new MailMessage(fromEmail, user.Email, "Subject", body);
                mailMessage.IsBodyHtml = true;
                SmtpClient smtpClient = new SmtpClient("localhost", 26);
                smtpClient.EnableSsl = false;
                smtpClient.UseDefaultCredentials = true;
                // smtpClient.Credentials = new NetworkCredential(fromEmail, "Password");
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}