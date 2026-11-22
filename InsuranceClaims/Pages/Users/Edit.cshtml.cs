using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;


namespace InsuranceClaims.Pages.Users
{

    [Authorize(Roles = "Administrator")]
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty]
        public int UserId { get; set; }
        [BindProperty]
        public string UserName { get; set; } = "";
        [BindProperty]
        public string Email { get; set; } = "";
        [BindProperty]
        public int RoleId { get; set; }
        [BindProperty]
        public bool IsActive { get; set; }
        public SelectList RoleList { get; set; } = default!;
        public string ErrorMessage { get; set; } = "";
        public IActionResult OnGet(int userId)
        {
            LoadRoles();
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            string sql = @"SELECT userName, email, roleId, isActive
                           FROM Users WHERE userId = @userId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return RedirectToPage("Index");
            }
            UserId = userId;
            UserName = reader["userName"].ToString()!;
            Email = reader["email"].ToString()!;
            RoleId = Convert.ToInt32(reader["roleId"]);
            IsActive = Convert.ToBoolean(reader["isActive"]);
            return Page();
        }
        public IActionResult OnPost()
        {
            LoadRoles();
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Username and email are required.";
                return Page();
            }
            string? signedInId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (signedInId == UserId.ToString())
            {
                if (!IsActive)
                {
                    ErrorMessage = "You cannot turn off the account you are signed in with.";
                    return Page();
                }
                if (!RoleIsAdministrator(RoleId))
                {
                    ErrorMessage = "You cannot remove your own Administrator role.";
                    return Page();
                }
            }
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            string sql = @"UPDATE Users
                           SET userName = @userName, email = @email, roleId = @roleId, isActive = @isActive
                           WHERE userId = @userId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userName", UserName.Trim());
            cmd.Parameters.AddWithValue("@email", Email.Trim());
            cmd.Parameters.AddWithValue("@roleId", RoleId);
            cmd.Parameters.AddWithValue("@isActive", IsActive);
            cmd.Parameters.AddWithValue("@userId", UserId);
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
        private bool RoleIsAdministrator(int roleId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("SELECT roleName FROM Roles WHERE roleId = @roleId", conn);
            cmd.Parameters.AddWithValue("@roleId", roleId);
            conn.Open();
            return string.Equals(cmd.ExecuteScalar()?.ToString(), "Administrator", StringComparison.OrdinalIgnoreCase);
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
