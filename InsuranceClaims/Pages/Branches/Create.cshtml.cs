using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.Branches
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty, Required] public string BranchNumber { get; set; } = "";
        [BindProperty, Required] public string BranchName { get; set; } = "";
        [BindProperty, Required] public string Location { get; set; } = "";
        [BindProperty, Required, RegularExpression(@"^[0-9+()\- .]{7,20}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = "";
        [BindProperty, Required, EmailAddress] public string Email { get; set; } = "";
        [BindProperty] public bool IsActive { get; set; } = true;
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"INSERT INTO Branches
                    (branchNumber, branchName, location, phone, email, isActive)
                    VALUES (@branchNumber, @branchName, @location, @phone, @email, @isActive)", conn);
            cmd.Parameters.AddWithValue("@branchNumber", BranchNumber.Trim());
            cmd.Parameters.AddWithValue("@branchName", BranchName.Trim());
            cmd.Parameters.AddWithValue("@location", Location.Trim());
            cmd.Parameters.AddWithValue("@phone", Phone.Trim());
            cmd.Parameters.AddWithValue("@email", Email.Trim());
            cmd.Parameters.AddWithValue("@isActive", IsActive);
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
