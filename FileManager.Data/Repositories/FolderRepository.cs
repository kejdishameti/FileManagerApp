using FileManagerApp.Data.Interfaces;
using FileManagerApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileManagerApp.Data.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        private readonly FileManagerDbContext _context;

        public FolderRepository(FileManagerDbContext context)
        {
            _context = context;
        }

        public async Task<Folder> GetByIdAsync(int id, int userId)
        {
            try
            {
                return await _context.Folders
                 .Include(f => f.Files)
                 .Include(f => f.ChildFolders)
                 .FirstOrDefaultAsync(f => f.Id == id &&
                                         !f.IsDeleted &&
                                         f.UserId == userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving folder by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Folder>> GetAllAsync(int userId)
        {
            try
            {
                return await _context.Folders
                    .Where(f => !f.IsDeleted && f.UserId == userId)
                    .OrderBy(f => f.Path)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all folders: {ex.Message}");
                throw;
            }
        }

        public async Task AddAsync(Folder entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                await _context.Folders.AddAsync(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding folder: {ex.Message}");
                throw;
            }
        }

        public void Update(Folder entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                _context.Folders.Update(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating folder: {ex.Message}");
                throw;
            }
        }

        public void Delete(Folder entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                entity.MarkAsDeleted();
                _context.Folders.Update(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting folder: {ex.Message}");
                throw;
            }
        }

        public async Task BatchDeleteAsync(IEnumerable<int> folderIds, int userId)
        {
            try
            {
                // Validate input
                if (folderIds == null || !folderIds.Any())
                    throw new ArgumentException("No folder IDs provided for deletion");

                var foldersToDelete = await _context.Folders
                    .Where(f => folderIds.Contains(f.Id) &&
                       !f.IsDeleted &&
                       f.UserId == userId)
                    .ToListAsync();

                foreach (var folder in foldersToDelete)
                {
                    folder.MarkAsDeleted();
                }

                _context.Folders.UpdateRange(foldersToDelete);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in batch folder deletion: {ex.Message}");
                throw;
            }
        }

        public async Task<Folder> GetFolderByPathAsync(string path, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Path cannot be empty", nameof(path));

                return await _context.Folders
                     .FirstOrDefaultAsync(f => f.Path == path &&
                                    !f.IsDeleted &&
                                    f.UserId == userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving folder by path: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Folder>> GetChildFoldersByParentIdAsync(int parentFolderId, int userId)
        {
            try
            {
                return await _context.Folders
                    .Where(f => f.ParentFolderId == parentFolderId &&
                               !f.IsDeleted &&
                               f.UserId == userId)
                    .OrderBy(f => f.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving child folders: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Folder>> SearchFoldersAsync(string searchTerm, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return new List<Folder>();

                searchTerm = searchTerm.ToLower();

                return await _context.Folders
                    .Where(f => !f.IsDeleted &&
                               f.UserId == userId &&
                               (f.Name.ToLower().Contains(searchTerm) ||
                                f.Path.ToLower().Contains(searchTerm) ||
                                f.Tags.Any(t => t.ToLower().Contains(searchTerm))))
                    .AsNoTracking()
                    .OrderBy(f => f.Path)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching folders: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Folder>> GetFoldersByTagAsync(string tag, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tag))
                    return new List<Folder>();

                tag = tag.Trim().ToLower();

                return await _context.Folders
                    .Where(f => !f.IsDeleted &&
                               f.UserId == userId &&
                               f.Tags.Contains(tag))
                    .OrderBy(f => f.Path)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving folders by tag: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<Folder>> GetFavoriteFoldersAsync(int userId)
        {
            return await _context.Folders
                .Where(f => !f.IsDeleted &&
                       f.UserId == userId &&
                       f.IsFavorite)
                .OrderBy(f => f.Path)
                .ToListAsync();
        }
    }
}