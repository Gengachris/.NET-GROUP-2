
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InsuranceClaims.Pages.Adjusters
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
        public int UserId { get; set; }

        [BindProperty]
        public int BranchId { get; set; }

        [BindProperty]
        public string FirstName { get; set; } = "";

        [BindProperty]
        public string LastName { get; set; } = "";

        [BindProperty]
        public string LicenseNumber { get; set; } = "";

        [BindProperty]
        public string Email { get; set; } = "";

        [BindProperty]
        public string Phone { get; set; } = "";

        public List<SelectListItem> Users { get; set; } = new();

        public List<SelectListItem> Branches { get; set; } = new();

        public string ErrorMessage { get; set; } = "";

        private string ConnectionString =>
            _configuration.GetConnectionString("InsuranceClaims")!;

        public void OnGet()
        {
            LoadDropdowns();
        }

        private void LoadDropdowns()
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            using (var cmd = new SqlCommand(@"
                SELECT u.userId, u.userName
                FROM Users u
                INNER JOIN Roles r ON u.roleId = r.roleId
                WHERE r.roleName = N'Claims Adjuster'
                  AND u.isActive = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM Adjusters a
                      WHERE a.userId = u.userId
                  )
                ORDER BY u.userName;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Users.Add(new SelectListItem
                    {
                        Value = reader["userId"].ToString(),
                        Text = reader["userName"].ToString()
                    });
                }
            }

            using (var cmd = new SqlCommand(@"
                SELECT branchId, branchName
                FROM Branches
                WHERE isActive = 1
                ORDER BY branchName;", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Branches.Add(new SelectListItem
                    {
                        Value = reader["branchId"].ToString(),
                        Text = reader["branchName"].ToString()
                    });
                }
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return Page();
            }

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                using var cmd = new SqlCommand("usp_Adjusters_Save", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@userId", SqlDbType.Int).Value = UserId;
                cmd.Parameters.Add("@firstName", SqlDbType.NVarChar, 50).Value = FirstName;
                cmd.Parameters.Add("@lastName", SqlDbType.NVarChar, 50).Value = LastName;
                cmd.Parameters.Add("@licenseNumber", SqlDbType.NVarChar, 50).Value = LicenseNumber;
                cmd.Parameters.Add("@email", SqlDbType.NVarChar, 100).Value = Email;
                cmd.Parameters.Add("@phone", SqlDbType.NVarChar, 20).Value = Phone;
                cmd.Parameters.Add("@branchId", SqlDbType.Int).Value = BranchId;

                conn.Open();
                cmd.ExecuteNonQuery();

                return RedirectToPage("Index");
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Message;
                LoadDropdowns();
                return Page();
            }
        }
    }
}
