using System;
using System.Collections.Generic;
using System.Text;

namespace FileOrganizer.Models
{
    internal class OrganizationRule
    {
        public string? Extension { get; set; }
        public string? DestinationFolder { get; set; }
    }
}
