using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace WebApplication2.Models.ViewModels
{
    public class InvoiceCreateViewModel
    {
        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        public string CustomerName { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public decimal InvoiceDiscountPct { get; set; } = 0;

        [MaxLength(1000)]
        public string Notes { get; set; }

        public List<InvoiceItemViewModel> Items { get; set; } = new List<InvoiceItemViewModel>();
    }

    public class InvoiceItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        [Required]
        [Range(1, 9999, ErrorMessage = "Quantity must be between 1 and 9999")]
        public int Quantity { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public decimal DiscountPct { get; set; } = 0;

        public decimal DiscountAmt { get; set; }

        public decimal LineTotal { get; set; }
    }

    public class InvoiceListViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }

    public class InvoiceDetailsViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal InvoiceDiscountPct { get; set; }
        public decimal InvoiceDiscountAmt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public List<InvoiceItemViewModel> Items { get; set; }
    }
}