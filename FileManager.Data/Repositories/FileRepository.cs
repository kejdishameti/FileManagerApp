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

        public async Task<DomainFile> GetByIdAsync(int id)
        {
            return await _context.Files
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<IEnumerable<DomainFile>> GetAllAsync()
        {
            return await _context.Files
                .Where(f => !f.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId)
        {
            return await _context.Files
                .Where(f => f.FolderId == folderId && !f.IsDeleted)
                .AsNoTracking()
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

        public async Task BatchDeleteAsync(IEnumerable<int> fileIds)
        {
            var filesToDelete = await _context.Files
                .Where(f => fileIds.Contains(f.Id) && !f.IsDeleted)
                .ToListAsync();

            foreach (var file in filesToDelete)
            {
                file.MarkAsDeleted();
            }

            _context.Files.UpdateRange(filesToDelete);
        }

        public async Task<DomainFile> GetFileByPathAsync(string path)
        {
            return await _context.Files
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.StoragePath == path && !f.IsDeleted);
        }

        public async Task<IEnumerable<DomainFile>> SearchFilesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return new List<DomainFile>();

                searchTerm = searchTerm.ToLower();

                var files = await _context.Files
                    .Where(f => !f.IsDeleted &&
                               (f.Name.ToLower().Contains(searchTerm) ||    
                                f.Tags.Any(t => t.ToLower().Contains(searchTerm)))
                    )
                    .AsNoTracking()
                    .ToListAsync();

                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error in SearchFilesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByTagAsync(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new List<DomainFile>();

            tag = tag.Trim().ToLower();

            return await _context.Files
                .Where(f => !f.IsDeleted && f.Tags.Contains(tag))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}