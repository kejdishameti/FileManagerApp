namespace FileManagerApp.API.DTO.File
{
    public class CreateFileDTO
    {
        public IFormFile File { get; set; }
        public int? FolderId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
