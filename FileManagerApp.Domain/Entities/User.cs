using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainFile = FileManagerApp.Domain.Entities.File;

namespace FileManagerApp.Domain.Entities
{
    public class User
    {

        public int Id { get; private set; }
        public string Email { get; private set; }
        public string Username { get; private set; }
        public byte[] PasswordHash { get; private set; }
        public byte[] PasswordSalt { get; private set; }
        public string Role { get; private set; } // "Admin" or "User"
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        public virtual ICollection<DomainFile> Files { get; private set; } = new List<DomainFile>();
        public virtual ICollection<Folder> Folders { get; private set; } = new List<Folder>();

        private User() { } // For EF Core

        public static User Create(string email, string username, string role = "User")
        {
            return new User
            {
                Email = email?.ToLower().Trim(),
                Username = username?.Trim(),
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                Files = new List<DomainFile>(),
                Folders = new List<Folder>()
            };
        }

        public void SetPassword(string password)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512();
            PasswordSalt = hmac.Key;
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }

        public bool VerifyPassword(string password)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(PasswordSalt);
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(PasswordHash);
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}
