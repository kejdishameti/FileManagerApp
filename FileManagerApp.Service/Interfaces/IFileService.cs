using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainFile = FileManagerApp.Domain.Entities.File;
using FileManagerApp.Service.DTOs.File;
using Microsoft.AspNetCore.Http;

namespace FileManagerApp.Service.Interfaces
{
    public interface IFileService
    {
        Task<IEnumerable<DomainFile>> GetFilesAsync(int userId, int? folderId);
        Task<DomainFile> GetFileByIdAsync(int id, int userId);
        Task<DomainFile> UploadFileAsync(CreateFileDTO dto, int userId);
        Task<bool> DeleteFileAsync(int id, int userId);
        Task BatchDeleteFilesAsync(IEnumerable<int> fileIds, int userId);
        Task<DomainFile> RenameFileAsync(int id, string newName, int userId);
        Task MoveFileAsync(int id, int? newFolderId, int userId);
        Task<FileDownloadResult> DownloadFileAsync(int id, int userId);
        Task<IEnumerable<DomainFile>> SearchFilesAsync(string term, int userId);
        Task<DomainFile> CopyFileAsync(int sourceFileId, int targetFolderId, int userId);
        Task<DomainFile> ToggleFavoriteAsync(int id, int userId);
        Task<IEnumerable<DomainFile>> GetFavoriteFilesAsync(int userId);
        Task<DomainFile> UpdateFileTagsAsync(int id, IEnumerable<string> tags, int userId);
        Task<(byte[] FileData, string ContentType)?> GetPreviewAsync(int id, int userId);
    }
}
