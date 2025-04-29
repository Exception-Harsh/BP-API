using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BP_API.Models
{
    public class FileUpload
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public byte[] FileContent { get; set; }
    }
}