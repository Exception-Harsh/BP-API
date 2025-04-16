using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BP_API.Models
{
    public class ApprovalWorkflow
    {
        public string StatusFlag { get; set; }
        public string WorkflowComment { get; set; }
        public string Username { get; set; }
    }
}