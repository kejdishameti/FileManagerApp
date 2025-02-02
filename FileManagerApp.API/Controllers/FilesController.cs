using FileManagerApp.Data.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using FileManagerApp.API.DTO.File;
using Microsoft.AspNetCore.Authorization;
using FileManagerApp.Service.Interfaces;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.DTOs.File;

namespace FileManagerApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : BaseApiController
    {
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService, IMapper mapper)
        {
            _fileService = fileService;
            _mapper = mapper;
        }

        // GET: api/files
        // Retrieves all files or files in a specific folder
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetFiles([FromQuery] int? folderId)
        {
            var userId = GetCurrentUserId();
            var files = await _fileService.GetFilesAsync(userId, folderId);
            var response = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(response);
        }

        // GET: api/files/{id}
        // Gets file metadata without downloading the data
        [HttpGet("{id}")]
        public async Task<ActionResult<FileDTO>> GetFile(int id)
        {
            var userId = GetCurrentUserId();
            var file = await _fileService.GetFileByIdAsync(id, userId);

            if (file == null)
                return NotFound($"File with ID {id} not found");

            var response = _mapper.Map<FileDTO>(file);
            return Ok(response);
        }

        // POST: api/files/upload
        // File upload
        [HttpPost("upload")]
        public async Task<ActionResult<FileUploadResponseDTO>> UploadFile([FromForm] CreateFileDTO createFileDto)
        {
            var userId = GetCurrentUserId();
            var result = await _fileService.UploadFileAsync(createFileDto, userId);
            var response = _mapper.Map<FileUploadResponseDTO>(result);
            return Ok(response);
        }

        // PUT: api/files/{id}/rename
        [HttpPut("{id}/rename")]
        public async Task<ActionResult<FileDTO>> RenameFile(int id, RenameFileDTO renameFileDto)
        {
            var userId = GetCurrentUserId();
            var file = await _fileService.RenameFileAsync(id, renameFileDto.NewName, userId);
            var response = _mapper.Map<FileDTO>(file);
            return Ok(response);
        }

        // PUT: api/files/{id}/tags
        // Updates file tags
        [HttpPut("{id}/tags")]
        public async Task<IActionResult> UpdateFileTags(int id, [FromBody] UpdateFileTagsDTO tagsDto)
        {
            var userId = GetCurrentUserId();
            var file = await _fileService.UpdateFileTagsAsync(id, tagsDto.Tags, userId);
            var response = _mapper.Map<FileDTO>(file);
            return Ok(response);
        }

        // DELETE: api/files/{id}
        // Soft deletes a file
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var userId = GetCurrentUserId();
            await _fileService.DeleteFileAsync(id, userId);
            return NoContent();
        }

        // POST: api/files/batch-delete
        // Soft deletes multiple files
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFiles([FromBody] BatchDeleteFilesDTO deleteDto)
        {
            var userId = GetCurrentUserId();
            await _fileService.BatchDeleteFilesAsync(deleteDto.FileIds, userId);
            return NoContent();
        }

        // GET: api/files/{id}/download
        // Downloads the file
        [HttpGet("{id}/download")]
        public async Task<ActionResult> DownloadFile(int id)
        {
            var userId = GetCurrentUserId();
            var fileResult = await _fileService.DownloadFileAsync(id, userId);
            return File(fileResult.Stream, fileResult.ContentType, fileResult.FileName);
        }

        // PUT: api/files/{id}/move
        // Moves the file to a different folder
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFile(int id, [FromBody] MoveFileDTO moveFileDto)
        {
            var userId = GetCurrentUserId();
            await _fileService.MoveFileAsync(id, moveFileDto.NewFolderId, userId);
            return NoContent();
        }

        // GET: api/files/search
        // Searches files by name or tags
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> SearchFiles([FromQuery] string term)
        {
            var userId = GetCurrentUserId();
            var files = await _fileService.SearchFilesAsync(term, userId);
            var response = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(response);
        }

        [HttpPost("{id}/copy")]
        public async Task<ActionResult<FileDTO>> CopyFile(int id, [FromBody] CopyFileDTO copyFileDto)
        {
            var userId = GetCurrentUserId();
            var file = await _fileService.CopyFileAsync(id, copyFileDto.TargetFolderId, userId);
            var response = _mapper.Map<FileDTO>(file);
            return Ok(response);
        }

        // PUT: api/files/{id}/favorite
        // Toggles the favorite status of a file
        [HttpPut("{id}/favorite")]
        public async Task<ActionResult<FileDTO>> ToggleFavorite(int id)
        {
            var userId = GetCurrentUserId();
            var file = await _fileService.ToggleFavoriteAsync(id, userId);
            var response = _mapper.Map<FileDTO>(file);
            return Ok(response);
        }

        // GET: api/files/favorites
        // Retrieves all favorite files
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<FileDTO>>> GetFavorites()
        {
            var userId = GetCurrentUserId();
            var files = await _fileService.GetFavoriteFilesAsync(userId);
            var response = _mapper.Map<IEnumerable<FileDTO>>(files);
            return Ok(response);
        }

        // GET: api/files/{id}/preview
        // Retrieves a preview of the file
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> GetPreview(int id)
        {
            var userId = GetCurrentUserId();
            var preview = await _fileService.GetPreviewAsync(id, userId);
            
            if (preview == null)
                return NotFound();

            return File(preview.Value.FileData, preview.Value.ContentType);
        }
    }
}