using FileManagerApp.Data.Interfaces;
using FileManagerApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerApp.Data.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly FileManagerDbContext _context;

        public FolderRepository(FileManagerDbContext context)
        {
            _context = context;
        }

        public async Task<Folder> GetByIdAsync(int id)
        {
            return await _context.Folders
                .Include(f => f.Files)
                .Include(f => f.ChildFolders)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<IEnumerable<Folder>> GetAllAsync()
        {
            return await _context.Folders
                .Where(f => !f.IsDeleted)
                .ToListAsync();
        }

        public async Task AddAsync(Folder entity)
        {
            await _context.Folders.AddAsync(entity);
        }

        public void Update(Folder entity)
        {
            _context.Folders.Update(entity);
        }

        public void Delete(Folder entity)
        {
            entity.MarkAsDeleted();
            _context.Folders.Update(entity);
        }

        public async Task BatchDeleteAsync(IEnumerable<int> folderIds)
        {
            try
            {
                var foldersToDelete = await _context.Folders
                    .Where(f => folderIds.Contains(f.Id) && !f.IsDeleted)
                    .ToListAsync();

                foreach (var folder in foldersToDelete)
                {
                    folder.MarkAsDeleted();
                    _context.Folders.Update(folder);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in batch folder deletion: {ex.Message}");
                throw; 
            }
        }

        public async Task<Folder> GetFolderByPathAsync(string path)
        {
            return await _context.Folders
                .FirstOrDefaultAsync(f => f.Path == path && !f.IsDeleted);
        }

        public async Task<IEnumerable<Folder>> GetChildFoldersByParentIdAsync(int parentFolderId)
        {
            return await _context.Folders
                .Where(f => f.ParentFolderId == parentFolderId && !f.IsDeleted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Folder>> SearchFoldersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return new List<Folder>();

                searchTerm = searchTerm.ToLower();

                var folders = await _context.Folders
                    .Where(f => !f.IsDeleted &&
                               (f.Name.ToLower().Contains(searchTerm) ||
                                f.Path.ToLower().Contains(searchTerm)))
                    .AsNoTracking()
                    .ToListAsync();

                return folders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error in SearchFoldersAsync: {ex.Message}");
                throw;
            }
        }
    }
}
