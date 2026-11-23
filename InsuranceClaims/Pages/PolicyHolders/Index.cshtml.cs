using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace InsuranceClaims.Pages.PolicyHolders
{
    [Authorize(Roles = "Administrator,Claims Manager")]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Search { get; set; } = "";
        public string Message { get; set; } = "";
        public List<PolicyHolderRow> PolicyHolders { get; set; } = new();

        public void OnGet(string? search)
        {
            Search = search ?? "";
            LoadPolicyHolders();
        }

        public IActionResult OnPostRemove(int policyHolderId, string? search)
        {
            Search = search ?? "";
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var countCmd = new SqlCommand("SELECT COUNT(*) FROM Claims WHERE policyHolderId = @policyHolderId", conn);
            countCmd.Parameters.AddWithValue("@policyHolderId", policyHolderId);
            conn.Open();
            if (Convert.ToInt32(countCmd.ExecuteScalar()) > 0)
            {
                Message = "This policyholder cannot be deleted because they have existing claims.";
                LoadPolicyHolders();
                return Page();
            }

            using var deleteCmd = new SqlCommand("DELETE FROM PolicyHolders WHERE policyHolderId = @policyHolderId", conn);
            deleteCmd.Parameters.AddWithValue("@policyHolderId", policyHolderId);
            deleteCmd.ExecuteNonQuery();
            Message = "Policyholder deleted.";
            LoadPolicyHolders();
            return Page();
        }

        private void LoadPolicyHolders()
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("InsuranceClaims"));
            using var cmd = new SqlCommand(@"SELECT policyHolderId, firstName, lastName, email, phone, policyNumber, address
                                             FROM PolicyHolders
                                             WHERE @search = N''
                                                OR firstName LIKE N'%' + @search + N'%'
                                                OR lastName LIKE N'%' + @search + N'%'
                                                OR email LIKE N'%' + @search + N'%'
                                                OR phone LIKE N'%' + @search + N'%'
                                                OR policyNumber LIKE N'%' + @search + N'%'
                                             ORDER BY lastName, firstName", conn);
            cmd.Parameters.AddWithValue("@search", Search.Trim());
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                PolicyHolders.Add(new PolicyHolderRow
                {
                    PolicyHolderId = Convert.ToInt32(reader["policyHolderId"]),
                    FirstName = reader["firstName"].ToString()!,
                    LastName = reader["lastName"].ToString()!,
                    Email = reader["email"].ToString()!,
                    Phone = reader["phone"].ToString()!,
                    PolicyNumber = reader["policyNumber"].ToString()!,
                    Address = reader["address"].ToString()!
                });
            }
        }

        public class PolicyHolderRow
        {
            public int PolicyHolderId { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string PolicyNumber { get; set; } = "";
            public string Address { get; set; } = "";
        }
    }
}
