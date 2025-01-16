using FileManagerApp.Data.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FileManagerApp.API.DTO.File;

namespace FileManagerApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly string _storageBasePath;
        private readonly long _maxFileSize = 100 * 1024 * 1024; // 10MB
        private readonly string[] _allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "text/plain" };

        public FilesController(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;

            // Get storage path from configuration 
            _storageBasePath = configuration["FileStorage:BasePath"];
        }

        // Get api/files
        // Retrives all files 
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetFiles([FromQuery] int? folderId)
        {
            try
            {
                IEnumerable<Domain.Entities.File> files;

                if (folderId.HasValue)
                {
                    Console.WriteLine($"Fetching files for folder: {folderId}");
                    files = await _unitOfWork.Files.GetFilesByFolderIdAsync(folderId.Value);
                }
                else
                {
                    Console.WriteLine("Fetching all files");
                    files = await _unitOfWork.Files.GetAllAsync();
                }

                var response = files.Select(f => new FileDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    ContentType = f.ContentType,
                    SizeInBytes = f.SizeInBytes,
                    CreatedAt = f.CreatedAt,
                    FolderId = f.FolderId,
                    Metadata = new Dictionary<string, string>(f.Metadata ?? new Dictionary<string, string>())
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

        // POST: api/files/upload
        // Handles file uploads with proper validation and error handling
        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponseDTO>> UploadFile([FromForm] CreateFileDTO createFileDto)
        {
            try
            {
                // Get the uploaded file and log initial request details
                var file = createFileDto.File;
                Console.WriteLine($"Starting upload request - File: {file?.FileName}, FolderId: {createFileDto.FolderId}");

                // Validate the incoming file
                if (file == null || file.Length == 0)
                    return BadRequest("No file was provided or file is empty");

                if (file.Length > _maxFileSize)
                    return BadRequest($"File size exceeds the limit of {_maxFileSize / 1024 / 1024}MB");

                if (!_allowedTypes.Contains(file.ContentType))
                    return BadRequest($"File type {file.ContentType} is not allowed");

                // If a folder ID is provided, verify the folder exists
                if (createFileDto.FolderId.HasValue)
                {
                    var folder = await _unitOfWork.Folders.GetByIdAsync(createFileDto.FolderId.Value);
                    Console.WriteLine($"Folder lookup result: {(folder != null ? "Found" : "Not Found")} for ID {createFileDto.FolderId.Value}");

                    if (folder == null)
                        return BadRequest($"Folder with ID {createFileDto.FolderId.Value} not found");
                }

                // Prepare the file storage location
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filePath = Path.Combine(_storageBasePath, uniqueFileName);

                // Ensure the storage directory exists
                if (!Directory.Exists(_storageBasePath))
                {
                    Console.WriteLine($"Creating storage directory: {_storageBasePath}");
                    Directory.CreateDirectory(_storageBasePath);
                }

                try
                {
                    // Save the physical file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    Console.WriteLine($"Physical file saved successfully to: {filePath}");

                    // Create the file entity with initial metadata
                    var fileEntity = Domain.Entities.File.Create(
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        filePath
                    );

                    Console.WriteLine($"File entity created - Initial state - ID: {fileEntity.Id}, FolderId: {fileEntity.FolderId}");

                    // Set the folder if specified
                    if (createFileDto.FolderId.HasValue)
                    {
                        fileEntity.MoveToFolder(createFileDto.FolderId);
                        Console.WriteLine($"After MoveToFolder - FolderId: {fileEntity.FolderId}");
                    }

                    // Add metadata if provided
                    if (createFileDto.Metadata != null && createFileDto.Metadata.Count > 0)
                    {
                        foreach (var item in createFileDto.Metadata)
                        {
                            fileEntity.AddMetadata(item.Key, item.Value);
                        }
                        Console.WriteLine($"Added {createFileDto.Metadata.Count} metadata items");
                    }

                    // Save to database
                    Console.WriteLine($"Before database save - FolderId: {fileEntity.FolderId}");
                    await _unitOfWork.Files.AddAsync(fileEntity);
                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"After database save - FolderId: {fileEntity.FolderId}");

                    // Prepare and return response
                    var response = new FileUploadResponseDTO
                    {
                        Id = fileEntity.Id,
                        Name = fileEntity.Name,
                        ContentType = fileEntity.ContentType,
                        SizeInBytes = fileEntity.SizeInBytes,
                        UploadedAt = fileEntity.CreatedAt,
                        FolderId = fileEntity.FolderId,
                        Metadata = new Dictionary<string, string>(fileEntity.Metadata)
                    };

                    Console.WriteLine($"Upload completed successfully - File ID: {response.Id}, FolderId: {response.FolderId}");
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    // If anything fails after the physical file is created, clean it up
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        Console.WriteLine($"Cleaned up physical file after error: {filePath}");
                    }

                    Console.WriteLine($"Error during file processing: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, "An error occurred while uploading the file");
            }
        }

        // Get api/files/{id}
        // Get files metadata without downloading the data
        [HttpGet("{id}")]
        public async Task<ActionResult<FileDTO>> GetFile(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            return Ok(_mapper.Map<FileDTO>(file));
        }

        // Put api/files/{id}
        // Updates the file metadata
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFile(int id, UpdateFileDTO updateFileDto)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            _mapper.Map(updateFileDto, file);

            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }


        // Delete api/files/{id}
        // Soft delete 
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            _unitOfWork.Files.Delete(file);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // Post api/files/batch-delete
        // Soft delete multiple files
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFiles([FromBody] BatchDeleteFilesDTO deleteDto)
        {
            try
            {
                if (deleteDto.FileIds == null || !deleteDto.FileIds.Any())
                {
                    return BadRequest("No file IDs provided for deletion");
                }

                Console.WriteLine($"Starting batch deletion of {deleteDto.FileIds.Count} files");

                await _unitOfWork.Files.BatchDeleteAsync(deleteDto.FileIds);

                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine("Batch deletion completed successfully");

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during batch deletion: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, "An error occurred while deleting files");
            }
        }

        // Get api/files/{id}/download
        // Downloads the file with the specified ID
        [HttpGet("{id}/download")]
        public async Task<ActionResult> DownloadFile(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            if (!System.IO.File.Exists(file.StoragePath))
                return NotFound("Physical file is missing");

            // Stream the file 
            var fileStream = System.IO.File.OpenRead(file.StoragePath);
            return File(fileStream, file.ContentType, file.Name);
        }


        // Put api/files/{id}/move
        // Moves the file to a different folder
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFile(int id, [FromBody] MoveFileDTO moveFileDto)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            if (moveFileDto.NewFolderId.HasValue)
            {
                var targetFolder = await _unitOfWork.Folders.GetByIdAsync(moveFileDto.NewFolderId.Value);
                if (targetFolder == null)
                    return BadRequest("Target folder not found");
            }

            file.MoveToFolder(moveFileDto.NewFolderId);
            _unitOfWork.Files.Update(file);
            await _unitOfWork.SaveChangesAsync();
            
            return NoContent();
        }

        // Get api/files/search
        // Searches files by name or metadata
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> SearchFiles([FromQuery] string term)
        {
            try
            {
                // Log the start of the search operation
                Console.WriteLine($"Starting search operation with term: {term}");

                if (string.IsNullOrWhiteSpace(term))
                    return BadRequest("Search term cannot be empty");

                // Log before calling the repository
                Console.WriteLine("Calling repository SearchFilesAsync method");
                var files = await _unitOfWork.Files.SearchFilesAsync(term);
                Console.WriteLine($"Repository returned {files?.Count() ?? 0} files");

                // Log the mapping operation
                Console.WriteLine("Starting to map files to DTOs");
                var response = files.Select(f => new FileDTO
                {
                    Id = f.Id,
                    Name = f.Name,
                    ContentType = f.ContentType,
                    SizeInBytes = f.SizeInBytes,
                    CreatedAt = f.CreatedAt,
                    FolderId = f.FolderId,
                    Metadata = new Dictionary<string, string>(f.Metadata ?? new Dictionary<string, string>())
                }).ToList();
                Console.WriteLine($"Successfully mapped {response.Count} files to DTOs");

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Enhanced error logging
                Console.WriteLine($"Error details:");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                return StatusCode(500, "An error occurred while searching files");
            }
        }
    }
}
