using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Branches
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
        public string Message { get; set; } = "";
        public List<BranchRow> Branches { get; set; } = new();

        public void OnGet(string? search)
        {
            Search = search ?? "";
            LoadBranches();
        }

        public IActionResult OnPostRemove(int branchId, string? search)
        {
            Search = search ?? "";
            if (!User.IsInRole("Administrator"))
            {
                Message = "Only an Administrator can delete a branch.";
                LoadBranches();
                return Page();
            }

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var countCmd = new SqlCommand(@"SELECT
                    (SELECT COUNT(*) FROM Adjusters WHERE branchId = @branchId) +
                    (SELECT COUNT(*) FROM Claims WHERE branchId = @branchId)", conn);
            countCmd.Parameters.AddWithValue("@branchId", branchId);
            conn.Open();
            if (Convert.ToInt32(countCmd.ExecuteScalar()) > 0)
            {
                Message = "This branch cannot be deleted because it has adjusters or claims.";
                LoadBranches();
                return Page();
            }

            using var deleteCmd = new SqlCommand("DELETE FROM Branches WHERE branchId = @branchId", conn);
            deleteCmd.Parameters.AddWithValue("@branchId", branchId);
            deleteCmd.ExecuteNonQuery();
            Message = "Branch deleted.";
            LoadBranches();
            return Page();
        }

        private void LoadBranches()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"SELECT branchId, branchNumber, branchName, location, phone, email, isActive
                                             FROM Branches
                                             WHERE @search = N''
                                                OR branchNumber LIKE N'%' + @search + N'%'
                                                OR branchName LIKE N'%' + @search + N'%'
                                                OR location LIKE N'%' + @search + N'%'
                                             ORDER BY branchName", conn);
            cmd.Parameters.AddWithValue("@search", Search.Trim());
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Branches.Add(new BranchRow
                {
                    BranchId = Convert.ToInt32(reader["branchId"]),
                    BranchNumber = reader["branchNumber"].ToString()!,
                    BranchName = reader["branchName"].ToString()!,
                    Location = reader["location"].ToString()!,
                    Phone = reader["phone"].ToString()!,
                    Email = reader["email"].ToString()!,
                    IsActive = Convert.ToBoolean(reader["isActive"])
                });
            }
        }

        public class BranchRow
        {
            public int BranchId { get; set; }
            public string BranchNumber { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string Location { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Email { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}
