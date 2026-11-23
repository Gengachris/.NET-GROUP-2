
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InsuranceClaims.Pages.Adjusters
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
        public int AdjusterId { get; set; }

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

        [BindProperty]
        public bool IsActive { get; set; }

        public List<SelectListItem> Users { get; set; } = new();

        public List<SelectListItem> Branches { get; set; } = new();

        public string ErrorMessage { get; set; } = "";

        private string ConnectionString =>
            _configuration.GetConnectionString("InsuranceClaims")!;

        public void OnGet(int adjusterId)
        {
            AdjusterId = adjusterId;
            LoadLists();

            using var conn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand("usp_Adjusters_Get", conn);

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@adjusterId", SqlDbType.Int).Value = adjusterId;

            conn.Open();

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                Response.StatusCode = 404;
                return;
            }

            UserId = Convert.ToInt32(reader["userId"]);
            FirstName = reader["firstName"].ToString()!;
            LastName = reader["lastName"].ToString()!;
            LicenseNumber = reader["licenseNumber"].ToString()!;
            Email = reader["email"].ToString()!;
            Phone = reader["phone"].ToString()!;
            BranchId = Convert.ToInt32(reader["branchId"]);
            IsActive = Convert.ToBoolean(reader["isActive"]);
        }

        private void LoadLists()
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
                        AND a.adjusterId <> @adjusterId
                  )
                ORDER BY u.userName;", conn))
            {
                cmd.Parameters.Add("@adjusterId", SqlDbType.Int)
                    .Value = AdjusterId;

                using var reader = cmd.ExecuteReader();

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
            LoadLists();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                using var cmd = new SqlCommand(
                    "usp_Adjusters_Update", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@adjusterId", SqlDbType.Int).Value = AdjusterId;
                cmd.Parameters.Add("@userId", SqlDbType.Int).Value = UserId;
                cmd.Parameters.Add("@firstName", SqlDbType.NVarChar, 50).Value = FirstName;
                cmd.Parameters.Add("@lastName", SqlDbType.NVarChar, 50).Value = LastName;
                cmd.Parameters.Add("@licenseNumber", SqlDbType.NVarChar, 50).Value = LicenseNumber;
                cmd.Parameters.Add("@email", SqlDbType.NVarChar, 100).Value = Email;
                cmd.Parameters.Add("@phone", SqlDbType.NVarChar, 20).Value = Phone;
                cmd.Parameters.Add("@branchId", SqlDbType.Int).Value = BranchId;
                cmd.Parameters.Add("@isActive", SqlDbType.Bit).Value = IsActive;

                conn.Open();
                cmd.ExecuteNonQuery();

                return RedirectToPage("Index");
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }
    }
}
