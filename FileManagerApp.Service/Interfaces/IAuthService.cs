using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FileManagerApp.Service.Interfaces
{
    public interface IAuthService
    {
        Task<UserDTO> RegisterAsync(RegisterDTO registerDto);
        Task<UserDTO> LoginAsync(LoginDTO loginDto);
        Task<string> CreateTokenAsync(User user);
    }
}
