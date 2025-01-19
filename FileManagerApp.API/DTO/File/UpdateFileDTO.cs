namespace FileManagerApp.API.DTO.File
{
    public class UpdateFileDTO
    {
        public string Name { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
