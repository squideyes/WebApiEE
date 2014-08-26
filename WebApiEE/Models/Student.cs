using System.ComponentModel.DataAnnotations;

namespace WebApiEE.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [Range(1, 99)]
        public int Age { get; set; }

        [Required]
        public string Gender { get; set; }
    }
}