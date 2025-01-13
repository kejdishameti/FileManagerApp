using FileManagerApp.Data.Interfaces;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FileManagerApp.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FileManagerDbContext _context;

        public UserRepository(FileManagerDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            
            return await _context.Users
                .Where(u => !u.IsDeleted)
                .ToListAsync();
        }

        public async Task<User> GetByEmailAsync(string email)
        {
           
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted);
        }

        public async Task AddAsync(User entity)
        {
            await _context.Users.AddAsync(entity);
        }

        public void Update(User entity)
        {
            _context.Users.Update(entity);
        }

        public void Delete(User entity)
        {
            entity.MarkAsDeleted();
            _context.Users.Update(entity);
        }
    }
}
