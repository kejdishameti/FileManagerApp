namespace FileManagerApp.API.DTO.File
{
    public class FileUploadResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public int? FolderId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public long SizeInBytes { get; set; }
        
    }
}
