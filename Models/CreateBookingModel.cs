using System;
using System.Collections.Generic;

namespace Photostudio.Models
{
    public class CreateBookingModel
    {
        public string HallSelect { get; set; }
        public DateTime StartDateReserving {get; set; }
        public int StartTimeReserving { get; set; }
        public DateTime EndDateReserving { get; set; }
        public int EndTimeReserving { get; set; }

        public DateTime GetStartHallReserving()
        {
            return StartDateReserving.AddHours(StartTimeReserving);
        }

        public DateTime GetEndHallReserving()
        {
            return EndDateReserving.AddHours(EndTimeReserving);
        }
    }
}