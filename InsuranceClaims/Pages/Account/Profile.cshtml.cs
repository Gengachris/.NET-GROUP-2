using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;


namespace InsuranceClaims.Pages.Account
{

    [Authorize]
    public class ProfileModel : PageModel
    {

        private readonly IConfiguration _configuration;
        public ProfileModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty]
        public string CurrentPassword { get; set; } = "";
        [BindProperty]
        public string NewPassword { get; set; } = "";
        [BindProperty]
        public string ConfirmPassword { get; set; } = "";
        public string Message { get; set; } = "";
        public void OnGet()
        {
        }


        public IActionResult OnPost()
        {
            if (NewPassword.Length < 6 || NewPassword != ConfirmPassword)
            {
                Message = "The new password must match and be at least 6 characters.";
                return Page();
            }
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            string connectionString = _configuration.GetConnectionString("InsuranceClaims")!;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string storedHash = "";
                using (SqlCommand read = new SqlCommand("SELECT passwordHash FROM Users WHERE userId = @userId", conn))
                {
                    read.Parameters.AddWithValue("@userId", userId);
                    storedHash = read.ExecuteScalar()?.ToString() ?? "";
                }
                if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, storedHash))
                {
                    Message = "The current password is wrong.";
                    return Page();
                }
                using (SqlCommand update = new SqlCommand("UPDATE Users SET passwordHash = @hash WHERE userId = @userId", conn))
                {
                    update.Parameters.AddWithValue("@hash", BCrypt.Net.BCrypt.HashPassword(NewPassword));
                    update.Parameters.AddWithValue("@userId", userId);
                    update.ExecuteNonQuery();
                }
            }
            Message = "Password saved.";
            return Page();
        }
    }
}
