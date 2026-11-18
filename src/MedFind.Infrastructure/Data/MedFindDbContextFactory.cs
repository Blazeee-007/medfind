using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MedFind.Infrastructure.Data;

/// <summary>
/// Design-time factory used only by EF Core CLI tools (dotnet ef migrations).
/// Uses a local connection string so migrations can be generated without Docker running.
/// </summary>
public class MedFindDbContextFactory : IDesignTimeDbContextFactory<MedFindDbContext>
{
    public MedFindDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MedFindDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=medfind;Username=medfind;Password=medfind",
            o => o.UseNetTopologySuite());

        return new MedFindDbContext(optionsBuilder.Options);
    }
}
