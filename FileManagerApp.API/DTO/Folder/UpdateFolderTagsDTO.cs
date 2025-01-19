namespace FileManagerApp.API.DTO.Folder
{
    public class UpdateFolderTagsDTO
    {
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
