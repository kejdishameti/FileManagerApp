using FileManagerApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Domain.ValidationAttributes;

namespace FileManagerApp.Domain.Entities
{
    public class File
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [FileNameValidation]
        public string Name { get; private set; }

        [Required]
        public string StoragePath { get; private set; }

        [Required]
        public string ContentType { get; private set; }
        public long SizeInBytes { get; private set; }
        public FileStatus Status { get; private set; }


        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }


        // Relationship with folders - a file belongs in one folder
        public int? FolderId { get; private set; }
        public virtual Folder Folder { get; private set; }

        // Method to move the file to a different folder
        public void MoveToFolder(int? FolderId)
        {
            FolderId = FolderId;
            ModifiedAt = DateTime.UtcNow;
        }

        // Additional file information stored as key-value pairs
        private Dictionary<string, string> _metadata = new();
        public IReadOnlyDictionary<string, string> Metadata => _metadata;


        // Private constructor for Entity Framework
        private File() { }

        public static File Create(string name, string contentType, long sizeInBytes, string storagePath)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("File must have a name");

            return new File
            {
                Name = name,
                ContentType = contentType,
                SizeInBytes = sizeInBytes,
                StoragePath = storagePath,
                CreatedAt = DateTime.UtcNow
                //Status = FileStatus.Processing  // New files start in Processing status
            };
        }

        // Methods to change the file's status
        public void MarkAsActive()
        {
            Status = FileStatus.Active;
            ModifiedAt = DateTime.UtcNow;
        }

        public void MarkAsArchived()
        {
            Status = FileStatus.Archived;
            ModifiedAt = DateTime.UtcNow;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        // Method to add extra information about the file
        public void AddMetadata(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Metadata key cannot be empty");

            _metadata[key] = value;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}
