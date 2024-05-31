using HRMCutTimeInOut.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMCutTimeInOut.Data.SQLSERVER;

public class ApplicationDBContext:DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) :base(options) {
    
    }
    
    // DbSet for your entities
    public DbSet<Shift> Shifts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shift>()
            .HasNoKey();
    }

}

