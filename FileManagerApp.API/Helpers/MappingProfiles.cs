using AutoMapper;
using FileManagerApp.API.DTO.File;
using FileManagerApp.API.DTO.Folder;
using FileManagerApp.Domain.Entities;
using DomainFile = FileManagerApp.Domain.Entities.File;

namespace FileManagerApp.API.Helpers
{
    public class MappingProfiles : Profile
    {
         public MappingProfiles()
            {
                // File mappings
                CreateMap<DomainFile, FileDTO>();
                CreateMap<CreateFileDTO, DomainFile>();
                CreateMap<RenameFileDTO, DomainFile>();

                // Folder mappings
                CreateMap<Folder, FolderDTO>();
                CreateMap<CreateFolderDTO, Folder>();
                CreateMap<RenameFolderDTO, Folder>();
            }
    }
}
