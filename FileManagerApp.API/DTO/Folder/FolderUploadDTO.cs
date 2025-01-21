namespace FileManagerApp.API.DTO.Folder
{
    public class FolderUploadDTO
    {
        public IFormFileCollection Files { get; set; }
        public int? ParentFolderId { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
