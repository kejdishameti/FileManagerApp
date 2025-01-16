namespace FileManagerApp.API.DTO.File
{
    public class BatchDeleteFilesDTO
    {
        public ICollection<int> FileIds { get; set; }
    }
}
