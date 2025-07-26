using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email de récupération")]
        public string Email { get; set; }  
    }
}
