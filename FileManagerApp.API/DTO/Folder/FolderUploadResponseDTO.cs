namespace FileManagerApp.API.DTO.Folder
{
    public class FolderUploadResponseDTO
    {
        public int FolderId { get; set; }
        public string FolderName { get; set; }
        public string Path { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public IEnumerable<string> CreatedFolders { get; set; }
    }
}
