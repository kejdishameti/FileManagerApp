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

            var folder = Folder.Create(name, parentFolderId, userId);
            folder.SetPath(parentPath);

            folder.UpdateTags(tags ?? new List<string>());

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

                    var fileEntity = Domain.Entities.File.Create(
                        pathParts.Last(), 
                        file.ContentType,
                        file.Length,
                        filePath,
                        userId
                    );

                    fileEntity.MoveToFolder(currentParentId);

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

            folder.MarkAsDeleted();
            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Folder> RenameFolderAsync(int id, string newName, int userId)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New folder name cannot be empty");

            var folder = await _unitOfWork.Folders.GetByIdAsync(id, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");

            var updatedFolder = Folder.Create(newName, folder.ParentFolderId, userId:folder.UserId);
            updatedFolder.SetPath(folder.ParentFolder?.Path ?? "");

            _unitOfWork.Folders.Update(updatedFolder);
            await _unitOfWork.SaveChangesAsync();

            return updatedFolder;
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

                // Find root folders (those without parents)
                var rootFolders = allFolders.Where(f => !f.ParentFolderId.HasValue).ToList();

                // Build tree structure starting from root folders
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

        public async Task<Folder> ToggleFavoriteAsync(int folderId, int userId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(folderId, userId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {folderId} not found");

            folder.ToggleFavorite();
            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<IEnumerable<Folder>> GetFavoriteFoldersAsync(int userId)
        {
            return await _unitOfWork.Folders.GetFavoriteFoldersAsync(userId);
        }
    }
}
