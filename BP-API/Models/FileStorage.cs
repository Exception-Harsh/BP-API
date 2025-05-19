using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BP_API.Models
{
    public class FileStorage
    {
        public int Number { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public byte[] FileData { get; set; }
    }
}