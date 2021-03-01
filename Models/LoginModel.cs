using System.ComponentModel.DataAnnotations;

namespace Photostudio.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Не указан Email")]
        public string Email { get; set; }
         
        [Required(ErrorMessage = "Не указан пароль")]
        public string Password { get; set; }

        public LoginModel()
        {
            
        }
        
        public LoginModel(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }
}