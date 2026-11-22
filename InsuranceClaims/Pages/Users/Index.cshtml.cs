using Microsoft.AspNetCore.Authorization;
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
        public void OnGet()
        {

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            string sql = @"SELECT u.userName, u.email, u.isActive, r.roleName
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
                    UserName = reader["userName"].ToString()!,
                    Email = reader["email"].ToString()!,
                    IsActive = Convert.ToBoolean(reader["isActive"]),
                    RoleName = reader["roleName"].ToString()!
                });
            }
        }

        public class UserRow
        {
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public string RoleName { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}
