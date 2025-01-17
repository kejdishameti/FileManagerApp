using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void SetPath(string parentPath = "")
        {
            Path = string.IsNullOrEmpty(parentPath)
                ? "/" + Name
                : parentPath + "/" + Name;
        }
        // Timestamps
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }


        // Relationship with parent folder
        public int? ParentFolderId { get; private set; }
        public virtual Folder ParentFolder { get; private set; }

        // Relationship with child folders
        private List<Folder> _childFolders = new();
        public IReadOnlyCollection<Folder> ChildFolders => _childFolders;

        // Relationship with files
        private List<File> _files = new();
        public IReadOnlyCollection<File> Files => _files;

        // Private constructor for Entity Framework
        private Folder() { }

        // Method to create a new folder
        public static Folder Create(string name, int? parentFolderId = null) 
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Folder must have a name");

            return new Folder
            {
                Name = name,
                ParentFolderId = parentFolderId,
                Path = "/" + name,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Metadata
        private Dictionary<string, string> _metadata = new();
        public Dictionary<string, string> Metadata
        {
            get => _metadata;
            private set => _metadata = value ?? new Dictionary<string, string>();
        }

        // Method to add a file to this folder
        public void AddFile(File file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            _files.Add(file);
            ModifiedAt = DateTime.UtcNow;
        }

        // Method to create a subfolder within this folder
        public void AddChildFolder(Folder folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            _childFolders.Add(folder);
            ModifiedAt = DateTime.UtcNow;
        }

        // Method to mark folder as deleted
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

        // Add methods to manage metadata
        public void AddOrUpdateMetadata(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Metadata key cannot be empty");

            var normalizedKey = key.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(value))
            {
                RemoveMetadata(normalizedKey);
                return;
            }

            _metadata[normalizedKey] = value.Trim();
            ModifiedAt = DateTime.UtcNow;
        }

        public string GetMetadataValue(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be empty");

            var normalizedKey = key.Trim().ToLower();
            return _metadata.TryGetValue(normalizedKey, out var value) ? value : null;
        }

        public void RemoveMetadata(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be empty");

            var normalizedKey = key.Trim().ToLower();
            if (_metadata.Remove(normalizedKey))
            {
                ModifiedAt = DateTime.UtcNow;
            }
        }
    }
}
