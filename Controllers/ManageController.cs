using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Photostudio.DAO;
using Photostudio.Models;

namespace Photostudio.Controllers
{
    public class ManageController : Controller
    {
        private IMyDAO _dao;
        private TypeDatabases primaryDbType = TypeDatabases.MongoDB;
        private TypeDatabases secondDbType = TypeDatabases.MySql;
        private IMyDAO GetDao()
        {
            return DAOFactory.GetDAO(primaryDbType);
        }
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserAdministrationPanel()
        {
            _dao = GetDao();
            List<Booking> list;
            ViewBag.ShowMongoFuction = false;
            if (User.IsInRole("User"))
            {
                list = await _dao.GetAllBookingsByUserEmailAsync(User.FindFirst(x => x.Type == ClaimTypes.Email).Value);
            }
            else
            {
                list = await _dao.GetAllBookingsByStatusAsync();
                if (primaryDbType == TypeDatabases.MongoDB)
                {
                    ViewBag.ShowMongoFuction = true;
                }
            }
            return View(list);
        }
        
        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateBookingStatus(int idBooking)
        {
            _dao = GetDao();
            await _dao.UpdateBookingStatusAsync(idBooking);
            return RedirectToAction("UserAdministrationPanel", "Manage");
        }
        [Authorize]
        public async Task<IActionResult> DeleteBooking(int idBooking)
        {
            _dao = GetDao();
            await _dao.DeleteBookingAsync(idBooking);
            return RedirectToAction("UserAdministrationPanel", "Manage");
        }
        [Authorize]
        public async Task<IActionResult> CancelBooking(int idBooking)
        {
            _dao = GetDao();
            await _dao.CancelBookingAsync(idBooking);
            return RedirectToAction("UserAdministrationPanel", "Manage");
        }
        [Authorize]
        public async Task<string> MigrationData()
        {
            _dao = GetDao();
            var data = await _dao.ReadDataForMigrationToOtherDb();
            var _secondDao = DAOFactory.GetDAO(secondDbType);
            await _secondDao.WriteDataToDbAfterRead(data);
            return "Миграция была выполнена";
        }
        [Authorize]
        public IActionResult Replication()
        {
            if (primaryDbType == TypeDatabases.MySql)
            {
                return RedirectToAction("UserAdministrationPanel");
            }
            return View();
        }
        [Authorize]
        public async Task<IActionResult> ReplicationResult(int writeConcern)
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                _dao = GetDao();
                var list = await ((MongoDAO) _dao).WriteDataForReplication(writeConcern);
                return PartialView(list);
            }

            return PartialView();
        }
        [Authorize]
        public async Task<IActionResult> Aggregation()
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                return View();
            }

            return RedirectToAction("UserAdministrationPanel");
        }
        [Authorize]
        public async Task<IActionResult> AggregationResult(int aggregationAction)
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                switch (aggregationAction)
                {
                    case 1: return RedirectToAction("MatchUser");
                    case 2: return RedirectToAction("MatchHall");
                    case 3: return RedirectToAction("MatchAndProjectBooking");
                    case 4: return RedirectToAction("GroupBooking");
                    case 5: return RedirectToAction("MatchAndGroupBookingsHall");
                }
            }
            return PartialView();
        }
        [Authorize]
        public async Task<IActionResult> MatchUser()
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                _dao = GetDao();
                var list =await ((MongoDAO) _dao).FindUserAggregationAsync("g.dima621@gmail.com", "0000");
                return PartialView(list);
            }

            return PartialView();
        }
        [Authorize]
        public async Task<IActionResult> MatchHall()
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                _dao = GetDao();
                var list = await ((MongoDAO) _dao).GetHallsAggregationAsync(300);
                return PartialView(list);
            }

            return PartialView();
        }
        [Authorize]
        public async Task<IActionResult> MatchAndProjectBooking()
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                _dao = GetDao();
                var list = await ((MongoDAO) _dao).GetBookingAggregationAsync(new DateTime(2021, 01, 10));
                return PartialView(list);
            }

            return PartialView();
        }
        [Authorize]
        public async Task<IActionResult> GroupBooking()
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                _dao = GetDao();
                var list =await ((MongoDAO) _dao).GetCountBookingsByUserAggregationAsync();
                return PartialView(list);
            }

            return PartialView();
        }
        [Authorize]
        public async Task<IActionResult> MatchAndGroupBookingsHall()
        {
            if (primaryDbType == TypeDatabases.MongoDB)
            {
                _dao = GetDao();
                var list =await ((MongoDAO) _dao).GetBookingByHallAggregationAsync();
                return PartialView(list);
            }

            return PartialView();
        }
    }
}