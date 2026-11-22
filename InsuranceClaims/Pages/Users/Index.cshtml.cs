using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Users
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel : PageModel
    {

        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public List<UserRow> Users { get; set; } = new();
        public string Message { get; set; } = "";
        public void OnGet()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            string sql = @"SELECT u.userId, u.userName, u.email, u.isActive, r.roleName
                           FROM Users u
                           INNER JOIN Roles r ON u.roleId = r.roleId
                           ORDER BY u.userName";
            using var cmd = new SqlCommand(sql, conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Users.Add(new UserRow
                {
                    UserId = Convert.ToInt32(reader["userId"]),
                    UserName = reader["userName"].ToString()!,
                    Email = reader["email"].ToString()!,
                    IsActive = Convert.ToBoolean(reader["isActive"]),
                    RoleName = reader["roleName"].ToString()!
                });
            }
        }
        public IActionResult OnPostRemove(int userId)
        {
            string? signedInId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (signedInId == userId.ToString())
            {
                Message = "You cannot delete the account you are signed in with.";
                OnGet();
                return Page();
            }
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            conn.Open();
            string countSql = @"SELECT
                (SELECT COUNT(*) FROM Adjusters WHERE userId = @userId) +
                (SELECT COUNT(*) FROM ClaimHistory WHERE changedBy = @userId) +
                (SELECT COUNT(*) FROM Payments WHERE recordedBy = @userId)";
            int used = 0;
            using (var count = new SqlCommand(countSql, conn))
            {
                count.Parameters.AddWithValue("@userId", userId);
                used = (int)count.ExecuteScalar()!;
            }
            if (used > 0)
            {
                using var turnOff = new SqlCommand("UPDATE Users SET isActive = 0 WHERE userId = @userId", conn);
                turnOff.Parameters.AddWithValue("@userId", userId);
                turnOff.ExecuteNonQuery();
                Message = "This user already has work, so the account was turned off instead of deleted.";
            }
            else
            {
                using var delete = new SqlCommand("DELETE FROM Users WHERE userId = @userId", conn);
                delete.Parameters.AddWithValue("@userId", userId);
                delete.ExecuteNonQuery();
                Message = "User deleted.";
            }
            OnGet();
            return Page();
        }
        public class UserRow
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public string RoleName { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}



