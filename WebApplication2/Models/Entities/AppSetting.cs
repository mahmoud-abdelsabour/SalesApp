using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models.Entities
{
    /// <summary>A named application setting (e.g. the low-stock threshold) stored in the database.</summary>
    [Table("AppSettings")]
    public class AppSetting
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; }

        [MaxLength(500)]
        public string Value { get; set; }
    }
}
