using Microsoft.EntityFrameworkCore;
using FileManagerApp.Domain.Entities;
using System.Text.Json;
using DomainFile = FileManagerApp.Domain.Entities.File;

namespace FileManagerApp.Data
{
    public class FileManagerDbContext : DbContext
    {
        public FileManagerDbContext(DbContextOptions<FileManagerDbContext> options)
           : base(options)
        {
        }

        public DbSet<DomainFile> Files { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the File entity
            modelBuilder.Entity<DomainFile>(entity =>
            {
                entity.ToTable("Files");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Folder)
                    .WithMany(e => e.Files)
                    .HasForeignKey(e => e.FolderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure tags as a JSON array
                entity.Property(e => e.Tags)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>()
                    );

                // Configure the IsFavorite property
                entity.Property(e => e.IsFavorite)
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Folder>(entity =>
            {
                entity.ToTable("Folders");
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.ParentFolder)
                    .WithMany(e => e.ChildFolders)
                    .HasForeignKey(e => e.ParentFolderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Path)
                    .IsUnique();

                // Configure tags as a JSON array
                entity.Property(e => e.Tags)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>()
                    );

                // Configure the IsFavorite property
                entity.Property(e => e.IsFavorite)
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
            });

            // Configure the User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50);
            });
        }
    }
}