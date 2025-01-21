﻿using FileManagerApp.Data.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FileManagerApp.API.DTO.Folder;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.Implementations;
using FileManagerApp.Service.Interfaces;
using FileManagerApp.Service.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace FileManagerApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FoldersController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFolderService _folderService;

        public FoldersController(IUnitOfWork unitOfWork, IMapper mapper, IFolderService folderService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _folderService = folderService;
        }

        // GET: api/folders
        // Retrieves all folders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetFolders()
        {
            try
            {
                var folders = await _unitOfWork.Folders.GetAllAsync();
                var folderDtos = folders.Select(f => new FolderDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt,
                    ParentFolderId = f.ParentFolderId,
                    Tags = f.Tags.ToList()
                });

                return Ok(folderDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving folders: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving folders");
            }
        }

        // POST: api/folders
        // Creates a new folder
        [HttpPost("create")]
        public async Task<ActionResult<FolderDTO>> CreateFolder(CreateFolderDTO createFolderDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(createFolderDto.Name))
                    return BadRequest("Folder name cannot be empty.");

                var folder = await _folderService.CreateFolderAsync(
                    createFolderDto.Name,
                    createFolderDto.ParentFolderId,
                    createFolderDto.Tags
                );

                var response = new FolderDTO
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Path = folder.Path,
                    CreatedAt = folder.CreatedAt,
                    ParentFolderId = folder.ParentFolderId,
                    Tags = folder.Tags.ToList(),
                    IsFavorite = folder.IsFavorite
                };

                return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating folder: {ex.Message}");
                return BadRequest($"Could not create folder: {ex.Message}");
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult<FolderUploadResponseDTO>> UploadFolder([FromForm] FolderUploadDTO uploadDto)
        {
            try
            {
                if (uploadDto.Files == null || uploadDto.Files.Count == 0)
                    return BadRequest("No files were provided.");

                (Folder rootFolder, IEnumerable<Domain.Entities.File> uploadedFiles) = await _folderService.UploadFolderAsync(
                    uploadDto.Files,
                    uploadDto.ParentFolderId,
                    uploadDto.Tags  
                );

                var response = new FolderUploadResponseDTO
                {
                    FolderId = rootFolder.Id,
                    FolderName = rootFolder.Name,
                    Path = rootFolder.Path,
                    TotalFiles = uploadedFiles.Count(),
                    TotalSize = uploadedFiles.Sum(f => f.SizeInBytes),
                    CreatedFolders = uploadedFiles.Select(f => f.Folder?.Path ?? "").Distinct()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading folder: {ex.Message}");
                return BadRequest($"Could not upload folder: {ex.Message}");
            }
        }

        // GET: api/folders/{id}
        // Retrieves a specific folder
        [HttpGet("{id}")]
        public async Task<ActionResult<FolderDTO>> GetFolder(int id)
        {
            var folder = await _unitOfWork.Folders.GetByIdAsync(id);

            if (folder == null)
                return NotFound($"Folder with ID {id} not found");

            var response = new FolderDTO
            {
                Id = folder.Id,
                Name = folder.Name,
                Path = folder.Path,
                CreatedAt = folder.CreatedAt,
                ParentFolderId = folder.ParentFolderId,
                Tags = folder.Tags.ToList()
            };

            return Ok(response);
        }

        // PUT: api/folders/{id}
        // Updates folder name
        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameFolder(int id, [FromBody] RenameFolderDTO renameDto)
        {
            try
            {
                // Validate the DTO
                if (string.IsNullOrWhiteSpace(renameDto.NewName))
                    return BadRequest("Folder name cannot be empty");

                var folder = await _folderService.RenameFolderAsync(id, renameDto.NewName);

                var response = new FolderDTO
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Path = folder.Path,
                    CreatedAt = folder.CreatedAt,
                    ParentFolderId = folder.ParentFolderId,
                    Tags = folder.Tags.ToList()
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while renaming the folder");
            }
        }

        // PUT: api/folders/{id}/tags
        // Updates folder tags
        [HttpPut("{id}/tags")]
        public async Task<IActionResult> UpdateFolderTags(int id, [FromBody] UpdateFolderTagsDTO tagsDto)
        {
            try
            {
                var folder = await _unitOfWork.Folders.GetByIdAsync(id);
                if (folder == null)
                    return NotFound($"Folder with ID {id} not found");

                folder.UpdateTags(tagsDto.Tags);
                _unitOfWork.Folders.Update(folder);
                await _unitOfWork.SaveChangesAsync();

                var response = new FolderDTO
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Path = folder.Path,
                    CreatedAt = folder.CreatedAt,
                    ParentFolderId = folder.ParentFolderId,
                    Tags = folder.Tags.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating folder tags");
            }
        }

        // DELETE: api/folders/{id}
        // Soft deletes a folder
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

        // POST: api/folders/batch-delete
        // Soft deletes multiple folders
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFolders([FromBody] BatchDeleteFoldersDTO deleteDto)
        {
            try
            {
                if (deleteDto.FolderIds == null || !deleteDto.FolderIds.Any())
                {
                    return BadRequest("No folder IDs provided for deletion");
                }

                await _unitOfWork.Folders.BatchDeleteAsync(deleteDto.FolderIds);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during batch folder deletion: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting folders");
            }
        }

        // PUT: api/folders/{id}/move
        // Moves a folder to a new parent
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFolder(int id, [FromBody] MoveFolderDTO moveFolderDto)
        {
            try
            {
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

                folder.UpdateParentFolder(moveFolderDto.NewParentFolderId);

                // Update the path
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

        // Helper method to prevent circular references when moving folders
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

        // GET: api/folders/{id}/children
        // Retrieves all children of a folder
        [HttpGet("{id}/children")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetChildFolders(int id)
        {
            var childFolders = await _unitOfWork.Folders.GetChildFoldersByParentIdAsync(id);
            var response = childFolders.Select(f => new FolderDTO
            {
                Id = f.Id,
                Name = f.Name,
                Path = f.Path,
                CreatedAt = f.CreatedAt,
                ParentFolderId = f.ParentFolderId,
                Tags = f.Tags.ToList()
            });

            return Ok(response);
        }

        // GET: api/folders/tree
        // Retrieves the folder hierarchy as a tree structure
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<FolderTreeDTO>>> GetFolderTree()
        {
            try
            {
                // Get the complete folder tree from the service
                var folderTree = await _folderService.GetFolderTreeAsync();

                // Log the successful retrieval
                Console.WriteLine("Successfully retrieved folder tree structure");

                return Ok(folderTree);
            }
            catch (Exception ex)
            {
                // Log any errors that occur
                Console.WriteLine($"Error retrieving folder tree: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the folder structure");
            }
        }

        // GET: api/folders/search
        // Searches for folders by name, path, or tags
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> SearchFolders([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest("Search term cannot be empty");

                var folders = await _unitOfWork.Folders.SearchFoldersAsync(searchTerm);
                var response = folders.Select(f => new FolderDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt,
                    ParentFolderId = f.ParentFolderId,
                    Tags = f.Tags.ToList()
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching folders: {ex.Message}");
                return StatusCode(500, "An error occurred while searching folders");
            }
        }

        // PUT: api/folders/{id}/favorite
        [HttpPut("{id}/favorite")]
        public async Task<ActionResult<FolderDTO>> ToggleFavorite(int id)
        {
            try
            {
                var folder = await _folderService.ToggleFavoriteAsync(id);

                var response = new FolderDTO
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Path = folder.Path,
                    CreatedAt = folder.CreatedAt,
                    ParentFolderId = folder.ParentFolderId,
                    Tags = folder.Tags.ToList(),
                    IsFavorite = folder.IsFavorite  
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while toggling favorite status");
            }
        }

        // GET: api/folders/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetFavorites()
        {
            try
            {
                var favorites = await _folderService.GetFavoriteFoldersAsync();

                var response = favorites.Select(f => new FolderDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt,
                    ParentFolderId = f.ParentFolderId,
                    Tags = f.Tags.ToList(),
                    IsFavorite = f.IsFavorite
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving favorite folders");
            }
        }
    }
}