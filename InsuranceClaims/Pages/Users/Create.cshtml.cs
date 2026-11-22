using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
namespace InsuranceClaims.Pages.Users
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {

        private readonly IConfiguration _configuration;
        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty]
        public string UserName { get; set; } = "";
        [BindProperty]
        public string Email { get; set; } = "";
        [BindProperty]
        public string Password { get; set; } = "";
        [BindProperty]
        public int RoleId { get; set; }
        public SelectList RoleList { get; set; } = default!;
        public string ErrorMessage { get; set; } = "";
        public void OnGet()
        {
            LoadRoles();
        }

        public IActionResult OnPost()
        {
            LoadRoles();
            if (Password.Length < 6)
            {
                ErrorMessage = "The password must be at least 6 characters.";
                return Page();
            }
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            string sql = @"INSERT INTO Users (userName, email, passwordHash, roleId, isActive, createdAt)
                       VALUES (@userName, @email, @passwordHash, @roleId, 1, GETDATE())";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userName", UserName);
            cmd.Parameters.AddWithValue("@email", Email);
            cmd.Parameters.AddWithValue("@passwordHash", BCrypt.Net.BCrypt.HashPassword(Password));
            cmd.Parameters.AddWithValue("@roleId", RoleId);
            conn.Open();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                ErrorMessage = "That username or email is already used.";
                return Page();
            }
            return RedirectToPage("Index");
        }
        private void LoadRoles()
        {
            var roles = new List<SelectListItem>();
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("SELECT roleId, roleName FROM Roles ORDER BY roleName", conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                roles.Add(new SelectListItem(reader["roleName"].ToString(), reader["roleId"].ToString()));
            }
            RoleList = new SelectList(roles, "Value", "Text");
        }
    }
}
