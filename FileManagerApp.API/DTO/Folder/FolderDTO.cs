namespace FileManagerApp.API.DTO.Folder
{
    public class FolderDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentFolderId { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public bool IsFavorite { get; set; }
    }
}
