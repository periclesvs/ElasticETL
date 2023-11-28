using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourMixedFarm.Models.Final
{
    public class Person
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public int IdArea { get; set; }
        [Required]
        public string? Name { get; set; }
        public bool Active { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
