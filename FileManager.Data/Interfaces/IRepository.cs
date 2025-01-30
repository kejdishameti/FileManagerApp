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
        Task<T> GetByIdAsync(int id, int userId);
        Task<IEnumerable<T>> GetAllAsync(int userId);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }

    public interface IFileRepository : IRepository<DomainFile>
    {
        Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId, int userId);
        Task<DomainFile> GetFileByPathAsync(string path, int userId);
        Task<IEnumerable<DomainFile>> SearchFilesAsync(string searchTerm, int userId);
        Task<IEnumerable<DomainFile>> GetFilesByTagAsync(string tag, int userId);
        Task<DomainFile> UpdateTagsAsync(int id, IEnumerable<string> tags, int userId);
        Task BatchDeleteAsync(IEnumerable<int> fileIds, int userId);
        Task<IEnumerable<DomainFile>> GetFavoriteFilesAsync(int userId);

    }

    public interface IFolderRepository : IRepository<Folder>
    {
        Task<Folder> GetFolderByPathAsync(string path, int userId);
        Task<IEnumerable<Folder>> GetChildFoldersByParentIdAsync(int parentFolderId, int userId);
        Task<IEnumerable<Folder>> SearchFoldersAsync(string searchTerm, int userId);
        Task<IEnumerable<Folder>> GetFoldersByTagAsync(string tag, int userId);
        Task BatchDeleteAsync(IEnumerable<int> folderIds, int userId);
        Task<IEnumerable<Folder>> GetFavoriteFoldersAsync(int userId);
    }
}
