using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebInterface.Data;
using WebInterface.Models;
using WebInterface.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace WebInterface.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, AppDbContext context,
    IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
            _emailService = emailService;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return Unauthorized("Account locked due to multiple failed attempts. Try again after 5 minutes.");
            }

            if (!result.Succeeded)
            {
                return Unauthorized("Invalid email or password.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var token = GenerateJwtToken(user, roles);

            return Ok(new
            {
                token,
                email = user.Email,
                employeeId = user.EmployeeId,
                roles
            });
        }
        public class InviteRequest
        {
            public int EmployeeId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        [Authorize(Roles = "HR")]
        [HttpPost("invite")]
        public async Task<IActionResult> Invite(InviteRequest request)
        {
            var employee = await _context.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
                return NotFound("Employee not found.");

            var alreadyLinked = await _userManager.Users
                .AnyAsync(u => u.EmployeeId == request.EmployeeId);
            if (alreadyLinked)
                return BadRequest("This employee already has a login account.");

            var oldInvites = _context.Invites.Where(i => i.EmployeeId == request.EmployeeId && !i.IsUsed);
            _context.Invites.RemoveRange(oldInvites);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var invite = new Invite
            {
                EmployeeId = employee.Id,
                Email = request.Email,
                Token = Guid.NewGuid().ToString("N"),
                Role = request.Role,
                ExpiryDate = DateTime.UtcNow.AddHours(24),
                IsUsed = false,
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = currentUserId
            };

            _context.Invites.Add(invite);
            await _context.SaveChangesAsync();

            var mvcBaseUrl = _configuration["MvcBaseUrl"];
            var inviteLink = $"{mvcBaseUrl}/Account/Register?token={invite.Token}";

            await _emailService.SendInviteEmailAsync(request.Email, employee.Name, inviteLink);

            return Ok(new { message = "Invite sent successfully.", expiresAt = invite.ExpiryDate });
        }
        public class RegisterRequest
        {
            public string Token { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var invite = await _context.Invites.FirstOrDefaultAsync(i => i.Token == request.Token);

            if (invite == null)
                return NotFound("Invalid invite link.");

            if (invite.IsUsed)
                return BadRequest("This invite has already been used.");

            if (invite.ExpiryDate < DateTime.UtcNow)
                return BadRequest("This invite has expired.");

            var user = new ApplicationUser
            {
                UserName = invite.Email,
                Email = invite.Email,
                EmailConfirmed = true,
                EmployeeId = invite.EmployeeId
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, invite.Role);

            invite.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok("Account created successfully. You can now log in.");
        }
        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            };

            if (user.EmployeeId.HasValue)
            {
                claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}