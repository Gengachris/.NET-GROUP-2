
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Adjusters
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Row> Rows { get; set; } = new();

        public string Message { get; set; } = "";

        public void OnGet()
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("InsuranceClaims"));

            using var cmd = new SqlCommand("usp_Adjusters_List", conn);

            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            conn.Open();

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Rows.Add(new Row
                {
                    AdjusterId = Convert.ToInt32(reader["adjusterId"]),
                    Name = reader["firstName"] + " " + reader["lastName"],
                    LicenseNumber = reader["licenseNumber"].ToString()!,
                    BranchName = reader["branchName"].ToString()!,
                    UserName = reader["userName"].ToString()!,
                    IsActive = Convert.ToBoolean(reader["isActive"])
                });
            }
        }

        public IActionResult OnPostRemove(int adjusterId)
        {
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("InsuranceClaims"));

            using var cmd = new SqlCommand("usp_Adjusters_Remove", conn);

            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@adjusterId", adjusterId);

            conn.Open();

            cmd.ExecuteNonQuery();

            Message = "The adjuster was deleted, or turned off because that person already has work.";

            OnGet();

            return Page();
        }

        public class Row
        {
            public int AdjusterId { get; set; }
            public string Name { get; set; } = "";
            public string LicenseNumber { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string UserName { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}
