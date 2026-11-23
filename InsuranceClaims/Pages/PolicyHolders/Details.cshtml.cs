using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.PolicyHolders
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PolicyHolderRow? PolicyHolder { get; set; }
        public List<ClaimRow> Claims { get; set; } = new();

        public void OnGet(int policyHolderId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var holderCmd = new SqlCommand(@"SELECT firstName, lastName, email, phone, policyNumber, address
                                                   FROM PolicyHolders WHERE policyHolderId = @policyHolderId", conn);
            holderCmd.Parameters.AddWithValue("@policyHolderId", policyHolderId);
            conn.Open();
            using (var reader = holderCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    PolicyHolder = new PolicyHolderRow
                    {
                        FirstName = reader["firstName"].ToString()!,
                        LastName = reader["lastName"].ToString()!,
                        Email = reader["email"].ToString()!,
                        Phone = reader["phone"].ToString()!,
                        PolicyNumber = reader["policyNumber"].ToString()!,
                        Address = reader["address"].ToString()!
                    };
                }
            }

            if (PolicyHolder == null)
                return;

            using var claimsCmd = new SqlCommand(@"SELECT c.claimNumber, c.claimDate, c.status, c.claimAmount, b.branchName
                                                   FROM Claims c
                                                   INNER JOIN Branches b ON c.branchId = b.branchId
                                                   WHERE c.policyHolderId = @policyHolderId
                                                   ORDER BY c.claimDate DESC", conn);
            claimsCmd.Parameters.AddWithValue("@policyHolderId", policyHolderId);
            using var claimsReader = claimsCmd.ExecuteReader();
            while (claimsReader.Read())
            {
                Claims.Add(new ClaimRow
                {
                    ClaimNumber = claimsReader["claimNumber"].ToString()!,
                    ClaimDate = Convert.ToDateTime(claimsReader["claimDate"]),
                    Status = claimsReader["status"].ToString()!,
                    ClaimAmount = Convert.ToDecimal(claimsReader["claimAmount"]),
                    BranchName = claimsReader["branchName"].ToString()!
                });
            }
        }

        public class PolicyHolderRow
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string PolicyNumber { get; set; } = "";
            public string Address { get; set; } = "";
        }

        public class ClaimRow
        {
            public string ClaimNumber { get; set; } = "";
            public DateTime ClaimDate { get; set; }
            public string Status { get; set; } = "";
            public decimal ClaimAmount { get; set; }
            public string BranchName { get; set; } = "";
        }
    }
}
