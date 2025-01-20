using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Data.UnitOfWork;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.Interfaces;
using FileManagerApp.Service.DTOs;

namespace FileManagerApp.Service.Implementations
{
    public class FolderService : IFolderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FolderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Folder> GetFolderByIdAsync(int id)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");
            return folder;
        }

        public async Task<IEnumerable<Folder>> GetAllFoldersAsync()
        {
            return await _unitOfWork.Folders.GetAllAsync();
        }

        public async Task<Folder> CreateFolderAsync(string name, int? parentFolderId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Folder name cannot be empty.");

            string parentPath = "";
            if (parentFolderId.HasValue)
            {
                var parentFolder = await _unitOfWork.Folders.GetByIdAsync(parentFolderId.Value);
                if (parentFolder == null)
                    throw new ArgumentException("Parent folder not found");
                parentPath = parentFolder.Path;
            }

            var folder = Folder.Create(name, parentFolderId);
            folder.SetPath(parentPath);

            await _unitOfWork.Folders.AddAsync(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<bool> DeleteFolderAsync(int id)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id);
            if (folder == null) return false;

            folder.MarkAsDeleted();
            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<Folder> UpdateFolderAsync(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("New folder name cannot be empty");

            var folder = await _unitOfWork.Folders.GetByIdAsync(id);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {id} not found");

            var updatedFolder = Folder.Create(newName, folder.ParentFolderId);
            updatedFolder.SetPath(folder.ParentFolder?.Path ?? "");

            _unitOfWork.Folders.Update(updatedFolder);
            await _unitOfWork.SaveChangesAsync();

            return updatedFolder;
        }

        public async Task<IEnumerable<Folder>> GetChildFoldersAsync(int parentId)
        {
            return await _unitOfWork.Folders.GetChildFoldersByParentIdAsync(parentId);
        }

        public async Task<IEnumerable<FolderTreeDTO>> GetFolderTreeAsync()
        {
            try
            {
                // Get all folders in a single database query
                var allFolders = (await _unitOfWork.Folders.GetAllAsync()).ToList();

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

        public async Task<Folder> ToggleFavoriteAsync(int folderId)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(folderId);
            if (folder == null)
                throw new ArgumentException($"Folder with ID {folderId} not found");

            folder.ToggleFavorite();
            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return folder;
        }

        public async Task<IEnumerable<Folder>> GetFavoriteFoldersAsync()
        {
            return await _unitOfWork.Folders.GetFavoriteFoldersAsync();
        }
    }
}
