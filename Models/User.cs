
namespace Photostudio.Models
{
    public class User
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        public User()
        {
            
        }
        public User(string id, string userName, string surname, string phoneNumber, string email, string password, string role)
        {
            Id = id;
            UserName = userName;
            Surname = surname;
            Email = email;
            PhoneNumber = phoneNumber;
            Password = password;
            Role = role;
        }

        public User(string userName, string surname, string phoneNumber,string password, string email)
        {
            UserName = userName;
            Surname = surname;
            PhoneNumber = phoneNumber;
            Password = password;
            Email = email;
            Role = "User";
        }

        
        
    }
}