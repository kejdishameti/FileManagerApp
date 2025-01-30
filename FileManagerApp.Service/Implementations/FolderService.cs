using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Data.UnitOfWork;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.Interfaces;
using FileManagerApp.Service.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using FileManagerApp.Domain.Enums;

namespace FileManagerApp.Service.Implementations
{
    public class FolderService : IFolderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly string _storageBasePath;

        public FolderService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _storageBasePath = configuration["FileStorage:BasePath"];
        }

        public async Task<Folder> GetFolderByIdAsync(int id, int userId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");
            return folder;
        }

        public async Task<IEnumerable<Folder>> GetAllFoldersAsync(int userId)
        {
            return await _unitOfWork.Folders.GetAllAsync(userId);
        }

        public async Task<Folder> CreateFolderAsync(string name, int userId, int? parentFolderId, IEnumerable<string> tags)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name cannot be empty.");

            string parentPath = "";
            if (parentFolderId.HasValue)
            {
                var parentFolder = await _unitOfWork.Folders.GetByIdAsync(parentFolderId.Value, userId);
                if (parentFolder == null)
                    throw new ArgumentException("Parent folder not found");
                parentPath = parentFolder.Path;
            }

            var folder = new Folder
            {
                Name = name,
                ParentFolderId = parentFolderId,
                Path = string.IsNullOrEmpty(parentPath) ? "/" + name : parentPath + "/" + name,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                Tags = tags?.ToList() ?? new List<string>()
            };

            await _unitOfWork.Folders.AddAsync(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<(Folder RootFolder, IEnumerable<Domain.Entities.File> Files)> UploadFolderAsync(
           IFormFileCollection files,
           int userId,
           int? parentFolderId,
           IEnumerable<string> tags)
        {
            var firstFile = files.FirstOrDefault() ??
                throw new ArgumentException("No files were provided in the upload");
            var rootFolderName = firstFile.FileName.Split('/', '\\')[0];
            if (string.IsNullOrWhiteSpace(rootFolderName))
                throw new ArgumentException("Could not determine folder name from upload");

            var rootFolder = await CreateFolderAsync(rootFolderName, userId, parentFolderId, tags);
            var uploadedFiles = new List<Domain.Entities.File>();
            var createdFolderPaths = new HashSet<string>();

            foreach (var file in files)
            {
                try
                {
                    var relativePath = file.FileName.Replace("\\", "/");
                    var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var currentParentId = rootFolder.Id;

                    for (int i = 1; i < pathParts.Length - 1; i++)
                    {
                        var subFolderPath = string.Join("/", pathParts.Take(i + 1));
                        if (!createdFolderPaths.Contains(subFolderPath))
                        {
                            var subFolder = await CreateFolderAsync(
                                pathParts[i],
                                userId,
                                currentParentId,
                                new List<string>());
                            createdFolderPaths.Add(subFolderPath);
                            currentParentId = subFolder.Id;
                        }
                    }

                    string uniqueFileName = $"{Guid.NewGuid()}_{pathParts.Last()}";
                    string filePath = Path.Combine(_storageBasePath, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var fileEntity = new Domain.Entities.File
                    {
                        Name = pathParts.Last(),
                        ContentType = file.ContentType,
                        SizeInBytes = file.Length,
                        StoragePath = filePath,
                        CreatedAt = DateTime.UtcNow,
                        UserId = userId,
                        FolderId = currentParentId,
                        Status = FileStatus.Active
                    };

                    await _unitOfWork.Files.AddAsync(fileEntity);
                    uploadedFiles.Add(fileEntity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file {file.FileName}: {ex.Message}");
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return (rootFolder, uploadedFiles);
        }

        public async Task<bool> DeleteFolderAsync(int id, int userId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null) return false;

            folder.IsDeleted = true;
            folder.DeletedAt = DateTime.UtcNow;

            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task BatchDeleteFoldersAsync(IEnumerable<int> folderIds, int userId)
        {
            if (folderIds == null || !folderIds.Any())
                throw new ArgumentException("No folder IDs provided for deletion");

            await _unitOfWork.Folders.BatchDeleteAsync(folderIds, userId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Folder> RenameFolderAsync(int id, string newName, int userId)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New folder name cannot be empty");

            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");

            var oldPath = folder.Path;

            folder.Name = newName;

            var parentPath = folder.ParentFolder?.Path ?? "";
            folder.Path = string.IsNullOrEmpty(parentPath) ? "/" + newName : parentPath + "/" + newName;

            await UpdateChildFolderPathsAsync(folder, oldPath, folder.Path);

            folder.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        private async Task UpdateChildFolderPathsAsync(Folder folder, string oldPath, string newPath)
        {
            var childFolders = await _unitOfWork.Folders.GetChildFoldersByParentIdAsync(folder.Id, folder.UserId);

            if (!childFolders.Any())
                return;

            foreach (var childFolder in childFolders)
            {
                childFolder.Path = childFolder.Path.Replace(oldPath, newPath);

                await UpdateChildFolderPathsAsync(childFolder, oldPath, newPath);

                _unitOfWork.Folders.Update(childFolder);
            }
        }

        public async Task<IEnumerable<Folder>> SearchFoldersAsync(string searchTerm, int userId)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be empty");

            return await _unitOfWork.Folders.SearchFoldersAsync(searchTerm, userId);
        }

        public async Task<Folder> UpdateTagsAsync(int id, IEnumerable<string> tags, int userId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");

            folder.Tags = tags?.ToList() ?? new List<string>();
            folder.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<bool> CheckCircularReferenceAsync(int sourceId, int targetParentId, int userId)
        {
            var currentFolder = await _unitOfWork.Folders.GetByIdAsync(targetParentId, userId);

            while (currentFolder != null)
            {
                if (currentFolder.Id == sourceId)
                    return true;

                if (!currentFolder.ParentFolderId.HasValue)
                    break;

                currentFolder = await _unitOfWork.Folders.GetByIdAsync(currentFolder.ParentFolderId.Value, userId);
            }

            return false;
        }

        public async Task<Folder> MoveFolderAsync(int id, int? newParentFolderId, int userId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");

            if (newParentFolderId.HasValue)
            {
                var targetFolder = await _unitOfWork.Folders.GetByIdAsync(newParentFolderId.Value, userId);
                if (targetFolder == null)
                    throw new ArgumentException("Target parent folder not found");

                if (await CheckCircularReferenceAsync(id, newParentFolderId.Value, userId))
                    throw new ArgumentException("Cannot move a folder into itself or its children");
            }

            folder.ParentFolderId = newParentFolderId;
            folder.ModifiedAt = DateTime.UtcNow;

            string parentPath = "";
            if (newParentFolderId.HasValue)
            {
                var parentFolder = await _unitOfWork.Folders.GetByIdAsync(newParentFolderId.Value, userId);
                parentPath = parentFolder.Path;
            }
            folder.Path = string.IsNullOrEmpty(parentPath) ? "/" + folder.Name : parentPath + "/" + folder.Name;

            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<IEnumerable<Folder>> GetChildFoldersAsync(int parentId, int userId)
        {
            return await _unitOfWork.Folders.GetChildFoldersByParentIdAsync(parentId, userId);
        }

        public async Task<IEnumerable<FolderTreeDTO>> GetFolderTreeAsync(int userId)
        {
            try
            {
                var allFolders = (await _unitOfWork.Folders.GetAllAsync(userId)).ToList();

                var rootFolders = allFolders.Where(f => !f.ParentFolderId.HasValue).ToList();

                var tree = rootFolders.Select(folder => BuildFolderTreeDTO(folder, allFolders)).ToList();

                return tree;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building folder tree: {ex.Message}");
                throw;
            }
        }

        // Helper method to build the tree using your existing DTO
        private FolderTreeDTO BuildFolderTreeDTO(Folder folder, List<Folder> allFolders)
        {
            var dto = new FolderTreeDTO
            {
                Id = folder.Id,
                Name = folder.Name,
                Children = new List<FolderTreeDTO>()
            };

            // Find and add all children recursively
            var childFolders = allFolders.Where(f => f.ParentFolderId == folder.Id).ToList();
            foreach (var childFolder in childFolders)
            {
                dto.Children.Add(BuildFolderTreeDTO(childFolder, allFolders));
            }

            return dto;
        }

        public async Task<Folder> ToggleFavoriteAsync(int id, int userId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");

            folder.IsFavorite = !folder.IsFavorite;
            folder.ModifiedAt = DateTime.UtcNow;

            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<IEnumerable<Folder>> GetFavoriteFoldersAsync(int userId)
        {
            return await _unitOfWork.Folders.GetFavoriteFoldersAsync(userId);
        }

        private async Task<bool> IsFolderCircularReference(int sourceId, int targetParentId)
        {
            var currentFolder = await _unitOfWork.Folders.GetByIdAsync(targetParentId, userId: 0); 
            while (currentFolder != null)
            {
                if (currentFolder.Id == sourceId)
                    return true;

                if (!currentFolder.ParentFolderId.HasValue)
                    break;

                currentFolder = await _unitOfWork.Folders.GetByIdAsync(currentFolder.ParentFolderId.Value, userId: 0);
            }
            return false;
        }
    }
}
