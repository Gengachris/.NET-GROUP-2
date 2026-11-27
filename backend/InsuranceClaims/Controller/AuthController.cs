using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InsuranceClaims.Data;
using InsuranceClaims.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
namespace InsuranceClaims.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly InsuranceDbContext _db;
        private readonly IConfiguration _configuration;
        public AuthController(InsuranceDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var login = request.userName.Trim();
            var user = await _db.Users
                .Include(row => row.role)
                .FirstOrDefaultAsync(row => row.userName == login || row.email == login);
            if (user == null || !user.isActive || user.role == null
                || !BCrypt.Net.BCrypt.Verify(request.password, user.passwordHash))
            {
                return Unauthorized(new { message = "Username or password is wrong, or the account is inactive." });
            }
            return Ok(new LoginResponse
            {
                token = CreateToken(user),
                userId = user.userId,
                userName = user.userName,
                role = user.role.roleName
            });
        }

        private string CreateToken(Users user)
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json.");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.userId.ToString()),
                new(ClaimTypes.Name, user.userName),
                new(ClaimTypes.Role, user.role!.roleName)
            };
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}