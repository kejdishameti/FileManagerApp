using FileManagerApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Service.DTOs;

namespace FileManagerApp.Service.Interfaces
{
    public interface IFolderService
    {
        Task<Folder> GetFolderByIdAsync(int id);
        Task<IEnumerable<Folder>> GetAllFoldersAsync();
        Task<Folder> CreateFolderAsync(string name, int? parentFolderId);
        Task<bool> DeleteFolderAsync(int id);
        Task<Folder> UpdateFolderAsync(int id, string newName);
        Task<IEnumerable<Folder>> GetChildFoldersAsync(int parentId);
        Task<IEnumerable<FolderTreeDTO>> GetFolderTreeAsync();
        Task<Folder> ToggleFavoriteAsync(int folderId);
        Task<IEnumerable<Folder>> GetFavoriteFoldersAsync();
    }
}
