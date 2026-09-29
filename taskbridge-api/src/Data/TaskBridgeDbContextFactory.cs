using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskBridge.Api.Data;

public sealed class TaskBridgeDbContextFactory : IDesignTimeDbContextFactory<TaskBridgeDbContext>
{
    public TaskBridgeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TaskBridge")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__TaskBridge before creating or applying database migrations.");

        var options = new DbContextOptionsBuilder<TaskBridgeDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TaskBridgeDbContext(options);
    }
}