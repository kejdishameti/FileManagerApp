using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Domain.Entities;
using DomainFile = FileManagerApp.Domain.Entities.File;


namespace FileManagerApp.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }

    public interface IFileRepository : IRepository<DomainFile>
    {
        Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId);
        Task<DomainFile> GetFileByPathAsync(string path);
    }

    public interface IFolderRepository : IRepository<Folder>
    {
        Task<Folder> GetFolderByPathAsync(string path);
        Task<IEnumerable<Folder>> GetChildFoldersByParentIdAsync(int parentFolderId);
    }
}
