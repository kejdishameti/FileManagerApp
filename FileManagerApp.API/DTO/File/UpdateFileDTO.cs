namespace FileManagerApp.API.DTO.File
{
    public class UpdateFileDTO
    {
        public string Name { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
