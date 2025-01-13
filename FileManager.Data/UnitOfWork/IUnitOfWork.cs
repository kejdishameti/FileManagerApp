using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Data.Interfaces;

namespace FileManagerApp.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        // Properties to access repositories
        IFileRepository Files { get; }
        IFolderRepository Folders { get; }
        IUserRepository Users { get; }

        // Method to save changes
        Task<int> SaveChangesAsync();
    }
}
