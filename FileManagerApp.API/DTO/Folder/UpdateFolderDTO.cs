namespace FileManagerApp.API.DTO.Folder
{
    public class UpdateFolderDTO
    {
        public string Name { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
