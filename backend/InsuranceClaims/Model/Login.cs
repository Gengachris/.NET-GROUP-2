namespace InsuranceClaims.Model
{
    public class LoginRequest
    {
        public string userName { get; set; } = "";
        public string password { get; set; } = "";
    }
    public class LoginResponse
    {
        public string token { get; set; } = "";
        public int userId { get; set; }
        public string userName { get; set; } = "";
        public string role { get; set; } = "";
    }
}
