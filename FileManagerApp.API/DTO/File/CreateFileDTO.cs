namespace FileManagerApp.API.DTO.File
{
    public class CreateFileDTO
    {
        public IFormFile File { get; set; }
        public int? FolderId { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
