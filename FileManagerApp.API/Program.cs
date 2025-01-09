using FileManagerApp.API.Helpers;
using FileManagerApp.Data;
using FileManagerApp.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using FileManagerApp.API.Helpers;
using System.Reflection;
using Npgsql;


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

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet();

            // Register our data layer services using the extension method we created
            builder.Services.AddDataLayer();
            builder.Services.AddServiceLayer();

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
