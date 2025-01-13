﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FileManagerApp.Domain.Entities;
using DomainFile = FileManagerApp.Domain.Entities.File;
using System.Text.Json.Serialization;
using System.Text.Json;

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

                // Store metadata as JSON
                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'{}'::jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions()) ?? new Dictionary<string, string>()
                    );
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

            // Configure the User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);


                entity.HasIndex(e => e.Email)
                    .IsUnique();

                // Configure property requirements
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
