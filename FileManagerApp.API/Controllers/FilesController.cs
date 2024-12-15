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
            IEnumerable<Domain.Entities.File> files;

            if (folderId.HasValue)
                files = await _unitOfWork.Files.GetFilesByFolderIdAsync(folderId.Value);
            else
                files = await _unitOfWork.Files.GetAllAsync();

            return Ok(_mapper.Map<IEnumerable<FileDTO>>(files));
        }

        // POST: api/files/upload
        // Handles file uploads with proper validation and error handling
        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponseDTO>> UploadFile([FromForm] CreateFileDTO createFileDto)
        {
            try
            {
                var file = createFileDto.File;

                // Validate the file
                if (file == null || file.Length == 0)
                    return BadRequest("No file was provided");

                if (file.Length > _maxFileSize)
                    return BadRequest($"File size exceeds the limit of {_maxFileSize / 1024 / 1024}MB");

                if (!_allowedTypes.Contains(file.ContentType))
                    return BadRequest("File type not allowed");

                // Generate a unique filename to prevent conflicts
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filePath = Path.Combine(_storageBasePath, uniqueFileName);

                // Ensure storage directory exists
                Directory.CreateDirectory(_storageBasePath);

                // Save the physical file using a stream to handle large files efficiently
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create and save file metadata
                var fileEntity = Domain.Entities.File.Create(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    filePath
                );

                if (createFileDto.FolderId.HasValue)
                {
                    fileEntity.MoveToFolder(createFileDto.FolderId);
                }

                await _unitOfWork.Files.AddAsync(fileEntity);
                await _unitOfWork.SaveChangesAsync();

                return Ok(_mapper.Map<FileUploadResponseDTO>(fileEntity));
            }
            catch (Exception ex)
            {
                // Log the error here
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

            // Stream the file instead of loading it all into memory
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
    }
}
