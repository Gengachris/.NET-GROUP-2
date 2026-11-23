using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Claims
{
    [Authorize(Roles = "Administrator,Claims Manager,Claims Adjuster,Branch Manager,Finance Officer")]
    public class DetailsModel : PageModel
    {

        private readonly IConfiguration _configuration;
        public DetailsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int ClaimId { get; set; }
        public string ClaimNumber { get; set; } = "";
        public DateTime ClaimDate { get; set; }
        public DateTime IncidentDate { get; set; }
        public decimal ClaimAmount { get; set; }
        public string IncidentLocation { get; set; } = "";
        public string IncidentDescription { get; set; } = "";
        public string Status { get; set; } = "";
        public string PolicyHolderName { get; set; } = "";
        public string PolicyNumber { get; set; } = "";
        public string BranchName { get; set; } = "";
        public List<HistoryRow> History { get; set; } = new();
        public IActionResult OnGet(int claimId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            conn.Open();
            using (var cmd = new SqlCommand("usp_Claims_Get", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@claimId", claimId);
                cmd.Parameters.AddWithValue("@userId", int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    return RedirectToPage("Index");
                }
                ClaimId = claimId;
                ClaimNumber = reader["claimNumber"].ToString()!;
                ClaimDate = Convert.ToDateTime(reader["claimDate"]);
                IncidentDate = Convert.ToDateTime(reader["incidentDate"]);
                ClaimAmount = Convert.ToDecimal(reader["claimAmount"]);
                IncidentLocation = reader["incidentLocation"].ToString()!;
                IncidentDescription = reader["incidentDescription"].ToString()!;
                Status = reader["status"].ToString()!;
                PolicyHolderName = reader["policyHolderName"].ToString()!;
                PolicyNumber = reader["policyNumber"].ToString()!;
                BranchName = reader["branchName"].ToString()!;
            }
            using var history = new SqlCommand("usp_Claims_History", conn);
            history.CommandType = System.Data.CommandType.StoredProcedure;
            history.Parameters.AddWithValue("@claimId", claimId);
            using var rows = history.ExecuteReader();
            while (rows.Read())
            {
                History.Add(new HistoryRow
                {
                    ChangeDate = Convert.ToDateTime(rows["changeDate"]),
                    OldStatus = rows["oldStatus"].ToString()!,
                    NewStatus = rows["NewStatus"].ToString()!,
                    Remarks = rows["remarks"].ToString()!,
                    UserName = rows["userName"].ToString()!
                });
            }
            return Page();
        }

        public IActionResult OnPostReview(int claimId)
        {
            return ChangeStatus(claimId, "Under Review", "Claim is under review.");
        }

        public IActionResult OnPostDocuments(int claimId)
        {
            return ChangeStatus(claimId, "Pending Documents", "Waiting for documents.");
        }

        private IActionResult ChangeStatus(int claimId, string newStatus, string remarks)
        {
            if (!User.IsInRole("Administrator") && !User.IsInRole("Claims Manager"))
                return RedirectToPage("Index");

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("usp_Claims_ChangeStatus", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@claimId", claimId);
            cmd.Parameters.AddWithValue("@newStatus", newStatus);
            cmd.Parameters.AddWithValue("@userId", int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
            cmd.Parameters.AddWithValue("@remarks", remarks);
            conn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToPage(new { claimId });
        }
        public class HistoryRow
        {
            public DateTime ChangeDate { get; set; }
            public string OldStatus { get; set; } = "";
            public string NewStatus { get; set; } = "";
            public string Remarks { get; set; } = "";
            public string UserName { get; set; } = "";
        }
    }
}
