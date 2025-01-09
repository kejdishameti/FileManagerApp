using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Data.Interfaces;
using FileManagerApp.Data.Repositories;
using FileManagerApp.Data.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace FileManagerApp.Data
{
    public static class DependencyInjection
    {
        // Method to add data layer services to the dependency service container
        public static IServiceCollection AddDataLayer(this IServiceCollection services)
        {
            
            // Add the repositories
            services.AddScoped<IFileRepository, FileRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();

            // Add the unit of work
            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
   
            return services;
        }
    }
}
