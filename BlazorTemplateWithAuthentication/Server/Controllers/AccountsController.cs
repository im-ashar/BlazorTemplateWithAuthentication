using BlazorTemplateWithAuthentication.Client.Pages;
using BlazorTemplateWithAuthentication.Server.Data;
using BlazorTemplateWithAuthentication.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using shared = BlazorTemplateWithAuthentication.Shared;

namespace BlazorTemplateWithAuthentication.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AccountsController(UserManager<AppUser> userManager,
                                  SignInManager<AppUser> signInManager,
                                  IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] shared.SignUp model)
        {
            var newUser = new AppUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(newUser, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(x => x.Description);
                return Ok(new SignUpResult { Successful = false, Errors = errors });
            }
            await _userManager.AddToRoleAsync(newUser, "User");
            return Ok(new SignUpResult { Successful = true });
        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] shared.Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);
            if (!result.Succeeded)
            {
                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    return BadRequest(new LoginResult { Successful = false, Error = "You Should Confirm Your Email To Login" });
                }
                return BadRequest(new LoginResult { Successful = false, Error = "Username Or Password Is Invalid" });
            }
            //use when we dont have roles
            /*var claims = new[] { new Claim(ClaimTypes.Name, model.Email) };*/

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, model.Email) };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSecurityKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(Convert.ToInt32(_configuration["JwtExpiryInDays"]));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtAudience"],
                claims,
                expires: expiry,
                signingCredentials: creds
            );

            return Ok(new LoginResult { Successful = true, Token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}
