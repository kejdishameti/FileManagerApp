using FileManagerApp.Data.UnitOfWork;
using FileManagerApp.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using DomainFile = FileManagerApp.Domain.Entities.File;

namespace FileManagerApp.Service.Implementations
{
    public class FileService : IFileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly string _storageBasePath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedTypes;

        public FileService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _storageBasePath = _configuration["FileStorage:BasePath"];
            _maxFileSize = _configuration.GetValue<long>("FileStorage:MaxFileSize", 100 * 1024 * 1024); // Default 100MB
            _allowedTypes = _configuration.GetSection("FileStorage:AllowedTypes").Get<string[]>() ??
                new[] { "image/jpeg", "image/png", "application/pdf", "text/plain" };
        }

        public async Task<DomainFile> GetFileByIdAsync(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");
            return file;
        }

        public async Task<IEnumerable<DomainFile>> GetAllFilesAsync()
        {
            return await _unitOfWork.Files.GetAllAsync();
        }

        public async Task<IEnumerable<DomainFile>> GetFilesByFolderIdAsync(int folderId)
        {
            return await _unitOfWork.Files.GetFilesByFolderIdAsync(folderId);
        }

        public async Task<DomainFile> UploadFileAsync(IFormFile file, int? folderId = null, IEnumerable<string>? tags = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided or file is empty");

            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds the limit of {_maxFileSize / 1024 / 1024}MB");

            if (!_allowedTypes.Contains(file.ContentType))
                throw new ArgumentException($"File type {file.ContentType} is not allowed");

            if (folderId.HasValue)
            {
                var folder = await _unitOfWork.Folders.GetByIdAsync(folderId.Value);
                if (folder == null)
                    throw new ArgumentException($"Folder with ID {folderId.Value} not found");
            }

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(_storageBasePath, uniqueFileName);
            Directory.CreateDirectory(_storageBasePath);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileEntity = DomainFile.Create(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    filePath
                );

                if (folderId.HasValue)
                {
                    fileEntity.MoveToFolder(folderId);
                }

                fileEntity.UpdateTags(tags ?? Enumerable.Empty<string>());

                await _unitOfWork.Files.AddAsync(fileEntity);
                await _unitOfWork.SaveChangesAsync();

                return fileEntity;
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);
            if (file == null) return false;

            file.MarkAsDeleted();
            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<DomainFile> UpdateFileAsync(int id, string newName, IEnumerable<string> tags)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            // Update tags if provided
            if (tags != null)
            {
                file.UpdateTags(tags);
            }

            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            return file;
        }

        public async Task<DomainFile> RenameFileAsync(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New file name cannot be empty");

            var file = await _unitOfWork.Files.GetByIdAsync(id);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            file.Rename(newName);

            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            return file;
        }

        public async Task<bool> MoveFileAsync(int id, int? newFolderId)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);
            if (file == null) return false;

            if (newFolderId.HasValue)
            {
                var targetFolder = await _unitOfWork.Folders.GetByIdAsync(newFolderId.Value);
                if (targetFolder == null)
                    throw new ArgumentException($"Target folder with ID {newFolderId.Value} not found");
            }

            file.MoveToFolder(newFolderId);
            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Stream> DownloadFileAsync(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);
            if (file == null)
                throw new ArgumentException($"File with ID {id} not found");

            if (!System.IO.File.Exists(file.StoragePath))
                throw new FileNotFoundException("Physical file is missing");

            return System.IO.File.OpenRead(file.StoragePath);
        }

        public async Task<IEnumerable<DomainFile>> SearchFilesByTagAsync(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be empty");

            return await _unitOfWork.Files.GetFilesByTagAsync(tag);
        }

        public async Task<DomainFile> CopyFileAsync(int sourceFileId, int targetFolderId)
        {
            var sourceFile = await _unitOfWork.Files.GetByIdAsync(sourceFileId);
            if (sourceFile == null)
                throw new ArgumentException($"File with ID {sourceFileId} not found");

            var targetFolder = await _unitOfWork.Folders.GetByIdAsync(targetFolderId);
            if (targetFolder == null)
                throw new ArgumentException($"Target folder with ID {targetFolderId} not found");

            string newFileName = $"Copy of {sourceFile.Name}";

            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(newFileName)}";
            string newFilePath = Path.Combine(_storageBasePath, uniqueFileName);

            try
            {
                System.IO.File.Copy(sourceFile.StoragePath, newFilePath);

                var newFile = DomainFile.Create(
                    newFileName,
                    sourceFile.ContentType,
                    new FileInfo(newFilePath).Length,
                    newFilePath
                );

                newFile.MoveToFolder(targetFolderId);

                newFile.UpdateTags(sourceFile.Tags);

                await _unitOfWork.Files.AddAsync(newFile);
                await _unitOfWork.SaveChangesAsync();

                return newFile;
            }
            catch
            {
                if (System.IO.File.Exists(newFilePath))
                {
                    System.IO.File.Delete(newFilePath);
                }
                throw;
            }
        }

        public async Task<DomainFile> ToggleFavoriteAsync(int fileId)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(fileId);
            if (file == null)
                throw new ArgumentException($"File with ID {fileId} not found");

            file.ToggleFavorite();
            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();

            return file;
        }

        public async Task<IEnumerable<DomainFile>> GetFavoriteFilesAsync()
        {
            return await _unitOfWork.Files.GetFavoriteFilesAsync();
        }
    }
}