using FileManagerApp.API.Helpers;
using FileManagerApp.Data;
using FileManagerApp.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System.Reflection;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FileManagerApp.Service.Interfaces;
using FileManagerApp.Service.Implementations;
using FileManagerApp.Service.Mappings;


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

            // Register the AuthService
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Register our data layer services using the extension method we created
            builder.Services.AddDataLayer();
            builder.Services.AddServiceLayer();

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(Program).Assembly);

            // Add Authentication and JWT configuration
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var tokenKey = builder.Configuration["TokenKey"];
                    if (string.IsNullOrEmpty(tokenKey))
                    {
                        throw new InvalidOperationException("TokenKey is not configured in appsettings.json");
                    }

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(tokenKey)),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

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
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
