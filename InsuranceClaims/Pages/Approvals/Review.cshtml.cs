using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Approvals
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class ReviewModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public ReviewModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        [BindProperty] public string Decision { get; set; } = "";
        [BindProperty] public decimal? ApprovedAmount { get; set; }
        [BindProperty] public string? RejectionReason { get; set; }
        [BindProperty] public string? Remarks { get; set; }

        public ClaimDetails? Claim { get; set; }
        public List<HistoryRow> History { get; set; } = new();
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet()
        {
            if (!LoadClaim()) return NotFound();
            ApprovedAmount = Claim!.ClaimAmount;
            return Page();
        }

        public IActionResult OnPost()
        {
            using (var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims")))
            using (var cmd = new SqlCommand("usp_Approvals_Decide", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@claimId", Id);
                cmd.Parameters.AddWithValue("@decision", Decision ?? "");
                cmd.Parameters.AddWithValue("@approvedAmount", (object?)ApprovedAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rejectionReason", (object?)RejectionReason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@remarks", (object?)Remarks ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@userId", int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
                conn.Open();
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    ErrorMessage = ex.Message;
                    if (!LoadClaim()) return NotFound();
                    return Page();
                }
            }
            TempData["Message"] = "Claim " + Decision.ToLower() + " successfully.";
            return RedirectToPage("Index");
        }

        private bool LoadClaim()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            conn.Open();

            using (var cmd = new SqlCommand(@"
                SELECT c.claimNumber, c.claimDate, c.incidentDate, c.claimAmount, c.approvedAmount,
                       c.incidentDescription, c.incidentLocation, c.status, c.rejectionReason,
                       ph.firstName + ' ' + ph.lastName AS policyHolderName, ph.policyNumber,
                       b.branchName,
                       a.firstName + ' ' + a.lastName AS adjusterName
                FROM Claims c
                INNER JOIN PolicyHolders ph ON c.policyHolderId = ph.policyHolderId
                INNER JOIN Branches b ON c.branchId = b.branchId
                LEFT JOIN Adjusters a ON c.adjusterId = a.adjusterId
                WHERE c.claimId = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", Id);
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return false;
                Claim = new ClaimDetails
                {
                    ClaimNumber = reader["claimNumber"].ToString()!,
                    ClaimDate = Convert.ToDateTime(reader["claimDate"]),
                    IncidentDate = Convert.ToDateTime(reader["incidentDate"]),
                    ClaimAmount = Convert.ToDecimal(reader["claimAmount"]),
                    IncidentDescription = reader["incidentDescription"].ToString()!,
                    IncidentLocation = reader["incidentLocation"].ToString()!,
                    Status = reader["status"].ToString()!,
                    PolicyHolderName = reader["policyHolderName"].ToString()!,
                    PolicyNumber = reader["policyNumber"].ToString()!,
                    BranchName = reader["branchName"].ToString()!,
                    AdjusterName = reader["adjusterName"] as string ?? "Not assigned"
                };
            }

            using (var cmd = new SqlCommand(@"
                SELECT h.oldStatus, h.NewStatus, h.changeDate, h.remarks, u.userName
                FROM ClaimHistory h
                INNER JOIN Users u ON h.changedBy = u.userId
                WHERE h.claimId = @id
                ORDER BY h.changeDate DESC", conn))
            {
                cmd.Parameters.AddWithValue("@id", Id);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    History.Add(new HistoryRow
                    {
                        OldStatus = reader["oldStatus"].ToString()!,
                        NewStatus = reader["NewStatus"].ToString()!,
                        ChangeDate = Convert.ToDateTime(reader["changeDate"]),
                        Remarks = reader["remarks"].ToString()!,
                        UserName = reader["userName"].ToString()!
                    });
                }
            }
            return true;
        }

        public class ClaimDetails
        {
            public string ClaimNumber { get; set; } = "";
            public DateTime ClaimDate { get; set; }
            public DateTime IncidentDate { get; set; }
            public decimal ClaimAmount { get; set; }
            public string IncidentDescription { get; set; } = "";
            public string IncidentLocation { get; set; } = "";
            public string Status { get; set; } = "";
            public string PolicyHolderName { get; set; } = "";
            public string PolicyNumber { get; set; } = "";
            public string BranchName { get; set; } = "";
            public string AdjusterName { get; set; } = "";
        }

        public class HistoryRow
        {
            public string OldStatus { get; set; } = "";
            public string NewStatus { get; set; } = "";
            public DateTime ChangeDate { get; set; }
            public string Remarks { get; set; } = "";
            public string UserName { get; set; } = "";
        }
    }
}