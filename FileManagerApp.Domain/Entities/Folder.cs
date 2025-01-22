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
        public string Name { get; private set; }

        [Required]
        [PathValidation]
        public string Path { get; internal set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Relationships
        public int? ParentFolderId { get; private set; }
        public virtual Folder ParentFolder { get; private set; }

        private List<Folder> _childFolders = new();
        public IReadOnlyCollection<Folder> ChildFolders => _childFolders;

        private List<File> _files = new();
        public IReadOnlyCollection<File> Files => _files;

        // Relationship with users
        public int UserId { get; private set; }

        // New property for tags
        private List<string> _tags = new();
        public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

        public bool IsFavorite { get; private set; }

        // Method to control favorite status
        public void ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
            ModifiedAt = DateTime.UtcNow;
        }

        public void SetPath(string parentPath = "")
        {
            Path = string.IsNullOrEmpty(parentPath)
                ? "/" + Name
                : parentPath + "/" + Name;
        }

        private Folder() { }

        public static Folder Create(string name, int? parentFolderId, int userId)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Folder must have a name");

            return new Folder
            {
                Name = name,
                ParentFolderId = parentFolderId,
                Path = "/" + name,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };
        }

        // Tag management methods
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

        public void AddFile(File file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            _files.Add(file);
            ModifiedAt = DateTime.UtcNow;
        }

        public void AddChildFolder(Folder folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            _childFolders.Add(folder);
            ModifiedAt = DateTime.UtcNow;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        public void UpdateParentFolder(int? newParentFolderId)
        {
            ParentFolderId = newParentFolderId;
            ModifiedAt = DateTime.UtcNow;
        }
    }
}