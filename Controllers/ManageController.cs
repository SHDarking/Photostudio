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
        private IMyDAO GetDao()
        {
            return DAOFactory.GetDAO(TypeDatabases.MongoDB);
        }
        
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserAdministrationPanel()
        {
            _dao = GetDao();
            List<Booking> list;
            if (User.IsInRole("User"))
            {
                list = await _dao.GetAllBookingsByUserEmailAsync(User.FindFirst(x => x.Type == ClaimTypes.Email).Value);
            }
            else
            {
                list = await _dao.GetAllBookingsByStatusAsync();
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
      
    }
}