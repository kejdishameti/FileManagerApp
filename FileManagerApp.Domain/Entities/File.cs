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

        // Relationship with folders
        public int? FolderId { get; private set; }
        public virtual Folder Folder { get; private set; }

        // New property for tags
        private List<string> _tags = new();
        public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

        public void MoveToFolder(int? folderId)
        {
            FolderId = folderId;
            ModifiedAt = DateTime.UtcNow;
        }

        // New methods for tag management
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be empty");

            var normalizedTag = tag.Trim().ToLower();
            if (!_tags.Contains(normalizedTag))
            {
                _tags.Add(normalizedTag);
                ModifiedAt = DateTime.UtcNow;
            }
        }

        public void RemoveTag(string tag)
        {
            var normalizedTag = tag.Trim().ToLower();
            if (_tags.Remove(normalizedTag))
            {
                ModifiedAt = DateTime.UtcNow;
            }
        }

        public void ClearTags()
        {
            if (_tags.Any())
            {
                _tags.Clear();
                ModifiedAt = DateTime.UtcNow;
            }
        }

        public void UpdateTags(IEnumerable<string> tags)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            _tags = tags.Select(t => t.Trim().ToLower())
                       .Where(t => !string.IsNullOrWhiteSpace(t))
                       .Distinct()
                       .ToList();
            ModifiedAt = DateTime.UtcNow;
        }

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
                CreatedAt = DateTime.UtcNow,
                Status = FileStatus.Processing
            };
        }

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
    }
}