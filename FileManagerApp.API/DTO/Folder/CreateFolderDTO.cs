﻿namespace FileManagerApp.API.DTO.Folder
{
    public class CreateFolderDTO
    {
        public string Name { get; set; }
        public int? ParentFolderId { get; set; } = null;
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }
}
