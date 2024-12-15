using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FileManagerApp.Data
{
    public class FileManagerDbContextFactory : IDesignTimeDbContextFactory<FileManagerDbContext>
    {
        public FileManagerDbContext CreateDbContext(string[] args)
        {
            // Create the configuration builder and set up our connection string
            var optionsBuilder = new DbContextOptionsBuilder<FileManagerDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=FileManagerDb;Username=postgres;Password=admin; Maximum Pool Size=10;Connection Idle Lifetime=60;Connection Pruning Interval=30;Timeout=30;Command Timeout=30;");

            return new FileManagerDbContext(optionsBuilder.Options);
        }
    }
}
