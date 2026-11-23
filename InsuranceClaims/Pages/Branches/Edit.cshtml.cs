using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Branches
{
    [Authorize(Roles = "Administrator")]
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty] public int BranchId { get; set; }
        [BindProperty, Required] public string BranchNumber { get; set; } = "";
        [BindProperty, Required] public string BranchName { get; set; } = "";
        [BindProperty, Required] public string Location { get; set; } = "";
        [BindProperty, Required, RegularExpression(@"^[0-9+()\- .]{7,20}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = "";
        [BindProperty, Required, EmailAddress] public string Email { get; set; } = "";
        [BindProperty] public bool IsActive { get; set; }
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet(int branchId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"SELECT branchNumber, branchName, location, phone, email, isActive
                                             FROM Branches WHERE branchId = @branchId", conn);
            cmd.Parameters.AddWithValue("@branchId", branchId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return RedirectToPage("Index");

            BranchId = branchId;
            BranchNumber = reader["branchNumber"].ToString()!;
            BranchName = reader["branchName"].ToString()!;
            Location = reader["location"].ToString()!;
            Phone = reader["phone"].ToString()!;
            Email = reader["email"].ToString()!;
            IsActive = Convert.ToBoolean(reader["isActive"]);
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"UPDATE Branches SET branchNumber = @branchNumber, branchName = @branchName,
                    location = @location, phone = @phone, email = @email, isActive = @isActive
                    WHERE branchId = @branchId", conn);
            cmd.Parameters.AddWithValue("@branchNumber", BranchNumber.Trim());
            cmd.Parameters.AddWithValue("@branchName", BranchName.Trim());
            cmd.Parameters.AddWithValue("@location", Location.Trim());
            cmd.Parameters.AddWithValue("@phone", Phone.Trim());
            cmd.Parameters.AddWithValue("@email", Email.Trim());
            cmd.Parameters.AddWithValue("@isActive", IsActive);
            cmd.Parameters.AddWithValue("@branchId", BranchId);
            conn.Open();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                ErrorMessage = "That branch number is already used.";
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}
