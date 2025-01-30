using FileManagerApp.Data.UnitOfWork;
using FileManagerApp.Domain.Enums;
using FileManagerApp.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using DomainFile = FileManagerApp.Domain.Entities.File;
using FileManagerApp.Service.DTOs.File;
using System.IO;
using IOFile = System.IO.File;

namespace FileManagerApp.Service.Implementations
{
    public class FileService : IFileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly string _storageBasePath;

        public FileService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _storageBasePath = _configuration["FileStorage:BasePath"];
        }

        public async Task<IEnumerable<DomainFile>> GetFilesAsync(int userId, int? folderId)
        {
            if (folderId.HasValue)
            {
                return await _unitOfWork.Files.GetFilesByFolderIdAsync(folderId.Value, userId);
            }
            return await _unitOfWork.Files.GetAllAsync(userId);
        }

        public async Task<DomainFile> GetFileByIdAsync(int id, int userId)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            return file;
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId, int userId)
        {
            return await _unitOfWork.Files.GetFilesByFolderIdAsync(folderId, userId);
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByTagAsync(string tag, int userId)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be empty");

            return await _unitOfWork.Files.GetFilesByTagAsync(tag, userId);
        }

        public async Task<DomainFile> UploadFileAsync(CreateFileDTO dto, int userId)
        {
            ValidateFileUpload(dto.File);

            if (dto.FolderId.HasValue)
            {
                var folder = await _unitOfWork.Folders.GetByIdAsync(dto.FolderId.Value, userId);
                if (folder == null)
                    throw new ArgumentException($"Folder with ID {dto.FolderId.Value} not found");
            }

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.File.FileName)}";
            string filePath = Path.Combine(_storageBasePath, uniqueFileName);

            Directory.CreateDirectory(_storageBasePath);

            try
            {
                await SavePhysicalFile(dto.File, filePath);

                var file = new DomainFile
                {
                    Name = dto.File.FileName,
                    ContentType = dto.File.ContentType,
                    SizeInBytes = dto.File.Length,
                    StoragePath = filePath,
                    UserId = userId,
                    FolderId = dto.FolderId,
                    Status = FileStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    Tags = dto.Tags?.ToList() ?? new List<string>()
                };

                await _unitOfWork.Files.AddAsync(file);
                await _unitOfWork.SaveChangesAsync();

                return file;
            }
            catch
            {
                if (IOFile.Exists(filePath))
                {
                    IOFile.Delete(filePath);
                }
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(int id, int userId)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);
            if (file == null)
                return false;

            file.IsDeleted = true;
            file.DeletedAt = DateTime.UtcNow;
            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task BatchDeleteFilesAsync(IEnumerable<int> fileIds, int userId)
        {
            if (fileIds == null || !fileIds.Any())
                throw new ArgumentException("No file IDs provided for deletion");

            await _unitOfWork.Files.BatchDeleteAsync(fileIds, userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<DomainFile> UpdateFileAsync(int id, string newName, IEnumerable<string> tags, int userId)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            var normalizedTags = tags?.Select(t => t.Trim().ToLower())
                           .Where(t => !string.IsNullOrWhiteSpace(t))
                           .Distinct()
                           .ToList() ?? new List<string>();

            file.Tags = normalizedTags;
            file.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            return file;
        }

        public async Task<DomainFile> RenameFileAsync(int id, string newName, int userId)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New file name cannot be empty");

            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            string currentExtension = Path.GetExtension(file.Name);
            bool hasNewExtension = Path.HasExtension(newName);
            string finalName = hasNewExtension ? newName : newName + currentExtension;

            file.Name = finalName;
            file.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            return file;
        }

        public async Task MoveFileAsync(int id, int? newFolderId, int userId)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            if (newFolderId.HasValue)
            {
                var targetFolder = await _unitOfWork.Folders.GetByIdAsync(newFolderId.Value, userId);
                if (targetFolder == null)
                    throw new ArgumentException($"Target folder with ID {newFolderId.Value} not found");
            }

            file.FolderId = newFolderId;
            file.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<FileDownloadResult> DownloadFileAsync(int id, int userId)
        {
            var file = await GetFileByIdAsync(id, userId);

            if (!File.Exists(file.StoragePath))
                throw new FileNotFoundException("Physical file is missing");

            return new FileDownloadResult
            {
                Stream = File.OpenRead(file.StoragePath),
                ContentType = file.ContentType,
                FileName = file.Name
            };
        }

        public async Task<IEnumerable<DomainFile>> SearchFilesAsync(string term, int userId)
        {
            if (string.IsNullOrWhiteSpace(term))
                throw new ArgumentException("Search term cannot be empty");

            return await _unitOfWork.Files.SearchFilesAsync(term, userId);
        }

        public async Task<DomainFile> CopyFileAsync(int sourceFileId, int targetFolderId, int userId)
        {
            var sourceFile = await _unitOfWork.Files.GetByIdAsync(sourceFileId, userId);
            if (sourceFile == null)
                throw new ArgumentException($"Source file with ID {sourceFileId} not found");

            var targetFolder = await _unitOfWork.Folders.GetByIdAsync(targetFolderId, userId);
            if (targetFolder == null)
                throw new ArgumentException($"Target folder with ID {targetFolderId} not found");

            string newFileName = $"Copy of {sourceFile.Name}";
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(newFileName)}";
            string newFilePath = Path.Combine(_storageBasePath, uniqueFileName);

            try
            {
                IOFile.Copy(sourceFile.StoragePath, newFilePath);

                var newFile = new DomainFile
                {
                    Name = newFileName,
                    ContentType = sourceFile.ContentType,
                    SizeInBytes = new FileInfo(newFilePath).Length,
                    StoragePath = newFilePath,
                    CreatedAt = DateTime.UtcNow,
                    Status = FileStatus.Active,
                    UserId = userId,
                    FolderId = targetFolderId,
                    Tags = new List<string>(sourceFile.Tags)
                };

                await _unitOfWork.Files.AddAsync(newFile);
                await _unitOfWork.SaveChangesAsync();

                return newFile;
            }
            catch
            {
                if (IOFile.Exists(newFilePath))
                {
                    IOFile.Delete(newFilePath);
                }
                throw;
            }
        }

        public async Task<DomainFile> ToggleFavoriteAsync(int id, int userId)
        {
            var file = await GetFileByIdAsync(id, userId);

            file.IsFavorite = !file.IsFavorite;
            file.ModifiedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return file;
        }

        public async Task<IEnumerable<DomainFile>> GetFavoriteFilesAsync(int userId)
        {
            return await _unitOfWork.Files.GetFavoriteFilesAsync(userId);
        }

        public async Task<DomainFile> UpdateFileTagsAsync(int id, IEnumerable<string> tags, int userId)
        {
            var file = await _unitOfWork.Files.UpdateTagsAsync(id, tags, userId);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            await _unitOfWork.SaveChangesAsync();
            return file;
        }

        private void ValidateFileUpload(IFormFile file)
        {
            var maxFileSize = _configuration.GetValue<long>("FileStorage:MaxFileSize", 100 * 1024 * 1024);
            var allowedTypes = _configuration.GetSection("FileStorage:AllowedTypes").Get<string[]>() ??
                new[] { "image/jpeg", "image/png", "application/pdf", "text/plain" };

            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided or file is empty");

            if (file.Length > maxFileSize)
                throw new ArgumentException($"File size exceeds the limit of {maxFileSize / 1024 / 1024}MB");

            if (!allowedTypes.Contains(file.ContentType))
                throw new ArgumentException($"File type {file.ContentType} is not allowed");
        }

        private async Task SavePhysicalFile(IFormFile file, string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }
    }
}
