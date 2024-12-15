using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FileManagerApp.Domain.Entities;
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

                // Store metadata as JSON
                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb");
            });

            // Configure the Folder entity
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
            });
        }
    }
}
