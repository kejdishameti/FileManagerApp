using FileManagerApp.Data.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FileManagerApp.API.DTO.File;
using Microsoft.AspNetCore.Authorization;
using FileManagerApp.Service.Interfaces;
using FileManagerApp.Domain.Entities;

namespace FileManagerApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly string _storageBasePath;
        private readonly long _maxFileSize = 100 * 1024 * 1024; // 100MB
        private readonly string[] _allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "text/plain" };
        private readonly IFileService _fileService;

        public FilesController(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _storageBasePath = configuration["FileStorage:BasePath"];
            _fileService = fileService;
        }

        // GET: api/files
        // Retrieves all files or files in a specific folder
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetFiles([FromQuery] int? folderId)
        {
            try
            {
                var userId = GetCurrentUserId();
                IEnumerable<Domain.Entities.File> files;

                if (folderId.HasValue)
                {
                    files = await _fileService.GetFilesByFolderIdAsync(folderId.Value, userId);
                }
                else
                {
                    files = await _fileService.GetAllFilesAsync(userId);
                }

                var response = files.Select(f => new FileDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    ContentType = f.ContentType,
                    SizeInBytes = f.SizeInBytes,
                    CreatedAt = f.CreatedAt,
                    FolderId = f.FolderId,
                    Tags = f.Tags.ToList()
                }).ToList();

                Console.WriteLine($"Successfully retrieved {response.Count} files");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving files: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while retrieving files");
            }
        }

        // GET: api/files/{id}
        // Gets file metadata without downloading the data
        [HttpGet("{id}")]
        public async Task<ActionResult<FileDTO>> GetFile(int id)
        {
            var userId = GetCurrentUserId();
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            var response = new FileDTO
            {
                Id = file.Id,
                Name = file.Name,
                ContentType = file.ContentType,
                SizeInBytes = file.SizeInBytes,
                CreatedAt = file.CreatedAt,
                FolderId = file.FolderId,
                Tags = file.Tags.ToList()
            };

            return Ok(response);
        }

        // POST: api/files/upload
        // Handles file uploads with proper validation and error handling
        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponseDTO>> UploadFile([FromForm] CreateFileDTO createFileDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var file = createFileDto.File;
                Console.WriteLine($"Starting upload request - File: {file?.FileName}, FolderId: {createFileDto.FolderId}");

                if (file == null || file.Length == 0)
                    return BadRequest("No file was provided or file is empty");

                if (file.Length > _maxFileSize)
                    return BadRequest($"File size exceeds the limit of {_maxFileSize / 1024 / 1024}MB");

                if (!_allowedTypes.Contains(file.ContentType))
                    return BadRequest($"File type {file.ContentType} is not allowed");

                if (createFileDto.FolderId.HasValue)
                {
                    var folder = await _unitOfWork.Folders.GetByIdAsync(createFileDto.FolderId.Value, userId);
                    if (folder == null)
                        return BadRequest($"Folder with ID {createFileDto.FolderId.Value} not found");
                }

                var uploadedFile = await _fileService.UploadFileAsync(
                    file,
                    userId: userId,
                    createFileDto.FolderId,  
                    createFileDto.Tags       
                );

                var response = new FileUploadResponseDTO
                {
                    Id = uploadedFile.Id,
                    Name = uploadedFile.Name,
                    ContentType = uploadedFile.ContentType,
                    SizeInBytes = uploadedFile.SizeInBytes,
                    UploadedAt = uploadedFile.CreatedAt,
                    FolderId = uploadedFile.FolderId,
                    Tags = uploadedFile.Tags.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while uploading the file");
            }
        }

        // PUT: api/files/{id}/rename
        [HttpPut("{id}/rename")]
        public async Task<ActionResult<FileDTO>> RenameFile(int id, RenameFileDTO renameFileDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var file = await _fileService.RenameFileAsync(id, renameFileDto.NewName, userId);

                var response = new FileDTO
                {
                    Id = file.Id,
                    Name = file.Name,
                    ContentType = file.ContentType,
                    SizeInBytes = file.SizeInBytes,
                    CreatedAt = file.CreatedAt,
                    FolderId = file.FolderId,
                    Tags = file.Tags.ToList()
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while renaming the file");
            }
        }

        // PUT: api/files/{id}/tags
        // Updates file tags
        [HttpPut("{id}/tags")]
        public async Task<IActionResult> UpdateFileTags(int id, [FromBody] UpdateFileTagsDTO tagsDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var file = await _unitOfWork.Files.GetByIdAsync(id, userId);
                if (file == null)
                    return NotFound($"File with ID {id} not found");

                file.UpdateTags(tagsDto.Tags);
                _unitOfWork.Files.Update(file);
                await _unitOfWork.SaveChangesAsync();

                var response = new FileDTO
                {
                    Id = file.Id,
                    Name = file.Name,
                    ContentType = file.ContentType,
                    SizeInBytes = file.SizeInBytes,
                    CreatedAt = file.CreatedAt,
                    FolderId = file.FolderId,
                    Tags = file.Tags.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating file tags");
            }
        }

        // DELETE: api/files/{id}
        // Soft deletes a file
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var userId = GetCurrentUserId();
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            _unitOfWork.Files.Delete(file);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/files/batch-delete
        // Soft deletes multiple files
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFiles([FromBody] BatchDeleteFilesDTO deleteDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (deleteDto.FileIds == null || !deleteDto.FileIds.Any())
                {
                    return BadRequest("No file IDs provided for deletion");
                }

                await _unitOfWork.Files.BatchDeleteAsync(deleteDto.FileIds, userId);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during batch deletion: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting files");
            }
        }

        // GET: api/files/{id}/download
        // Downloads the file
        [HttpGet("{id}/download")]
        public async Task<ActionResult> DownloadFile(int id)
        {
            var userId = GetCurrentUserId();
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            if (!System.IO.File.Exists(file.StoragePath))
                return NotFound("Physical file is missing");

            var fileStream = System.IO.File.OpenRead(file.StoragePath);
            return File(fileStream, file.ContentType, file.Name);
        }

        // PUT: api/files/{id}/move
        // Moves the file to a different folder
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFile(int id, [FromBody] MoveFileDTO moveFileDto)
        {
            var userId = GetCurrentUserId();
            var file = await _unitOfWork.Files.GetByIdAsync(id, userId);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            if (moveFileDto.NewFolderId.HasValue)
            {
                var targetFolder = await _unitOfWork.Folders.GetByIdAsync(moveFileDto.NewFolderId.Value, userId);
                if (targetFolder == null)
                    return BadRequest("Target folder not found");
            }

            file.MoveToFolder(moveFileDto.NewFolderId);
            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/files/search
        // Searches files by name or tags
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> SearchFiles([FromQuery] string term)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest("Search term cannot be empty");

                var files = await _unitOfWork.Files.SearchFilesAsync(term, userId);

                var response = files.Select(f => new FileDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    ContentType = f.ContentType,
                    SizeInBytes = f.SizeInBytes,
                    CreatedAt = f.CreatedAt,
                    FolderId = f.FolderId,
                    Tags = f.Tags.ToList()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error details: {ex.Message}");
                return StatusCode(500, "An error occurred while searching files");
            }
        }

        [HttpPost("{id}/copy")]
        public async Task<ActionResult<FileDTO>> CopyFile(int id, [FromBody] CopyFileDTO copyFileDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var copiedFile = await _fileService.CopyFileAsync(id, copyFileDto.TargetFolderId, userId);

                var response = new FileDTO
                {
                    Id = copiedFile.Id,
                    Name = copiedFile.Name,
                    ContentType = copiedFile.ContentType,
                    SizeInBytes = copiedFile.SizeInBytes,
                    CreatedAt = copiedFile.CreatedAt,
                    FolderId = copiedFile.FolderId,
                    Tags = copiedFile.Tags.ToList()
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying file: {ex.Message}");
                return StatusCode(500, "An error occurred while copying the file");
            }
        }

        // PUT: api/files/{id}/favorite
        // Toggles the favorite status of a file
        [HttpPut("{id}/favorite")]
        public async Task<ActionResult<FileDTO>> ToggleFavorite(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var file = await _fileService.ToggleFavoriteAsync(id, userId);

                var response = new FileDTO
                {
                    Id = file.Id,
                    Name = file.Name,
                    ContentType = file.ContentType,
                    SizeInBytes = file.SizeInBytes,
                    CreatedAt = file.CreatedAt,
                    FolderId = file.FolderId,
                    Tags = file.Tags.ToList(),
                    IsFavorite = file.IsFavorite  
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

        // GET: api/files/favorites
        // Retrieves all favorite files
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetFavorites()
        {
            try
            {
                var userId = GetCurrentUserId();
                var favorites = await _fileService.GetFavoriteFilesAsync(userId);

                var response = favorites.Select(f => new FileDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    ContentType = f.ContentType,
                    SizeInBytes = f.SizeInBytes,
                    CreatedAt = f.CreatedAt,
                    FolderId = f.FolderId,
                    Tags = f.Tags.ToList(),
                    IsFavorite = f.IsFavorite
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving favorite files");
            }
        }
    }
}