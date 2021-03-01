using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Photostudio.DAO;
using Photostudio.Models;

namespace Photostudio.Controllers
{
    public class HomeController : Controller
    {
        private IMyDAO _dao;
        private IMyDAO GetDao()
        {
            return DAOFactory.GetDAO(TypeDatabases.MongoDB);
        }
        
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Catalog()
        {
            _dao = GetDao();
            List<Hall> halls = await _dao.GetAllHallsAsync();
            return View(halls);
        }

    }
}