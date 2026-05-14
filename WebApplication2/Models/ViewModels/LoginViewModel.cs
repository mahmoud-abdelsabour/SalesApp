using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(200)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}