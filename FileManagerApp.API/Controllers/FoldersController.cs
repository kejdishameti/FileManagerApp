﻿using FileManagerApp.Data.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FileManagerApp.API.DTO.Folder;
using FileManagerApp.Domain.Entities;

namespace FileManagerApp.API.Controllers
{
    public class FoldersController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        
        public FoldersController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Get api/folders
        // Rertrives all folders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetFolders()
        {
            var folders = await _unitOfWork.Folders.GetAllAsync();
            return Ok(_mapper.Map<IEnumerable<FolderDTO>>(folders));
        }

        // Post api/folders
        // Creates a new folder
        [HttpPost]
        public async Task<ActionResult<FolderDTO>> CreateFolder(CreateFolderDTO createFolderDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(createFolderDto.Name))
                    return BadRequest("Folder name cannot be empty.");

                string parentPath = "";
                if (createFolderDto.ParentFolderId.HasValue)
                {
                    var parentFolder = await _unitOfWork.Folders.GetByIdAsync(createFolderDto.ParentFolderId.Value);
                    if (parentFolder == null)
                        return BadRequest("Parent folder not found");
                    parentPath = parentFolder.Path;
                }

                var folder = Folder.Create(createFolderDto.Name, createFolderDto.ParentFolderId);
                folder.SetPath(parentPath);

                await _unitOfWork.Folders.AddAsync(folder);
                await _unitOfWork.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetFolder),
                    new { id = folder.Id },
                    _mapper.Map<FolderDTO>(folder)
                );
            }
            catch (Exception ex)
            {
                return BadRequest($"Could not create folder: {ex.Message}");
            }
        }

        // Get api/folders/{id}
        // Retrives a folder by id
        [HttpGet("{id}")]
        public async Task<ActionResult<FolderDTO>> GetFolder(int id)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound($"Folder with ID {id} not found");

            return Ok(_mapper.Map<FolderDTO>(folder));
        }

        // Put api/folders/{id}
        // Updates a folder
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFolder(int id, UpdateFolderDTO updateFolderDto)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound($"Folder with ID {id} not found");

            _mapper.Map(updateFolderDto, folder);

            _unitOfWork.Folders.Update(folder);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // Delete api/folders/{id}
        // Marks a folder as deleted (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound($"Folder with ID {id} not found");

            _unitOfWork.Folders.Delete(folder);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFolder(int id, [FromBody] MoveFolderDTO moveFolderDto)
        {
            try
            {
                // Get the folder we want to move
                var folder = await _unitOfWork.Folders.GetByIdAsync(id);
                if (folder == null)
                    return NotFound($"Folder with ID {id} not found");

                if (moveFolderDto.NewParentFolderId.HasValue)
                {
                    var targetFolder = await _unitOfWork.Folders.GetByIdAsync(moveFolderDto.NewParentFolderId.Value);
                    if (targetFolder == null)
                        return BadRequest("Target parent folder not found");

                    // Prevent moving a folder into itself or its children
                    if (await IsFolderCircularReference(id, moveFolderDto.NewParentFolderId.Value))
                        return BadRequest("Cannot move a folder into itself or its children");
                }

                // Update the folder's parent and path
                folder.UpdateParentFolder(moveFolderDto.NewParentFolderId);

                // Get the new parent's path (if any) to update this folder's path
                string parentPath = "";
                if (moveFolderDto.NewParentFolderId.HasValue)
                {
                    var parentFolder = await _unitOfWork.Folders.GetByIdAsync(moveFolderDto.NewParentFolderId.Value);
                    parentPath = parentFolder.Path;
                }
                folder.SetPath(parentPath);

                _unitOfWork.Folders.Update(folder);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while moving the folder");
            }
        }

        private async Task<bool> IsFolderCircularReference(int sourceId, int targetParentId)
        {
            var currentFolder = await _unitOfWork.Folders.GetByIdAsync(targetParentId);
            while (currentFolder != null)
            {
                if (currentFolder.Id == sourceId)
                    return true;

                if (!currentFolder.ParentFolderId.HasValue)
                    break;

                currentFolder = await _unitOfWork.Folders.GetByIdAsync(currentFolder.ParentFolderId.Value);
            }
            return false;
        }

        // Get api/folders/{id}/children
        // Retrives all children of a folder
        [HttpGet("{id}/children")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetChildFolders(int id)
        {
            var childFolders = await _unitOfWork.Folders.GetChildFoldersByParentIdAsync(id);
            return Ok(_mapper.Map<IEnumerable<FolderDTO>>(childFolders));
        }
    }
}
