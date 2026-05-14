using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "English name is required")]
        [MaxLength(200)]
        public string NameEn { get; set; }

        [Required(ErrorMessage = "Arabic name is required")]
        [MaxLength(200)]
        public string NameAr { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public int ProductCount { get; set; }
    }
}