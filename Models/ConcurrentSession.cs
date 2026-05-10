using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models.Entities
{
    [Table("ConcurrentSessions")]
    public class ConcurrentSession
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string SessionToken { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime LastActivity { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string DeviceInfo { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}