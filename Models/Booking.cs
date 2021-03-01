using System;

namespace Photostudio.Models
{
    public class Booking
    {
        public string IdBooking { get; set;}
        public User User { get; set;}
        public DateTime CreatingDateTime { get; set;} = DateTime.Now;
        public Hall RentedHall { get; set;}
        public DateTime StartHallReserving { get; set; }
        public DateTime EndHallReserving { get; set; }
        public string Status { get; set;}
        
        public int TotalCost { get; set; }
        public int Count { get; set; }

        public Booking()
        {
            
        }
        
        public Booking(string idBooking, User user, Hall rentedHall)
        {
            IdBooking = idBooking;
            User = user;
            RentedHall = rentedHall;
        }

        public Booking(string idBooking, User user, DateTime creatingDateTime, Hall rentedHall, string status)
        {
            IdBooking = idBooking;
            User = user;
            CreatingDateTime = creatingDateTime;
            RentedHall = rentedHall;
            Status = status;
        }
    }
}