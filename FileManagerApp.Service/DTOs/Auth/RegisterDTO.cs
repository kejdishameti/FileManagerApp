using System.ComponentModel.DataAnnotations;

namespace FileManagerApp.Service.DTOs.Auth
{
    public class RegisterDTO
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
