using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Photostudio.DAO;
using Photostudio.Models;

namespace Photostudio.Controllers
{
    public class Reserving : Controller
    {
        private IMyDAO _dao;
        private IMyDAO GetDao()
        {
            return DAOFactory.GetDAO(TypeDatabases.MongoDB);
        }
        [HttpGet]// GET}
        public async Task<IActionResult> CreateBooking()
        {
            _dao = GetDao();
            List<Hall> list = await _dao.GetAllHalls();
            SelectList hallList = new SelectList(list,"IdHall","Name");
            
            ViewBag.Hall = hallList;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateBooking(CreateBookingModel model)
        {
            _dao = GetDao();
            
            Booking booking = new Booking();
            booking.User = new User {Email = User.FindFirst(x => x.Type == ClaimTypes.Email).Value};
            booking.RentedHall = new Hall {IdHall = model.HallSelect};
            booking.StartHallReserving = model.GetStartHallReserving();
            booking.EndHallReserving = model.GetEndHallReserving();
            bool isBookingAdded =  await _dao.CreateBookingAsync(booking);
            if ( isBookingAdded)
            {
                return RedirectToAction("UserAdministrationPanel","Manage");
            }
            else
            {
                List<Hall> list = await _dao.GetAllHalls();
                SelectList hallList = new SelectList(list,"IdHall","Name",model.HallSelect);
                ViewBag.Hall = hallList;
                return View(model);
            }
        }
        
        public async Task<IActionResult> GetFreedomHallTime() // обработка ajax запросов
        {
            _dao = GetDao();
            
            int id = int.Parse(Request.Query["id"]);
            int action = int.Parse(Request.Query["action"]); // получаем 3 критерия поиска дат по залам
            DateTime date = DateTime.MinValue;
            if (action != 2)
            {
                date = DateTime.Parse(Request.Query["date"]);
            }


            DateTime dayStartFindWeek;
            DateTime dayEndFindWeek;  // переменные хранения дат задающих промежуток

            GetStartAndEndWeekDays(date, action, out dayStartFindWeek, out dayEndFindWeek); // определяем промежуток в недели для вывода на экран

            var listDates = await _dao.GetCalendarHall(id, dayStartFindWeek, dayEndFindWeek); // получаем даты для заполнения календаря

            Calendar calendar = new Calendar(id, dayStartFindWeek, dayEndFindWeek, listDates);
            return PartialView(calendar); // генерация нового календарика для клиента
        }
        private void GetStartAndEndWeekDays(DateTime date,int action,out DateTime startWeek, out DateTime endWeek)
        {
            if (action == 1)
            {
                startWeek = date.AddDays(-7);
                endWeek = date.AddDays(-1).AddHours(23);
            }
            else if (action == 2)
            {
                DateTime nowDate = DateTime.Now.Date;
                int dayOfWeek = (int)nowDate.DayOfWeek;

                if (dayOfWeek == 0)
                {
                    dayOfWeek = 7;
                }

                startWeek = nowDate.AddDays(-(dayOfWeek - 1));
                endWeek = startWeek.AddDays(6).AddHours(23);
            }
            else if (action == 3)
            {
                startWeek = date.AddDays(7);
                endWeek = startWeek.AddDays(6).AddHours(23);
            }
            else
            {
                startWeek = DateTime.MinValue;
                endWeek = DateTime.MinValue;
            }
        }
    }
}