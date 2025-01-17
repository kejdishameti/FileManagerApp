namespace FileManagerApp.API.DTO.Folder
{
    public class FolderDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentFolderId { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
