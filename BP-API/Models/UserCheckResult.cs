using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BP_API.Models
{
    public class UserCheckResult
    {
        public bool IsValid { get; set; } // Indicates if the user is valid
        public string Code { get; set; }  // Holds the organization code (if applicable)
        public string Role { get; set; }  // Indicates the user's role
    }
}