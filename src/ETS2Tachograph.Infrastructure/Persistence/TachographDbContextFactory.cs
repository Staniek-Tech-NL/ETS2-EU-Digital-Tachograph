using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ETS2Tachograph.Infrastructure.Persistence;

public sealed class TachographDbContextFactory : IDesignTimeDbContextFactory<TachographDbContext>
{
    public TachographDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TachographDbContext>();
        builder.UseSqlite("Data Source=tachograph.db");
        return new TachographDbContext(builder.Options);
    }
}
