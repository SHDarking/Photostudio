using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Photostudio.Models;


namespace Photostudio.DAO
{
    public class MongoDAO : DAOFactory,IMyDAO
    {
        private string ConnectionLink { get; }
        
        public MongoDAO(IConfiguration configuration)
        {
            ConnectionLink = configuration["ConnectionStrings:MongoCloudConnection"];
        }

        private MongoClient GetConnection()
        {
            return new(ConnectionLink);
        }
        
        public async Task CreateUserAsync(User user)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("user");
            var filter = new BsonDocument();
            var oldUserId = collection.Find(filter).Sort("{_id : -1}").Limit(1).First();
            int newUserId = oldUserId["_id"].AsInt32 + 1;
            await collection.InsertOneAsync(new BsonDocument
            {
                {"_id", newUserId},
                {"name", user.UserName},
                {"surname", user.Surname},
                {"phone_number", user.PhoneNumber},
                {"email", user.Email},
                {"password", user.Password},
                {"role", "User"}
            });
        }
        public async Task<User> FindUserByEmailAndPassword(string email, string password)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("user");
            var filter = new BsonDocument("$and", new BsonArray
            {
                new BsonDocument("email",email),
                new BsonDocument("password",password)
            });
            var user = await collection.Find(filter).FirstOrDefaultAsync();
            if (user is null) return null;
            var customer = new User();
            customer.UserName = user["name"].AsString;
            customer.Surname = user["surname"].AsString;
            customer.PhoneNumber = user["phone_number"].AsString;
            customer.Email = user["email"].AsString;
            customer.Role = user["role"].AsString;
            return customer;
        }
        public async Task<bool> FindUserByEmail(string email)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("user");
            var filter = new BsonDocument("email", email);
            var user = await collection.Find(filter).ToListAsync();
            if (user.Count > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public async Task<List<Hall>> GetAllHallsAsync()
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("hall");
            var collectionList =  collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            List<Hall> hallList = new List<Hall>();
            foreach (var item in await collectionList)
            {
                Hall hall = new Hall();
                hall.IdHall = item["_id"].AsInt32.ToString();
                hall.Name = item["hall_name"].AsString;
                hall.Description = item["description"].AsString;
                hall.Price = item["price"].AsInt32;
                hallList.Add(hall);
            }

            return hallList;
        }
        public async Task<bool> CreateBookingAsync(Booking booking)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("hall");
            var filter = Builders<BsonDocument>.Filter.Eq("_id",int.Parse(booking.RentedHall.IdHall));
            booking.RentedHall.Name = collection.Find(filter).First()["hall_name"].AsString;
            collection = database.GetCollection<BsonDocument>("booking");
            var builder = Builders<BsonDocument>.Filter;
            filter = builder.Lt("start_hall_reserving", new BsonDateTime(booking.EndHallReserving)) &
                         builder.Gt("end_hall_reserving", new BsonDateTime(booking.StartHallReserving)) & builder.Eq("hall", booking.RentedHall.Name);
            var checkAvailable = collection.Find(filter).FirstOrDefault();
            if (checkAvailable != null) return false;
            collection = database.GetCollection<BsonDocument>("user");
            filter = Builders<BsonDocument>.Filter.Eq("email", booking.User.Email);
            int userId = collection.Find(filter).First()["_id"].AsInt32;
            collection = database.GetCollection<BsonDocument>("booking");
            int newBookingId = collection.Find(FilterDefinition<BsonDocument>.Empty).Sort("{_id : -1}").Limit(1).First()["_id"].AsInt32 + 1;
            await collection.InsertOneAsync(new BsonDocument
            {
                {"_id", newBookingId},
                {"user", userId},
                {"creating_date", new BsonDateTime(DateTime.Now)},
                {"hall", booking.RentedHall.Name},
                {"start_hall_reserving", new BsonDateTime(booking.StartHallReserving)},
                {"end_hall_reserving", new BsonDateTime(booking.EndHallReserving)},
                {"status", "Новый"}
            });
            return true;
        }
        public async Task UpdateBookingStatusAsync(int idBooking)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", idBooking);
            var booking =  collection.Find(filter).First();
            string newStatus = String.Empty;
            switch (booking["status"].AsString)
            {
                case "Новый" : newStatus = "Ожидает оплаты";
                    break;
                case "Ожидает оплаты" : newStatus = "Подтвержден";
                    break;
                case "Подтвержден" : newStatus = "Выполнен";
                    break;
            }
            var updateOption = Builders<BsonDocument>.Update.Set("status", newStatus);
            await collection.UpdateOneAsync(filter, updateOption);
        }
        public async Task DeleteBookingAsync(int idBooking)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", idBooking);
            await collection.DeleteOneAsync(filter);
        }
        public async Task CancelBookingAsync(int idBooking)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", idBooking);
            var updateOption = Builders<BsonDocument>.Update.Set("status", "Отменен");
            await collection.FindOneAndUpdateAsync(filter, updateOption);
        }
        public async Task<List<Booking>> GetAllBookingsByUserEmailAsync(string email)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("user");
            var filter = new BsonDocument("email", email);
            int idUser = collection.Find(filter).First()["_id"].AsInt32;
            collection = database.GetCollection<BsonDocument>("booking");
            filter = new BsonDocument("user",idUser);
            var documentList = await collection.Find(filter).ToListAsync();
            var bookingList = new List<Booking>();
            for (int i = 0; i < documentList.Count; i++)
            {
                Booking booking = new Booking();
                booking.IdBooking = documentList[i]["_id"].AsInt32.ToString();
                booking.CreatingDateTime = documentList[i]["creating_date"].ToLocalTime();
                booking.RentedHall = new Hall();
                booking.RentedHall.Name = documentList[i]["hall"].AsString;
                booking.StartHallReserving = documentList[i]["start_hall_reserving"].ToLocalTime();
                booking.EndHallReserving = documentList[i]["end_hall_reserving"].ToLocalTime();
                collection = database.GetCollection<BsonDocument>("hall");
                filter = new BsonDocument("hall_name", $"{booking.RentedHall.Name}");
                int price = collection.Find(filter).First()["price"].AsInt32;
                booking.TotalCost = price * (booking.EndHallReserving - booking.StartHallReserving).Hours;
                booking.Status = documentList[i]["status"].AsString;
                bookingList.Add(booking);
            }
            return bookingList;
        }
        public async Task<List<Booking>> GetAllBookingsByStatusAsync()
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var filter = new BsonDocument("status", new BsonDocument("$in",new BsonArray(new []{"Новый", "Ожидает оплаты", "Подтвержден"})));
            var bookings = await collection.Find(filter).Sort("{status : 1, creating_date : -1}").ToListAsync();
            var bookingList = new List<Booking>();

            foreach (var item in bookings)
            {
                var booking = new Booking();
                booking.IdBooking = item["_id"].AsInt32.ToString();
                
                collection = database.GetCollection<BsonDocument>("user");
                filter = new BsonDocument("_id", item["user"].AsInt32);
                var userDocument = collection.Find(filter).First();
                
                booking.User = new User
                {
                    UserName = userDocument["name"].AsString,
                    Surname = userDocument["surname"].AsString,
                    PhoneNumber = userDocument["phone_number"].AsString
                };
                booking.CreatingDateTime = item["creating_date"].ToLocalTime();
                booking.RentedHall = new Hall { Name = item["hall"].AsString};
                booking.StartHallReserving = item["start_hall_reserving"].ToLocalTime();
                booking.EndHallReserving = item["end_hall_reserving"].ToLocalTime();
                
                collection = database.GetCollection<BsonDocument>("hall");
                filter = new BsonDocument("hall_name", $"{booking.RentedHall.Name}");
                int price = collection.Find(filter).First()["price"].AsInt32;
                
                booking.TotalCost = price * (booking.EndHallReserving - booking.StartHallReserving).Hours;
                booking.Status = item["status"].AsString;
                bookingList.Add(booking);
            }

            return bookingList;
        }
        
        // методы для отрисовки страницы создания заказа
        
        public async Task<List<DateTime[]>> GetCalendarHall(int id, DateTime dayStartFindWeek, DateTime dayEndFindWeek)
        {
            List<DateTime[]> listDates= new List<DateTime[]>();

            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("hall");
            var filter = Builders<BsonDocument>.Filter.Eq("_id",new BsonInt32(id));
            var hallName = collection.Find(filter).First()["hall_name"].AsString;
            
            collection = database.GetCollection<BsonDocument>("booking");
            var builder = Builders<BsonDocument>.Filter; 
            filter = builder.Eq("hall",new BsonString(hallName)) &
                     builder.In("status", new BsonArray(new []{"Новый", "Ожидает оплаты", "Подтвержден"})) &
                     builder.Gte("start_hall_reserving", new BsonDateTime(dayStartFindWeek)) &
                     builder.Lte("start_hall_reserving", new BsonDateTime(dayEndFindWeek));
            var documentList = await collection.Find(filter)
                .Project("{_id : 0, user : 0, creating_date : 0, hall : 0, status : 0}").ToListAsync();
            foreach (var item in documentList)
            {
                listDates.Add(new []{item["start_hall_reserving"].ToLocalTime(), item["end_hall_reserving"].ToLocalTime()});
            }

            return listDates;
        }
        public async Task<List<Hall>> GetAllHalls()
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("hall");
            var documentList = await collection.Find(FilterDefinition<BsonDocument>.Empty).Project("{ description : 0, price : 0}").ToListAsync();
            List<Hall> list = new List<Hall>();
            foreach (var item in documentList)
            {
                list.Add(new Hall {IdHall = item["_id"].AsInt32.ToString(), Name = item["hall_name"].AsString});
            }
            return list;
        }
        
        
    
        
    }
}