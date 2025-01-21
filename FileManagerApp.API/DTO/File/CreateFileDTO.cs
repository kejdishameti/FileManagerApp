using System.ComponentModel.DataAnnotations;

namespace FileManagerApp.API.DTO.File
{
    public class CreateFileDTO
    {
        [Required]
        public IFormFile File { get; set; }
        public int? FolderId { get; set; }
        public IEnumerable<string>? Tags { get; set; }
    }
}
