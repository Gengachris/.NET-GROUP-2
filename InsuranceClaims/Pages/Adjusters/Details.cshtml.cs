
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InsuranceClaims.Pages.Adjusters
{
    [Authorize(Roles = "Administrator")]
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int AdjusterId { get; set; }
        public string Name { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string BranchName { get; set; } = "";
        public bool IsActive { get; set; }

        public List<ClaimRow> Claims { get; set; } = new();

        public IActionResult OnGet(int adjusterId)
        {
            string connectionString =
                _configuration.GetConnectionString("InsuranceClaims")!;

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            // Get adjuster information
            using (var cmd = new SqlCommand("usp_Adjusters_Get", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@adjusterId", SqlDbType.Int).Value = adjusterId;

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound();
                }

                AdjusterId = adjusterId;
                Name = reader["firstName"] + " " + reader["lastName"];
                LicenseNumber = reader["licenseNumber"].ToString()!;
                Email = reader["email"].ToString()!;
                Phone = reader["phone"].ToString()!;
                IsActive = Convert.ToBoolean(reader["isActive"]);
            }

            // Get assigned claims
            using (var cmd = new SqlCommand("usp_Adjuster_Claims", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@adjusterId", SqlDbType.Int).Value = adjusterId;

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Claims.Add(new ClaimRow
                    {
                        ClaimNumber = reader["claimNumber"].ToString()!,
                        Status = reader["status"].ToString()!,
                        ClaimAmount = Convert.ToDecimal(reader["claimAmount"])
                    });
                }
            }

            return Page();
        }

        public class ClaimRow
        {
            public string ClaimNumber { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal ClaimAmount { get; set; }
        }
    }
}
