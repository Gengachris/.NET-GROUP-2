using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;


namespace InsuranceClaims.Pages.Investigations
{

    [Authorize(Roles = "Claims Adjuster")]
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public CreateModel(IConfiguration configuration) { _configuration = configuration; }
        [BindProperty] public int ClaimId { get; set; }
        [BindProperty] public DateTime InvestigationDate { get; set; } = DateTime.Today;
        [BindProperty] public string Findings { get; set; } = "";
        [BindProperty] public decimal RecommendedAmount { get; set; }
        [BindProperty] public string Remarks { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public void OnGet(int claimId) { ClaimId = claimId; }
        public IActionResult OnPost()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("usp_Investigations_Add", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@claimId", ClaimId);
            cmd.Parameters.AddWithValue("@userId", int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
            cmd.Parameters.AddWithValue("@investigationDate", InvestigationDate.Date);
            cmd.Parameters.AddWithValue("@findings", Findings ?? "");
            cmd.Parameters.AddWithValue("@recommendedAmount", RecommendedAmount);
            cmd.Parameters.AddWithValue("@remarks", Remarks ?? "");
            conn.Open();
            try { cmd.ExecuteNonQuery(); }
            catch (SqlException ex) { ErrorMessage = ex.Message; return Page(); }
            return RedirectToPage("/Claims/Details", new { claimId = ClaimId });
        }
    }
}
