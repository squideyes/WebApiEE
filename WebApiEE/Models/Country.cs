using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiEE.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required]
        [Index(IsUnique = true)]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Index(IsUnique = true)]
        [StringLength(8)]
        public string Code { get; set; }

        [StringLength(100)]
        public string Capital { get; set; }

        [StringLength(100)]
        public string Province { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Area { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Population { get; set; }

        public virtual List<City> Cities { get; set; }
    }
}