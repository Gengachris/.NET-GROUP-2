using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Claims
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class CreateModel : PageModel
    {

        private readonly IConfiguration _configuration;
        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [BindProperty] public int PolicyHolderId { get; set; }
        [BindProperty] public int BranchId { get; set; }
        [BindProperty] public DateTime ClaimDate { get; set; } = DateTime.Today;
        [BindProperty] public DateTime IncidentDate { get; set; } = DateTime.Today;
        [BindProperty] public decimal ClaimAmount { get; set; }
        [BindProperty] public string IncidentLocation { get; set; } = "";
        [BindProperty] public string IncidentDescription { get; set; } = "";
        public SelectList PolicyHolderList { get; set; } = default!;
        public SelectList BranchList { get; set; } = default!;
        public string ErrorMessage { get; set; } = "";
        public void OnGet()
        {
            LoadLists();
        }

        public IActionResult OnPost()
        {
            LoadLists();
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand("usp_Claims_Register", conn);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@policyHolderId", PolicyHolderId);
            cmd.Parameters.AddWithValue("@branchId", BranchId);
            cmd.Parameters.AddWithValue("@claimDate", ClaimDate.Date);
            cmd.Parameters.AddWithValue("@incidentDate", IncidentDate.Date);
            cmd.Parameters.AddWithValue("@claimAmount", ClaimAmount);
            cmd.Parameters.AddWithValue("@location", IncidentLocation ?? "");
            cmd.Parameters.AddWithValue("@description", IncidentDescription ?? "");
            cmd.Parameters.AddWithValue("@userId", int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));
            conn.Open();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
            return RedirectToPage("Index");
        }
        private void LoadLists()
        {
            var holders = new List<SelectListItem>();
            var branches = new List<SelectListItem>();
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            conn.Open();
            using (var cmd = new SqlCommand("SELECT policyHolderId, firstName, lastName, policyNumber FROM PolicyHolders ORDER BY lastName", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    holders.Add(new SelectListItem(reader["firstName"] + " " + reader["lastName"] + " (" + reader["policyNumber"] + ")", reader["policyHolderId"].ToString()));
            }
            using (var cmd = new SqlCommand("SELECT branchId, branchName FROM Branches WHERE isActive = 1 ORDER BY branchName", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    branches.Add(new SelectListItem(reader["branchName"].ToString(), reader["branchId"].ToString()));
            }
            PolicyHolderList = new SelectList(holders, "Value", "Text");
            BranchList = new SelectList(branches, "Value", "Text");
        }
    }
}
