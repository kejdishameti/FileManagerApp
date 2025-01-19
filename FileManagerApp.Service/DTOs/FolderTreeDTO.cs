using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerApp.Service.DTOs
{
    public class FolderTreeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<FolderTreeDTO> Children { get; set; } = new List<FolderTreeDTO>();
    }
}
