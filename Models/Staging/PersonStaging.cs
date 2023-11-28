using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourMixedFarm.Models.Staging
{
    [Table("PERSON_STG")]
    public class PersonStaging
    {
        public int Id { get; set; }
        [FillMeForeignKey(new string[] {"CdArea"}, "Areas", "IdArea")]
        public int IdArea { get; set; }
        public string? CdArea { get; set; }
        public string? Name { get; set; }
        public bool Active { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
