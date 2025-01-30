using System.ComponentModel.DataAnnotations;
using FileManagerApp.Domain.ValidationAttributes;

namespace FileManagerApp.Domain.Entities
{
    public class Folder
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [FileNameValidation]
        public string Name { get; set; }

        [Required]
        [PathValidation]
        public string Path { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public int? ParentFolderId { get; set; }
        public virtual Folder ParentFolder { get; set; }

        public List<Folder> _childFolders = new();
        public IReadOnlyCollection<Folder> ChildFolders => _childFolders;

        public List<File> _files = new();
        public IReadOnlyCollection<File> Files => _files;

        public int UserId { get; set; }

        public ICollection<string> Tags { get; set; } = new List<string>();
        public bool IsFavorite { get; set; }
    }
}