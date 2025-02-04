using System.ComponentModel.DataAnnotations;
namespace EmployeePortalWeb.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [DataType(DataType.Upload)]
        public string PDFDocumentPath { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string Email { get; set; }
    }
}
