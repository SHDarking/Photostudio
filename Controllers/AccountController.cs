using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Photostudio.Models;
using Photostudio.DAO;



namespace Photostudio.Controllers
{
    public class AccountController : Controller
    {
        private IMyDAO _dao;

        private IMyDAO GetDao()
        {
            return DAOFactory.GetDAO(TypeDatabases.MongoDB);
        }
        
        private async Task Authenticate(string userName, string email ,string roleName)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName ),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, roleName),
                new Claim(ClaimTypes.Email, email)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "UserDataCookies", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
        
        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(RegisterModel model)
        {
            _dao = GetDao();
            bool isNotExistUser = await _dao.FindUserByEmail(model.Email);
                if (isNotExistUser)
                {
                    User user = new User(model.Name, model.Surname, model.PhoneNumber, model.Password, model.Email);
                    // добавляем пользователя в бд
                    await _dao.CreateUserAsync(user);

                    await Authenticate(user.UserName,user.Email, user.Role); // аутентификация
 
                    return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            _dao = GetDao();
            User user = await _dao.FindUserByEmailAndPassword(model.Email, model.Password);
            if (user != null)
            {
                await Authenticate(user.UserName,user.Email, user.Role);
                return RedirectToAction("UserAdministrationPanel", "Manage");
            }
            return View(model);
        }
        
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        
    }
}