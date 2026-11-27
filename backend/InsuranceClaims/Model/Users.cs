namespace InsuranceClaims.Model
{
    public class Users
    {

        public int userId { get; set; }
        public string userName { get; set; } = "";
        public string email { get; set; } = "";
        public string passwordHash { get; set; } = "";
        public int roleId { get; set; }
        public bool isActive { get; set; }
        public DateTime createdAt { get; set; }
        public int? branchId { get; set; }
        public decimal? approvalLimit { get; set; }
        public Roles? role { get; set; }
    }
}
