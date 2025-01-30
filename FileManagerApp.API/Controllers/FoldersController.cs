using FileManagerApp.Data.UnitOfWork;
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
        private readonly IMapper _mapper;
        private readonly IFolderService _folderService;

        public FoldersController(IMapper mapper, IFolderService folderService)
        {
            _mapper = mapper;
            _folderService = folderService;
        }

        // GET: api/folders
        // Retrieves all folders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetFolders()
        {
            var userId = GetCurrentUserId();
            var folders = await _folderService.GetAllFoldersAsync(userId);
            var folderDtos = _mapper.Map<IEnumerable<FolderDTO>>(folders);
            return Ok(folderDtos);
        }

        // POST: api/folders
        // Creates a new folder
        [HttpPost("create")]
        public async Task<ActionResult<FolderDTO>> CreateFolder(CreateFolderDTO createFolderDto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(createFolderDto.Name))
                return BadRequest("Folder name cannot be empty.");

            var folder = await _folderService.CreateFolderAsync(
                createFolderDto.Name,
                userId: userId,
                createFolderDto.ParentFolderId,
                createFolderDto.Tags
            );

            var response = _mapper.Map<FolderDTO>(folder);
            return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, response);
        }

        [HttpPost("upload")]
        public async Task<ActionResult<FolderUploadResponseDTO>> UploadFolder([FromForm] FolderUploadDTO uploadDto)
        {
            var userId = GetCurrentUserId();

            if (uploadDto.Files == null || uploadDto.Files.Count == 0)
                return BadRequest("No files were provided.");

            var (rootFolder, uploadedFiles) = await _folderService.UploadFolderAsync(
                uploadDto.Files,
                userId,
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

        // GET: api/folders/{id}
        // Retrieves a specific folder
        [HttpGet("{id}")]
        public async Task<ActionResult<FolderDTO>> GetFolder(int id)
        {
            var userId = GetCurrentUserId();
            var folder = await _folderService.GetFolderByIdAsync(id, userId);

            if (folder == null)
                return NotFound($"Folder with ID {id} not found");

            var response = _mapper.Map<FolderDTO>(folder);
            return Ok(response);
        }

        // PUT: api/folders/{id}
        // Updates folder name
        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameFolder(int id, [FromBody] RenameFolderDTO renameDto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(renameDto.NewName))
                return BadRequest("Folder name cannot be empty");

            var folder = await _folderService.RenameFolderAsync(id, renameDto.NewName, userId);
            var response = _mapper.Map<FolderDTO>(folder);
            return Ok(response);
        }

        // PUT: api/folders/{id}/tags
        // Updates folder tags
        [HttpPut("{id}/tags")]
        public async Task<IActionResult> UpdateFolderTags(int id, [FromBody] UpdateFolderTagsDTO tagsDto)
        {
            var userId = GetCurrentUserId();
            var folder = await _folderService.UpdateTagsAsync(id, tagsDto.Tags, userId);
            var response = _mapper.Map<FolderDTO>(folder);
            return Ok(response);
        }

        // DELETE: api/folders/{id}
        // Soft deletes a folder
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _folderService.DeleteFolderAsync(id, userId);

            if (!result)
                return NotFound($"Folder with ID {id} not found");

            return NoContent();
        }

        // POST: api/folders/batch-delete
        // Soft deletes multiple folders
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFolders([FromBody] BatchDeleteFoldersDTO deleteDto)
        {
            var userId = GetCurrentUserId();
            if (deleteDto.FolderIds == null || !deleteDto.FolderIds.Any())
                return BadRequest("No folder IDs provided for deletion");

            await _folderService.BatchDeleteFoldersAsync(deleteDto.FolderIds, userId);
            return NoContent();
        }

        // PUT: api/folders/{id}/move
        // Moves a folder to a new parent
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFolder(int id, [FromBody] MoveFolderDTO moveFolderDto)
        {
            var userId = GetCurrentUserId();
            var folder = await _folderService.MoveFolderAsync(id, moveFolderDto.NewParentFolderId, userId);
            var response = _mapper.Map<FolderDTO>(folder);
            return Ok(response);
        }

        // GET: api/folders/{id}/children
        // Retrieves all children of a folder
        [HttpGet("{id}/children")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetChildFolders(int id)
        {
            var userId = GetCurrentUserId();
            var childFolders = await _folderService.GetChildFoldersAsync(id, userId);
            var response = _mapper.Map<IEnumerable<FolderDTO>>(childFolders);
            return Ok(response);
        }

        // GET: api/folders/tree
        // Retrieves the folder hierarchy as a tree structure
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<FolderTreeDTO>>> GetFolderTree()
        {
            var userId = GetCurrentUserId();
            var folderTree = await _folderService.GetFolderTreeAsync(userId);
            return Ok(folderTree);
        }

        // GET: api/folders/search
        // Searches for folders by name, path, or tags
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> SearchFolders([FromQuery] string searchTerm)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest("Search term cannot be empty");

            var folders = await _folderService.SearchFoldersAsync(searchTerm, userId);
            var response = _mapper.Map<IEnumerable<FolderDTO>>(folders);
            return Ok(response);
        }

        // PUT: api/folders/{id}/favorite
        [HttpPut("{id}/favorite")]
        public async Task<ActionResult<FolderDTO>> ToggleFavorite(int id)
        {
            var userId = GetCurrentUserId();
            var folder = await _folderService.ToggleFavoriteAsync(id, userId);
            var response = _mapper.Map<FolderDTO>(folder);
            return Ok(response);
        }

        // GET: api/folders/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<FolderDTO>>> GetFavorites()
        {
            var userId = GetCurrentUserId();
            var favorites = await _folderService.GetFavoriteFoldersAsync(userId);
            var response = _mapper.Map<IEnumerable<FolderDTO>>(favorites);
            return Ok(response);
        }
    }
}