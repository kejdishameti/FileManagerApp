using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainFile = FileManagerApp.Domain.Entities.File;
using Microsoft.AspNetCore.Http;

namespace FileManagerApp.Service.Interfaces
{
    public interface IFileService
    {
        Task<DomainFile> GetFileByIdAsync(int id, int userId);
        Task<IEnumerable<DomainFile>> GetAllFilesAsync(int userId);
        Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId, int userId);
        Task<DomainFile> UploadFileAsync(IFormFile file, int userId, int? folderId = null, IEnumerable<string>? tags = null);
        Task<bool> DeleteFileAsync(int id, int userId);
        Task<DomainFile> RenameFileAsync(int id, string newName, int userId);
        Task<bool> MoveFileAsync(int id, int? newFolderId, int userId);
        Task<Stream> DownloadFileAsync(int id, int userId);
        Task<DomainFile> CopyFileAsync(int sourceFileId, int targetFolderId, int userId);
        Task<DomainFile> ToggleFavoriteAsync(int fileId, int userId);
        Task<IEnumerable<DomainFile>> GetFavoriteFilesAsync(int userId);
    }
}
