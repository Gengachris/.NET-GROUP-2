using InsuranceClaims.Model;
using Microsoft.EntityFrameworkCore;


namespace InsuranceClaims.Data
{
    public class InsuranceDbContext : DbContext
    {

        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Roles>().HasKey(role => role.roleId);
            modelBuilder.Entity<Users>().HasKey(user => user.userId);
        }

    }

}
