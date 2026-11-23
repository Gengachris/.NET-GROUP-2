using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.PolicyHolders
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CreateModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty, Required] public string FirstName { get; set; } = "";
        [BindProperty, Required] public string LastName { get; set; } = "";
        [BindProperty, Required, EmailAddress] public string Email { get; set; } = "";
        [BindProperty, Required, RegularExpression(@"^[0-9+()\- .]{7,20}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = "";
        [BindProperty, Required] public string PolicyNumber { get; set; } = "";
        [BindProperty, Required] public string Address { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"INSERT INTO PolicyHolders
                    (firstName, lastName, email, phone, policyNumber, address, createdAt)
                    VALUES (@firstName, @lastName, @email, @phone, @policyNumber, @address, GETDATE())", conn);
            cmd.Parameters.AddWithValue("@firstName", FirstName.Trim());
            cmd.Parameters.AddWithValue("@lastName", LastName.Trim());
            cmd.Parameters.AddWithValue("@email", Email.Trim());
            cmd.Parameters.AddWithValue("@phone", Phone.Trim());
            cmd.Parameters.AddWithValue("@policyNumber", PolicyNumber.Trim());
            cmd.Parameters.AddWithValue("@address", Address.Trim());
            conn.Open();
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                ErrorMessage = "That policy number is already used.";
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}
