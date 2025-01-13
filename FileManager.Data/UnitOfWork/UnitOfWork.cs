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
        private readonly FileManagerDbContext _context;
        private IFileRepository _filesRepository;
        private IFolderRepository _foldersRepository;
        private IUserRepository _usersRepository;
        private bool _disposed;

        public UnitOfWork(FileManagerDbContext context)
        {
            _context = context;
        }

        // Add Users property alongside existing repositories
        public IFileRepository Files =>
            _filesRepository ??= new FileRepository(_context);

        public IFolderRepository Folders =>
            _foldersRepository ??= new FolderRepository(_context);

        public IUserRepository Users =>
            _usersRepository ??= new UserRepository(_context);

        // Existing SaveChanges and Dispose methods remain the same
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while saving changes", ex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
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
