using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourMixedFarm.Models.Extraction
{
    public class PersonExtraction
    {
        public int Id { get; set; }
        public string? CdArea { get; set; }
        public string? Name { get; set; } 
        public bool Active { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
