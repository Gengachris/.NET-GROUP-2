using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty]
        public string LoginName { get; set; } = "";
        [BindProperty]
        public string Password { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string connectionString = _configuration.GetConnectionString("InsuranceClaims")!;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string sql = @"SELECT u.userId, u.userName, u.passwordHash, r.roleName
                           FROM Users u
                           INNER JOIN Roles r ON u.roleId = r.roleId
                           WHERE (u.userName = @login OR u.email = @login)
                             AND u.isActive = 1";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@login", LoginName);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ErrorMessage = "Username or password is wrong, or the account is inactive.";
                            return Page();
                        }
                        string hashedPasswordFromDb = reader["passwordHash"].ToString()!;
                        if (!BCrypt.Net.BCrypt.Verify(Password, hashedPasswordFromDb))
                        {
                            ErrorMessage = "Username or password is wrong, or the account is inactive.";
                            return Page();
                        }
                        var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, reader["userId"].ToString()!),
                        new Claim(ClaimTypes.Name, reader["userName"].ToString()!),
                        new Claim(ClaimTypes.Role, reader["roleName"].ToString()!)
                    };
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(identity));
                    }
                }
            }
            return RedirectToPage("/Index");
        }
    }
}
