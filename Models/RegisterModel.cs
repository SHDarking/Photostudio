using System.ComponentModel.DataAnnotations;
using Photostudio.DAO;

namespace Photostudio.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage ="Не указан Email")]
        public string Email { get; set; }
         
        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
         [Required(ErrorMessage =  "Не указано имя")]
         [DataType(DataType.Text)]
        public string Name { get; set; }
        [Required(ErrorMessage = "Не указана фамалия")]
        [DataType(DataType.Text)]
        public string Surname { get; set; }
        [Required(ErrorMessage = "Не указан номер мобильного телефона")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }
        public string UserRole { get; } = "User";

        public RegisterModel()
        {
            
        }
        
        public RegisterModel(string name, string surname, string phoneNumber, string email, string password)
        {
            Name = name;
            Surname = surname;
            PhoneNumber = phoneNumber;
            Email = email;
            Password = password;
        }
        
    }
}