using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Data.UnitOfWork;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.Interfaces;

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
    }
}
