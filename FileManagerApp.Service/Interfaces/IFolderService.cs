using FileManagerApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerApp.Service.Interfaces
{
    internal interface IFolderService
    {
        Task<Folder> GetFolderByIdAsync(int id);
        Task<IEnumerable<Folder>> GetAllFoldersAsync();
        Task<Folder> CreateFolderAsync(string name, int? parentFolderId);
        Task<bool> DeleteFolderAsync(int id);
        Task<Folder> UpdateFolderAsync(int id, string newName);
        Task<IEnumerable<Folder>> GetChildFoldersAsync(int parentId);
    }
}
