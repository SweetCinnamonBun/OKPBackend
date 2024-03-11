using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using OKPBackend.Models.Domain;
using OKPBackend.Models.DTO.Users;
using OKPBackend.Repositories.Users;
using OKPBackend.Services;

namespace OKPBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration config;
        private readonly IMapper mapper;
        private readonly UserManager<User> userManager;
        private readonly EmailService emailService;
        private readonly IUsersRepository usersRepository;

        public AccountController(IConfiguration config, IMapper mapper, UserManager<User> userManager, EmailService emailService, IUsersRepository usersRepository)
        {
            this.userManager = userManager;
            this.emailService = emailService;
            this.usersRepository = usersRepository;
            this.mapper = mapper;
            this.config = config;

        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        {
            if (ModelState.IsValid)
            {

                var user = await userManager.FindByEmailAsync(userLoginDto.Email);

                if (user == null)
                {
                    return BadRequest("Salasana tai sähköpostisi on väärä.");
                }

                if (user.EmailConfirmed == false)
                {
                    return BadRequest("Vahvista sähköpostisi kirjautuaksesi sisään.");
                }

                if (user != null)
                {
                    var checkPasswordResult = await userManager.CheckPasswordAsync(user, userLoginDto.Password);

                    if (checkPasswordResult)
                    {
                        var roles = await userManager.GetRolesAsync(user);

                        if (roles != null)
                        {
                            var jwtToken = usersRepository.CreateJWTToken(user, roles.ToList());

                            var response = new LoginResponseDto
                            {
                                JwtToken = jwtToken
                            };

                            return Ok(response);
                        }

                    }
                }
            }


            return BadRequest("Salasana tai sähköpostisi on väärä.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userRegisterDto)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest("Käytitkö oikeaa sähköpostiosoitetta!");
            }

            //Checks if passwords match
            if (userRegisterDto.Password != userRegisterDto.ConfirmPassword)
            {
                return BadRequest("Salasanat eivät täsmää.");
            }

            if (userRegisterDto.Roles == null || userRegisterDto.Roles.Length == 0)
            {
                return BadRequest("A role must be assigned for a user.");
            }

            var newUser = new User
            {
                Email = userRegisterDto.Email,
                UserName = userRegisterDto.Username,
                PasswordHash = userRegisterDto.Password
            };

            //Creates a new user
            var identityResult = await userManager.CreateAsync(newUser, userRegisterDto.Password);

            //Checks if the user was created successfully
            if (identityResult.Succeeded)
            {
                //Checks if the client provided roles to the user.
                if (userRegisterDto.Roles != null && userRegisterDto.Roles.Any())
                {
                    //Adds new roles to the user
                    var rolesResponse = await userManager.AddToRolesAsync(newUser, userRegisterDto.Roles);
                }
                else
                {
                    return BadRequest("You have to assign at least one role to the user!");
                }
            }
            else
            {
                return BadRequest("Rekisteröityminen ei onnistunut: " + "\n" + string.Join(" ", identityResult.Errors.Select(e => e.Description)));
            }

            try
            {
                if (await SendConfirmEmailAsync(newUser))
                {
                    return Ok(new JsonResult(new { title = "Account Created", message = "Your account has been created, please confirm your email address" }));
                }
            }
            catch (System.Exception)
            {
                return BadRequest("Email failed");
            };


            return BadRequest("Jotain meni vikaan.");
        }

        [HttpPut("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            // Finds the user based on the email provided.
            var user = await userManager.FindByEmailAsync(confirmEmailDto.Email);

            // If no user was found, it returns an error stating that the email has not been registered.
            if (user == null)
            {
                return Unauthorized("Tämä sähköposti ei ole rekisteröity.");
            }

            // Checks if email was already confirmed.
            if (user.EmailConfirmed == true)
            {
                return BadRequest("Sähköpostisi on jo vahvistettu. Voit kirjatua sisään.");
            }

            // Decodes the token that was passed in from the client.
            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(confirmEmailDto.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);
                var result = await userManager.ConfirmEmailAsync(user, decodedToken);

                if (result.Succeeded)
                {
                    return Ok(new JsonResult(new { title = "Email Confirmed", message = "Your email address is confirmed. You can login now" }));
                }

            }
            catch (System.Exception e)
            {
                return BadRequest(e.Message);
            }

            return BadRequest("Jotain meni vikaan. Kirjoita sähköpostisi uudestaan alla olevaan kenttään saadaksesi uuden sähkoposti linkin.");


        }

        [HttpPost("resend-email-confirmation-link/{email}")]
        public async Task<IActionResult> ResendEmailConformationLink(string email)
        {
            // Checks if the query parameter was empty
            if (string.IsNullOrEmpty(email)) return BadRequest("Please provide an email address");

            // Finds the user based on the email address provided.
            var user = await userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("Tämä sähköposti ei ole rekisteröitynyt vielä.");
            if (user.EmailConfirmed == true) return BadRequest("Olet jo vahvistanut sähköpostiosoitteesi. Voit kirjatua sisään.");

            try
            {
                if (await SendConfirmEmailAsync(user))
                {
                    return Ok(new JsonResult(new { title = "Confirmation link sent", message = "Please confirm your email address" }));
                }

                return BadRequest("Sähköpostin lähettäminen epäonnistui.");
            }
            catch (System.Exception)
            {

                return BadRequest("Sähköpostin lähettäminen epäonnistui.");
            }

        }

        [HttpPost("forgot-password/{email}")]

        public async Task<IActionResult> ForgotUsernameOrPassword(string email)
        {
            if (string.IsNullOrEmpty(email) || email == "") return BadRequest("Invalid email");

            var user = await userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized("Tätä sähköpostia ei ole vielä rekisteröity.");
            if (user.EmailConfirmed == false) return BadRequest("Vahvista sähköpostisi ensin.");

            try
            {
                if (await SendForgotUsernameOrPasswordEmail(user))
                {
                    return Ok("Sähköposti viesti lähetetty.");
                }

                return BadRequest("Sähköpostiviestin lähettäminen epäonnistui");
            }
            catch (System.Exception)
            {

                return BadRequest("Sähköpostiviestin lähettäminen epäonnistui");
            }
        }

        [HttpPut("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            if (resetPasswordDto.NewPassword.IsNullOrEmpty() || resetPasswordDto.ConfirmNewPassword.IsNullOrEmpty())
            {
                return BadRequest("Please provide a new password and confirm password");
            }
            var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null) return Unauthorized("This email address has not been registered yet");
            if (user.EmailConfirmed == false) return BadRequest("Please confirm your email address first");

            try
            {
                var decodedTokenBytes = WebEncoders.Base64UrlDecode(resetPasswordDto.Token);
                var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

                var result = await userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
                if (result.Succeeded)
                {
                    return Ok("Salasanan vaihtaminen onnistui!");
                }

                return BadRequest("Salasana ei täyty vaatimuksia.");
            }
            catch (System.Exception)
            {

                return BadRequest("Salasana ei täyty vaatimuksia.");
            }
        }



        //HELPER METHODS
        private async Task<bool> SendConfirmEmailAsync(User user)
        {
            DotNetEnv.Env.Load();
            string confirm_email_path = Environment.GetEnvironmentVariable("confirm_email_path");
            string reset_password_path = Environment.GetEnvironmentVariable("reset_password_path");
            string application_name = Environment.GetEnvironmentVariable("application_name");

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            Console.WriteLine(user.Email);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"http://localhost:5173/confirm-email?token={token}&email={user.Email}";

            var body = $"<p>Hei: {user.UserName}</p> + <p>Vahvista sähköpostiosoitteesi painamalla allaolevaa linkkiä</p>" +
                        $"<p><a href=\"{url}\">Paina tästä</a></p>" +
                        "<p>Kiitos</p>" +
                        $"<br>{application_name}";

            var emailSend = new EmailSendDto(user.Email, "Vahvista sähköpostiositteesi", body);

            return await emailService.SendEmailAsync(emailSend);


        }

        private async Task<bool> SendForgotUsernameOrPasswordEmail(User user)
        {
            DotNetEnv.Env.Load();
            string reset_password_path = Environment.GetEnvironmentVariable("reset_password_path");
            string application_name = Environment.GetEnvironmentVariable("application_name");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = $"http://localhost:5173/reset-password?token={token}&email={user.Email}";

            var body = $"<p>Hei: {user.UserName}</p>" + "<p>Paina allaolevaa linkkiä vaihtaakseksi salasanasi</p>" +
                        $"<p><a href=\"{url}\">Paina tästä</a></p>" +
                        "<p>Kiitos</p>" +
                        $"<br>{application_name}";

            var emailSend = new EmailSendDto(user.Email, "Vaihda salasana", body);

            return await emailService.SendEmailAsync(emailSend);
        }

    }
}