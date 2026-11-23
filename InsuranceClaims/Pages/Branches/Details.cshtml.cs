using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Branches
{
    [Authorize(Roles = "Administrator,Claims Manager,Claims Adjuster,Branch Manager,Finance Officer")]
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public BranchRow? Branch { get; set; }
        public List<AdjusterRow> Adjusters { get; set; } = new();
        public List<ClaimRow> Claims { get; set; } = new();

        public void OnGet(int branchId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var branchCmd = new SqlCommand(@"SELECT branchNumber, branchName, location, phone, email, isActive
                                                   FROM Branches WHERE branchId = @branchId", conn);
            branchCmd.Parameters.AddWithValue("@branchId", branchId);
            conn.Open();
            using (var reader = branchCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    Branch = new BranchRow
                    {
                        BranchNumber = reader["branchNumber"].ToString()!,
                        BranchName = reader["branchName"].ToString()!,
                        Location = reader["location"].ToString()!,
                        Phone = reader["phone"].ToString()!,
                        Email = reader["email"].ToString()!,
                        IsActive = Convert.ToBoolean(reader["isActive"])
                    };
                }
            }

            if (Branch == null)
                return;

            using var adjusterCmd = new SqlCommand(@"SELECT firstName, lastName, licenseNumber, email, phone, isActive
                                                     FROM Adjusters WHERE branchId = @branchId ORDER BY lastName, firstName", conn);
            adjusterCmd.Parameters.AddWithValue("@branchId", branchId);
            using (var reader = adjusterCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Adjusters.Add(new AdjusterRow
                    {
                        Name = reader["firstName"] + " " + reader["lastName"],
                        LicenseNumber = reader["licenseNumber"].ToString()!,
                        Email = reader["email"].ToString()!,
                        Phone = reader["phone"].ToString()!,
                        IsActive = Convert.ToBoolean(reader["isActive"])
                    });
                }
            }

            using var claimsCmd = new SqlCommand(@"SELECT claimNumber, claimDate, status, claimAmount
                                                   FROM Claims WHERE branchId = @branchId ORDER BY claimDate DESC", conn);
            claimsCmd.Parameters.AddWithValue("@branchId", branchId);
            using var claimsReader = claimsCmd.ExecuteReader();
            while (claimsReader.Read())
            {
                Claims.Add(new ClaimRow
                {
                    ClaimNumber = claimsReader["claimNumber"].ToString()!,
                    ClaimDate = Convert.ToDateTime(claimsReader["claimDate"]),
                    Status = claimsReader["status"].ToString()!,
                    ClaimAmount = Convert.ToDecimal(claimsReader["claimAmount"])
                });
            }
        }

        public class BranchRow
        {
            public string BranchNumber { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string Location { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public bool IsActive { get; set; }
        }

        public class AdjusterRow
        {
            public string Name { get; set; } = "";
            public string LicenseNumber { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public bool IsActive { get; set; }
        }

        public class ClaimRow
        {
            public string ClaimNumber { get; set; } = "";
            public DateTime ClaimDate { get; set; }
            public string Status { get; set; } = "";
            public decimal ClaimAmount { get; set; }
        }
    }
}
