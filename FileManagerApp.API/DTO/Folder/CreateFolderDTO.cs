using System.ComponentModel.DataAnnotations;

namespace FileManagerApp.API.DTO.Folder
{
    public class CreateFolderDTO
    {
        [Required]
        public string Name { get; set; }
        public int? ParentFolderId { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
