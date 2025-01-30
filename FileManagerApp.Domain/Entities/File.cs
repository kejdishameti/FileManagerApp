using FileManagerApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using FileManagerApp.Domain.ValidationAttributes;

namespace FileManagerApp.Domain.Entities
{
    public class File
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [FileNameValidation]
        public string Name { get; set; }

        [Required]
        public string StoragePath { get; set; }

        [Required]
        public string ContentType { get; set; }
        public long SizeInBytes { get; set; }
        public FileStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? FolderId { get; set; }
        public virtual Folder Folder { get; set; }
        public int UserId { get; set; }
        public List<string> Tags = new();
        public bool IsFavorite { get; set; }
    }
}