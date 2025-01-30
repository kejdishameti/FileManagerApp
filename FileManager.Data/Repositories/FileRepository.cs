using FileManagerApp.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<DomainFile>> GetAllAsync(int userId)
        {
            return await _context.Files
                .Where(f => !f.IsDeleted && f.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DomainFile> GetByIdAsync(int id, int userId)
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id &&
                                        !f.IsDeleted &&
                                        f.UserId == userId);
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId, int userId)
        {
            return await _context.Files
                .Where(f => f.FolderId == folderId &&
                           !f.IsDeleted &&
                           f.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DomainFile> GetFileByPathAsync(string path, int userId)
        {
            return await _context.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.StoragePath == path &&
                                    !f.IsDeleted &&
                                    f.UserId == userId);
        }

        public async Task<IEnumerable<DomainFile>> SearchFilesAsync(string searchTerm, int userId)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DomainFile>();

            return await _context.Files
                .Where(f => !f.IsDeleted &&
                           f.UserId == userId &&
                           (EF.Functions.ILike(f.Name, $"%{searchTerm}%") ||
                            f.Tags.Any()))  
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByTagAsync(string tag, int userId)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new List<DomainFile>();

            // Normalize tag for consistent searching
            tag = tag.Trim().ToLower();

            return await _context.Files
                .Where(f => !f.IsDeleted &&
                           f.UserId == userId &&
                           f.Tags.Contains(tag))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DomainFile> UpdateTagsAsync(int id, IEnumerable<string> tags, int userId)
        {
            var file = await _context.Files
                .FirstOrDefaultAsync(f => f.Id == id &&
                                         !f.IsDeleted &&
                                         f.UserId == userId);

            if (file == null)
                return null;

            file.Tags = tags?.ToList() ?? new List<string>();
            file.ModifiedAt = DateTime.UtcNow;

            _context.Files.Update(file);
            return file;
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
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            _context.Files.Update(entity);
        }

        public async Task BatchDeleteAsync(IEnumerable<int> fileIds, int userId)
        {
            if (fileIds == null || !fileIds.Any())
                throw new ArgumentException("No file IDs provided for deletion");

            var filesToDelete = await _context.Files
                .Where(f => fileIds.Contains(f.Id) &&
                           !f.IsDeleted &&
                           f.UserId == userId)
                .ToListAsync();

            foreach (var file in filesToDelete)
            {
                file.IsDeleted = true;
                file.DeletedAt = DateTime.UtcNow;
            }

            _context.Files.UpdateRange(filesToDelete);
        }

        public async Task<IEnumerable<DomainFile>> GetFavoriteFilesAsync(int userId)
        {
            return await _context.Files
               .Where(f => !f.IsDeleted &&
                          f.UserId == userId &&
                          f.IsFavorite)
               .AsNoTracking()
               .ToListAsync();
        }
    }
}