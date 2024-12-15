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
        public string Path { get; private set; }

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
        public static Folder Create(string name, int ? parentFolderId = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Folder must have a name");

            return new Folder
            {
                Name = name,
                ParentFolderId = parentFolderId,
                CreatedAt = DateTime.UtcNow
            };
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
    }
}
