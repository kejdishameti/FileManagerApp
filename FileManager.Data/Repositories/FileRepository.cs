using FileManagerApp.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainFile = FileManagerApp.Domain.Entities.File;


namespace FileManagerApp.Data.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly FileManagerDbContext _context;

        public FileRepository(FileManagerDbContext context)
        {
            _context = context;
        }

        public async Task<DomainFile> GetByIdAsync(int id)
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<IEnumerable<DomainFile>> GetAllAsync()
        {
            return await _context.Files
                .Where(f => !f.IsDeleted)
                .ToListAsync();
        }

        public async Task AddAsync(DomainFile entity)
        {
            await _context.Files.AddAsync(entity);
        }

        public void Update(DomainFile entity)
        {
            _context.Files.Update(entity);
        }

        public void Delete(DomainFile entity)
        {
            entity.MarkAsDeleted();
            _context.Files.Update(entity);
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId)
        {
            return await _context.Files
                .Where(f => f.FolderId == folderId && !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<DomainFile> GetFileByPathAsync(string path)
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.StoragePath == path && !f.IsDeleted);
        }
    }
}
