using FileManagerApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Service.DTOs;
using Microsoft.AspNetCore.Http;

namespace FileManagerApp.Service.Interfaces
{
    public interface IFolderService
    {
        Task<Folder> GetFolderByIdAsync(int id, int userId);
        Task<IEnumerable<Folder>> GetAllFoldersAsync(int userId);
        Task<Folder> CreateFolderAsync(string name, int userId, int? parentFolderId, IEnumerable<string> tags);
        Task<(Folder RootFolder, IEnumerable<Domain.Entities.File> Files)> UploadFolderAsync(
        IFormFileCollection files,
        int userId,
        int? parentFolderId,
        IEnumerable<string> tags);
        Task<bool> DeleteFolderAsync(int id, int userId);
        Task<Folder> RenameFolderAsync(int id, string newName, int userId);
        Task<IEnumerable<Folder>> GetChildFoldersAsync(int parentId, int userId);
        Task<IEnumerable<FolderTreeDTO>> GetFolderTreeAsync(int userId);
        Task<Folder> ToggleFavoriteAsync(int folderId, int userId);
        Task<IEnumerable<Folder>> GetFavoriteFoldersAsync(int userId);
        Task<Folder> UpdateTagsAsync(int id, IEnumerable<string> tags, int userId);
        Task BatchDeleteFoldersAsync(IEnumerable<int> folderIds, int userId);
        Task<Folder> MoveFolderAsync(int id, int? newParentFolderId, int userId);
        Task<IEnumerable<Folder>> SearchFoldersAsync(string searchTerm, int userId);
        Task<bool> CheckCircularReferenceAsync(int sourceId, int targetParentId, int userId);
    }
}
