using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models.Entities
{
    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; }

        public int CustomerId { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal")]
        public decimal InvoiceDiscountPct { get; set; } = 0;

        [Column(TypeName = "decimal")]
        public decimal InvoiceDiscountAmt { get; set; } = 0;

        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User CreatedByUser { get; set; }

        public virtual ICollection<InvoiceItem> Items { get; set; }
    }
}