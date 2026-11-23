using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.PolicyHolders
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class EditModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public EditModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty] public int PolicyHolderId { get; set; }
        [BindProperty, Required] public string FirstName { get; set; } = "";
        [BindProperty, Required] public string LastName { get; set; } = "";
        [BindProperty, Required, EmailAddress] public string Email { get; set; } = "";
        [BindProperty, Required, RegularExpression(@"^[0-9+()\- .]{7,20}$", ErrorMessage = "Enter a valid phone number.")] public string Phone { get; set; } = "";
        [BindProperty, Required] public string PolicyNumber { get; set; } = "";
        [BindProperty, Required] public string Address { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public IActionResult OnGet(int policyHolderId)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"SELECT firstName, lastName, email, phone, policyNumber, address
                                             FROM PolicyHolders WHERE policyHolderId = @policyHolderId", conn);
            cmd.Parameters.AddWithValue("@policyHolderId", policyHolderId);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return RedirectToPage("Index");

            PolicyHolderId = policyHolderId;
            FirstName = reader["firstName"].ToString()!;
            LastName = reader["lastName"].ToString()!;
            Email = reader["email"].ToString()!;
            Phone = reader["phone"].ToString()!;
            PolicyNumber = reader["policyNumber"].ToString()!;
            Address = reader["address"].ToString()!;
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"UPDATE PolicyHolders SET firstName = @firstName, lastName = @lastName,
                    email = @email, phone = @phone, policyNumber = @policyNumber, address = @address
                    WHERE policyHolderId = @policyHolderId", conn);
            cmd.Parameters.AddWithValue("@firstName", FirstName.Trim());
            cmd.Parameters.AddWithValue("@lastName", LastName.Trim());
            cmd.Parameters.AddWithValue("@email", Email.Trim());
            cmd.Parameters.AddWithValue("@phone", Phone.Trim());
            cmd.Parameters.AddWithValue("@policyNumber", PolicyNumber.Trim());
            cmd.Parameters.AddWithValue("@address", Address.Trim());
            cmd.Parameters.AddWithValue("@policyHolderId", PolicyHolderId);
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
