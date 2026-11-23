using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Claims
{
    [Authorize(Roles = "Administrator,Claims Manager,Claims Adjuster,Branch Manager,Finance Officer")]
    public class IndexModel : PageModel
    {
        
        private readonly IConfiguration _configuration;
        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string Search { get; set; } = "";
        public List<ClaimRow> Claims { get; set; } = new();
        public void OnGet(string? search, string? status)
        {
            Search = search ?? "";
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("usp_Claims_List", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@search", Search);
            cmd.Parameters.AddWithValue("@status", status ?? "");
            cmd.Parameters.AddWithValue("@userId", int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Claims.Add(new ClaimRow
                {
                    ClaimNumber = reader["claimNumber"].ToString()!,
                    PolicyHolderName = reader["policyHolderName"].ToString()!,
                    BranchName = reader["branchName"].ToString()!,
                    Status = reader["status"].ToString()!,
                    ClaimAmount = Convert.ToDecimal(reader["claimAmount"])
                });
            }

        }

        public class ClaimRow
        {
            public string ClaimNumber { get; set; } = "";
            public string PolicyHolderName { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal ClaimAmount { get; set; }
        }
    }
}
