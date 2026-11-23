using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Approvals
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<PendingClaim> Claims { get; set; } = new();
        public void OnGet()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("usp_Approvals_List", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Claims.Add(new PendingClaim
                {
                    ClaimId = Convert.ToInt32(reader["claimId"]),
                    ClaimNumber = reader["claimNumber"].ToString()!,
                    ClaimDate = Convert.ToDateTime(reader["claimDate"]),
                    PolicyHolderName = reader["policyHolderName"].ToString()!,
                    BranchName = reader["branchName"].ToString()!,
                    ClaimAmount = Convert.ToDecimal(reader["claimAmount"])
                });
            }
        }
        public class PendingClaim
        {
            public int ClaimId { get; set; }
            public string ClaimNumber { get; set; } = "";
            public DateTime ClaimDate { get; set; }
            public string PolicyHolderName { get; set; } = "";
            public string BranchName { get; set; } = "";
            public decimal ClaimAmount { get; set; }
        }
    }
}

