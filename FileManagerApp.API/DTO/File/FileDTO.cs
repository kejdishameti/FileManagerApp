using Microsoft.Extensions.Configuration.UserSecrets;

namespace FileManagerApp.API.DTO.File
{
    public class FileDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContentType { get; set; }
        public long SizeInBytes { get; set; }
        public string StoragePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? FolderId { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public bool IsFavorite { get; set; }
        public int UserId { get; set;}
    }
}
