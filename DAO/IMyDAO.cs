using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Photostudio.Models;

namespace Photostudio.DAO
{
    public interface IMyDAO
    {
        public Task CreateUserAsync(User user);
        public Task<User> FindUserByEmailAndPassword(string email, string password);
        public  Task<bool> FindUserByEmail(string email);
        public Task<List<Hall>> GetAllHallsAsync();
        public Task<bool> CreateBookingAsync(Booking booking);
        public Task UpdateBookingStatusAsync(int idBooking);
        public Task DeleteBookingAsync(int idBooking);
        public Task CancelBookingAsync(int idBooking);
        public Task<List<Booking>> GetAllBookingsByUserEmailAsync(string email);
        public Task<List<Booking>> GetAllBookingsByStatusAsync();

        public Task<List<DateTime[]>> GetCalendarHall(int id,DateTime dayStartFindWeek,DateTime dayEndFindWeek);
        public Task<List<Hall>> GetAllHalls();

        public Task<Dictionary<string,List<Dictionary<string,string>>>> ReadDataForMigrationToOtherDb();
        public Task WriteDataToDbAfterRead(Dictionary<string,List<Dictionary<string,string>>> tables);

    }
}