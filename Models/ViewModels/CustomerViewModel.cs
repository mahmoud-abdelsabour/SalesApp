using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModels
{
    public class CustomerViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(200)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [MaxLength(50)]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number must contain digits only")]
        public string Phone { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [MaxLength(500)]
        public string Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public int InvoiceCount { get; set; }
    }
}