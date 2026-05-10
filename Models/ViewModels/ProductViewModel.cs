using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace WebApplication2.Models.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "English name is required")]
        [MaxLength(200)]
        public string NameEn { get; set; }

        [Required(ErrorMessage = "Arabic name is required")]
        [MaxLength(200)]
        public string NameAr { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 9999999, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, 999999, ErrorMessage = "Stock quantity must be 0 or more")]
        public int StockQuantity { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; }
    }
}