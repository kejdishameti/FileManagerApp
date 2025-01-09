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
        Task<DomainFile> GetFileByIdAsync(int id);
        Task<IEnumerable<DomainFile>> GetAllFilesAsync();
        Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId);
        Task<DomainFile> UploadFileAsync(IFormFile file, int? folderId, Dictionary<string, string> metadata);
        Task<bool> DeleteFileAsync(int id);
        Task<DomainFile> UpdateFileAsync(int id, string newName, Dictionary<string, string> metadata);
        Task<bool> MoveFileAsync(int id, int? newFolderId);
        Task<Stream> DownloadFileAsync(int id);
    }
}
