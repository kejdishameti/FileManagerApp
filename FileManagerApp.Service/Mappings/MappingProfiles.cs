using AutoMapper;
using FileManagerApp.Domain.Entities;
using FileManagerApp.Service.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerApp.Service.Mappings
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles() 
        {
            CreateMap<User, UserDTO>();
        }
    }
}
