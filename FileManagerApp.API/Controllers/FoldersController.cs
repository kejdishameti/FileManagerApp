using FileManagerApp.Data.UnitOfWork;
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
                    Console.WriteLine($"Creating folder in parent path: {parentPath}");
                }
                else
                {
                    Console.WriteLine("Creating root-level folder");
                }

                var folder = Folder.Create(createFolderDto.Name, createFolderDto.ParentFolderId);

                folder.SetPath(parentPath);  

                await _unitOfWork.Folders.AddAsync(folder);
                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine($"Successfully created folder: {folder.Path}");

                return CreatedAtAction(
                    nameof(GetFolder),
                    new { id = folder.Id },
                    _mapper.Map<FolderDTO>(folder)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating folder: {ex.Message}");
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
        // Updates folder properties
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

        // Updates or adds custom metadata key-value pairs for the folder
        [HttpPut("{id}/metadata")]
        public async Task<IActionResult> UpdateFolderMetadata(int id, [FromBody] UpdateFolderMetadataDTO metadataDto)
        {
            try
            {
                // Get the folder
                var folder = await _unitOfWork.Folders.GetByIdAsync(id);
                if (folder == null)
                    return NotFound($"Folder with ID {id} not found");

                Console.WriteLine($"Updating metadata for folder {id} - {folder.Name}");

                // Update each piece of metadata
                foreach (var item in metadataDto.Metadata)
                {
                    folder.AddOrUpdateMetadata(item.Key, item.Value);
                    Console.WriteLine($"Added/Updated metadata: {item.Key} = {item.Value}");
                }

                // Save changes
                _unitOfWork.Folders.Update(folder);
                await _unitOfWork.SaveChangesAsync();

                // Prepare response
                var response = new FolderDTO
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Path = folder.Path,
                    CreatedAt = folder.CreatedAt,
                    ParentFolderId = folder.ParentFolderId,
                    Metadata = new Dictionary<string, string>(folder.Metadata)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating folder metadata: {ex.Message}");
                return StatusCode(500, "An error occurred while updating folder metadata");
            }
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

        // Post api/folders/batch-delete
        // Marks multiple folders as deleted (soft delete)
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFolders([FromBody] BatchDeleteFoldersDTO deleteDto)
        {
            try
            {
                if (deleteDto.FolderIds == null || !deleteDto.FolderIds.Any())
                {
                    return BadRequest("No folder IDs provided for deletion");
                }

                Console.WriteLine($"Starting batch deletion of {deleteDto.FolderIds.Count} folders");

                await _unitOfWork.Folders.BatchDeleteAsync(deleteDto.FolderIds);

                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine("Batch folder deletion completed successfully");

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during batch folder deletion: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, "An error occurred while deleting folders");
            }
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

        // Get api/folders/tree
        // Retrives the folder structure as a tree
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<FolderTreeDTO>>> GetFolderTree()
        {
            try
            {
                // First, get all folders
                var allFolders = await _unitOfWork.Folders.GetAllAsync();

                // Start with root folders (those without a parent)
                var rootFolders = allFolders.Where(f => !f.ParentFolderId.HasValue).ToList();

                // Build tree starting from root folders
                var treeView = rootFolders.Select(folder => BuildFolderTree(folder, allFolders)).ToList();

                return Ok(treeView);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the folder structure");
            }
        }

        // Helper method to build the tree structure recursively
        private FolderTreeDTO BuildFolderTree(Folder folder, IEnumerable<Folder> allFolders)
        {
            // Find all children of current folder
            var children = allFolders
                .Where(f => f.ParentFolderId == folder.Id)
                .Select(childFolder => BuildFolderTree(childFolder, allFolders))
                .ToList();

            // Create the DTO with folder info and its children
            return new FolderTreeDTO
            {
                Id = folder.Id,
                Name = folder.Name,
                Children = children
            };
        }

        // Get api/folders/search
        // Searches for folders by name or path
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> SearchFolders([FromQuery] FolderSearchDTO searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                    return BadRequest("Search term cannot be empty");

                var folders = await _unitOfWork.Folders.SearchFoldersAsync(searchDto.SearchTerm);

                var response = folders.Select(f => new FolderDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    CreatedAt = f.CreatedAt,
                    ParentFolderId = f.ParentFolderId
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching folders: {ex.Message}");
                return StatusCode(500, "An error occurred while searching folders");
            }
        }
    }
}
