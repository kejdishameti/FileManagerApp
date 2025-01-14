namespace FileManagerApp.API.DTO.Folder
{
    public class FolderTreeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FolderTreeDTO> Children { get; set; } = new List<FolderTreeDTO>();
    }
}
