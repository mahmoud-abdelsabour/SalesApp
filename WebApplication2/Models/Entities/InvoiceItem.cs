using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models.Entities
{
    [Table("InvoiceItems")]
    public class InvoiceItem
    {
        [Key]
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal")]
        public decimal DiscountPct { get; set; } = 0;

        [Column(TypeName = "decimal")]
        public decimal DiscountAmt { get; set; } = 0;

        [Column(TypeName = "decimal")]
        public decimal LineTotal { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}