namespace FileManagerApp.API.DTO.File
{
    public class FileUploadResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public int? FolderId { get; set; }
        public long SizeInBytes { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public int UserId { get; set; }

    }
}
