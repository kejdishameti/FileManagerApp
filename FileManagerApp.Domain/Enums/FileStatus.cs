using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerApp.Domain.Enums
{
    public enum FileStatus
    {
        // A normal, accessible file
        Active = 0,

        // When a file is being uploaded or modified
        Processing = 1,

        // For files that have been stored for long-term retention
        Archived = 2,

        // When something goes wrong with processing
        Failed = 3
    }
}
