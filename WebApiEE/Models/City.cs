using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApiEE.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [Index("IX_City_CountryIdName", IsUnique = true, Order = 1)]
        public int CountryId { get; set; }

        [Required]
        [Index("IX_City_CountryIdName", IsUnique = true, Order = 2)]
        [StringLength(100)]
        public string Name { get; set; }

        public int? Population { get; set; }

        public virtual Country Country { get; set; }
    }
}