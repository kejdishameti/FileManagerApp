using FileManagerApp.API.Helpers;
using FileManagerApp.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using FileManagerApp.API.Helpers;
using System.Reflection;

namespace FileManagerApp.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddDbContext<FileManagerDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


            // Register our data layer services using the extension method we created
            builder.Services.AddDataLayer();

            // Add AutoMapper
            builder.Services.AddAutoMapper(Assembly.GetEntryAssembly());

            // Add controllers
            builder.Services.AddControllers();

            // Add API documentation tools
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
