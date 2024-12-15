using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Data.Interfaces;
using FileManagerApp.Data.Repositories;

namespace FileManagerApp.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        // Database context that will be shared across repositories
        private readonly FileManagerDbContext _context;

        // Repository instances
        private IFileRepository _filesRepository;
        private IFolderRepository _foldersRepository;

        // Track whether Dispose has been called
        private bool _disposed;

        public UnitOfWork(FileManagerDbContext context)
        {
            _context = context;
        }

        // Lazy loading of repositories (only create them when first accessed)
        public IFileRepository Files
        {
            get { return _filesRepository ??= new FileRepository(_context); }
        }
        
        public IFolderRepository Folders
        {
            get { return _foldersRepository ??= new FolderRepository(_context); }
        }

        // Method to save changes
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                // Attempt to save changes at once
                return await _context.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                // Log the exception
                throw new Exception("Error occurred while saving changes", ex);
                
            }
        }

        // Implementation of IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Dispose managed resources
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
