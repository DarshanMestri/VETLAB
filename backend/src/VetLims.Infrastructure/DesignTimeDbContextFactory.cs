using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VetLims.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VetLimsDbContext>
{
    public VetLimsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VetLimsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=vet_lims;Username=vet_user;Password=p@ssw0rd")
            .Options;
        return new VetLimsDbContext(options);
    }
}
