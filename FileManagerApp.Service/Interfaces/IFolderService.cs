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
        Task<Folder> GetFolderByIdAsync(int id);
        Task<IEnumerable<Folder>> GetAllFoldersAsync();
        Task<Folder> CreateFolderAsync(string name, int? parentFolderId, IEnumerable<string> tags);
        Task<(Folder RootFolder, IEnumerable<Domain.Entities.File> Files)> UploadFolderAsync(
        IFormFileCollection files,
        int? parentFolderId,
        IEnumerable<string> tags);
        Task<bool> DeleteFolderAsync(int id);
        Task<Folder> RenameFolderAsync(int id, string newName);
        Task<IEnumerable<Folder>> GetChildFoldersAsync(int parentId);
        Task<IEnumerable<FolderTreeDTO>> GetFolderTreeAsync();
        Task<Folder> ToggleFavoriteAsync(int folderId);
        Task<IEnumerable<Folder>> GetFavoriteFoldersAsync();
    }
}
