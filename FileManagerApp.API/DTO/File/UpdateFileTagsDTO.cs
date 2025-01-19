namespace FileManagerApp.API.DTO.File
{
    public class UpdateFileTagsDTO
    {
        public ICollection<string> Tags { get; set; } = new List<string>();
    }
}
