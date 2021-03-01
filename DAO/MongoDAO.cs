using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            var user =  collection.Find(filter).First();
            if (user == null) return null;
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
        
        // метод для миграции данных
        public async Task<Dictionary<string,List<Dictionary<string,string>>>> ReadDataForMigrationToOtherDb()
        {
            var userList = new List<Dictionary<string,string>>();
            var hallList = new List<Dictionary<string,string>>();
            var bookingList = new List<Dictionary<string,string>>();
            var statusList = new List<Dictionary<string,string>>();
            var roleList = new List<Dictionary<string,string>>();
            
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");

            var userCollection = database.GetCollection<BsonDocument>("user");
            var hallCollection = database.GetCollection<BsonDocument>("hall");
            var bookingCollection = database.GetCollection<BsonDocument>("booking");

            var roleDocumentList = await userCollection.Distinct<string>("role", FilterDefinition<BsonDocument>.Empty).ToListAsync();
            for (int i = 0; i < roleDocumentList.Count; i++)
            {
                roleList.Add(new Dictionary<string, string>
                {
                    {"Id", (i + 1).ToString()},
                    {"Role", roleDocumentList[i]}
                });
            }
            
            var statusDocumentList = await bookingCollection.Distinct<string>("status", FilterDefinition<BsonDocument>.Empty).ToListAsync();
            for (int i = 0; i < statusDocumentList.Count; i++)
            {
                statusList.Add(new Dictionary<string, string>
                {
                    {"Id", (i + 1).ToString()},
                    {"Status", statusDocumentList[i]}
                });
            }
            
            var hallDocumentList = await hallCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            foreach(var item in hallDocumentList)
            {
                hallList.Add(new Dictionary<string, string>
                {
                    {"Id", item["_id"].AsInt32.ToString()},
                    {"Name", item["hall_name"].AsString},
                    {"Description", item["description"].AsString},
                    {"Price", item["price"].AsInt32.ToString()}
                });
            }

            var userDocumentList = await userCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            foreach (var item in userDocumentList)
            {
                int roleId = 0;
                foreach (var t in roleList)
                {
                    if (t["Role"] == item["role"].AsString)
                    {
                        roleId = int.Parse(t["Id"]);
                    }
                }
                userList.Add(new Dictionary<string, string>
                {
                    {"Id", item["_id"].AsInt32.ToString()},
                    {"Name", item["name"].AsString},
                    {"Surname", item["surname"].AsString},
                    {"PhoneNumber", item["phone_number"].AsString},
                    {"Email", item["email"].AsString},
                    {"Password", item["password"].AsString},
                    {"Role", roleId.ToString()},
                    
                });
            }

            var bookingDocumentList = await bookingCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            foreach (var item in bookingDocumentList)
            {
                int idHall = 0;
                int statusId = 0;
                foreach (var t in hallList)
                {
                    if (t["Name"] == item["hall"].AsString)
                    {
                        idHall = int.Parse(t["Id"]);
                    }
                }

                foreach (var t in statusList)
                {
                    if (t["Status"] == item["status"].AsString)
                    {
                        statusId = int.Parse(t["Id"]);
                    } 
                }
                bookingList.Add(new Dictionary<string, string>
                {
                    {"Id", item["_id"].AsInt32.ToString()},
                    {"User", item["user"].AsInt32.ToString()},
                    {"CreatingDate", item["creating_date"].ToLocalTime().ToString("s")},
                    {"Hall", idHall.ToString()},
                    {"StartHallReserving",item["start_hall_reserving"].ToLocalTime().ToString("s")},
                    {"EndHallReserving", item["end_hall_reserving"].ToLocalTime().ToString("s")},
                    {"Status", statusId.ToString()}
                });
            }
            
            return new Dictionary<string, List<Dictionary<string, string>>>
            {
                {"user", userList},
                {"hall", hallList},
                {"booking", bookingList},
                {"status", statusList},
                {"role", roleList}
            };
        }
        public async Task WriteDataToDbAfterRead(Dictionary<string,List<Dictionary<string, string>>> tables)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            
            var userCollection = database.GetCollection<BsonDocument>("user");
            var hallCollection = database.GetCollection<BsonDocument>("hall");
            var bookingCollection = database.GetCollection<BsonDocument>("booking");

            foreach (var item in tables)
            {
                List<BsonDocument> documentList;
                if (item.Key == "user")
                {
                    documentList = new List<BsonDocument>();
                    foreach (var listItem in item.Value)
                    {
                        documentList.Add(new BsonDocument
                        {
                            {"_id", new BsonInt32(int.Parse(listItem["Id"]))},
                            {"name", listItem["Username"]},
                            {"surname", listItem["Surname"]},
                            {"phone_number", listItem["PhoneNumber"]},
                            {"email", listItem["Email"]},
                            {"password", listItem["Password"]},
                            {"role", listItem["Role"]}
                        });
                    }

                    await userCollection.InsertManyAsync(documentList);
                }
                else if (item.Key == "hall")
                {
                    documentList = new List<BsonDocument>();
                    foreach (var listItem in item.Value)
                    {
                        documentList.Add(new BsonDocument
                        {
                            {"_id", new BsonInt32(int.Parse(listItem["Id"]))},
                            {"hall_name", listItem["Name"]},
                            {"description", listItem["Description"]},
                            {"price", new BsonInt32(int.Parse(listItem["Price"]))}
                        });
                    }

                    await hallCollection.InsertManyAsync(documentList);
                }
                else if (item.Key == "booking")
                {
                    documentList = new List<BsonDocument>();
                    foreach (var listItem in item.Value)
                    {
                        documentList.Add(new BsonDocument
                        {
                            {"_id", new BsonInt32(int.Parse(listItem["Id"]))},
                            {"user", new BsonInt32(int.Parse(listItem["User"]))},
                            {"creating_date", new BsonDateTime(DateTime.Parse(listItem["CreatingDate"],CultureInfo.InvariantCulture))},
                            {"hall", listItem["Hall"]},
                            {"start_hall_reserving", new BsonDateTime(DateTime.Parse(listItem["StartHallReserving"],CultureInfo.InvariantCulture))},
                            {"end_hall_reserving", new BsonDateTime(DateTime.Parse(listItem["EndHallReserving"],CultureInfo.InvariantCulture))},
                            {"status", listItem["Status"]}
                        });
                    }

                    await bookingCollection.InsertManyAsync(documentList);
                }
                
                
            }
        }
        
        // методы для репликации данных
        public async Task<List<User>> WriteDataForReplication(int writeConcern)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            WriteConcern concern = null;
            switch (writeConcern)
            {
                case 1: concern = WriteConcern.W1;
                    break;
                case 2: concern = WriteConcern.Unacknowledged;
                    break;
                case 3: concern = WriteConcern.WMajority;
                    break;
            }
            var collection = database.GetCollection<BsonDocument>("user").WithWriteConcern(concern);
            
            int lastId = collection.Find(FilterDefinition<BsonDocument>.Empty).Sort("{_id : -1}").Limit(1).First()["_id"].AsInt32;
            var listUsers = new List<Dictionary<string, string>>
            {
                new() {{"Name","Фекла"},{"Surname","Фадеева"}},
                new() {{"Name","Бронислав"},{"Surname","Воронов"}},
                new() {{"Name","Германн"},{"Surname","Дмитриев"}},
                new() {{"Name","Александр"},{"Surname","Орехов"}},
                new() {{"Name","Демьян"},{"Surname","Шестаков"}},
                new() {{"Name","Антонина"},{"Surname","Гущина"}},
                new() {{"Name","Игнатий"},{"Surname","Шестаков"}},
                new() {{"Name","Онисим"},{"Surname","Егоров"}},
                new() {{"Name","Созон"},{"Surname","Титов"}},
                new() {{"Name","Петр"},{"Surname","Поляков"}},
                new() {{"Name","Демьян"},{"Surname","Красильников"}},
                new() {{"Name","Ефим"},{"Surname","Макаров"}},
                new() {{"Name","Лидия"},{"Surname","Ширяева"}},
                new() {{"Name","Василий"},{"Surname","Баранов"}},
                new() {{"Name","Михаил"},{"Surname","Носков"}},
                new() {{"Name","Владлен"},{"Surname","Бобров"}},
                new() {{"Name","Валентина"},{"Surname","Блинова"}},
                new() {{"Name","Денис"},{"Surname","Сидоров"}},
                new() {{"Name","Федот"},{"Surname","Прохоров"}},
                new() {{"Name","Элеонора"},{"Surname","Одинцова"}}
            };
            int i = lastId + 1;
            int lastIndex = i + 10000;
            int countErrors = 0;
            while(i < lastIndex)
            {
                var random = new Random();
                var randNum = random.Next(0, listUsers.Count-1);
                try
                {
                    await collection.InsertOneAsync(new BsonDocument
                    {
                        {"_id", i},
                        {"name", listUsers[randNum]["Name"]},
                        {"surname", listUsers[randNum]["Surname"]},
                        {"phone_number", "+380505554545"},
                        {"email", $"test.mail{i}@gmail.com"},
                        {"password", $"user{i}"},
                        {"role", "User"}
                    });
                    i++;
                    countErrors = 0;
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                    countErrors++;
                    if (countErrors > 3)
                    {
                        break;
                    }
                }
            }

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Gt("_id", lastId);
            var list =await collection.Find(filter).Sort("{_id : 1}").ToListAsync();
            var userList = new List<User>();
            foreach (var item in list)
            {
                User user = new User
                {
                    Id = item["_id"].AsInt32.ToString(),
                    UserName = item["name"].AsString,
                    Surname = item["surname"].AsString,
                    PhoneNumber = item["phone_number"].AsString,
                    Email = item["email"].AsString,
                    Password = item["password"].AsString,
                    Role = item["role"].AsString
                };
                userList.Add(user);
            }

            return userList;
        }
        
        // for lab 6 aggregation methods
        
        public async Task<List<User>> FindUserAggregationAsync(string email, string password)
        {

            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("user");
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Eq("email", email) & filterBuilder.Eq("password", password);
            var  document = await collection.Aggregate().Match(filter).ToListAsync();
            
            if (document.Count == 0) return null;
            
            var userList = new List<User>(1);
            foreach (var item in document)
            {
                userList.Add(new User
                {
                    UserName = item["name"].AsString,
                    Surname = item["surname"].AsString,
                    PhoneNumber = item["phone_number"].AsString,
                    Email = item["email"].AsString,
                    Role = item["role"].AsString
                });
            }
            
            return userList;
        }

        public async Task<List<Hall>> GetHallsAggregationAsync(int price)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("hall");
            var filterBuilder = Builders<BsonDocument>.Filter;
            var filter = filterBuilder.Gte("price", price);
            var documentList =await collection.Aggregate().Match(filter).ToListAsync();

            var hallList = new List<Hall>();

            foreach (var item in documentList)
            {
                hallList.Add(new ()
                {
                    IdHall = item["_id"].AsInt32.ToString(),
                    Name = item["hall_name"].AsString,
                    Price = item["price"].AsInt32
                });
            }

            return hallList;
        }

        public async Task<List<Booking>> GetBookingAggregationAsync(DateTime dateTime)
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var filter = Builders<BsonDocument>.Filter.Gte("creating_date",new BsonDateTime(dateTime));
            var documentList =await collection.Aggregate().Match(filter).Project("{user : 0}").ToListAsync();
            var bookingList = new List<Booking>();

            foreach (var item in documentList)
            {
                bookingList.Add(new Booking
                {
                    IdBooking = item["_id"].AsInt32.ToString(),
                    RentedHall = new Hall {Name = item["hall"].AsString},
                    CreatingDateTime = item["creating_date"].ToLocalTime(),
                    StartHallReserving = item["start_hall_reserving"].ToLocalTime(),
                    EndHallReserving = item["end_hall_reserving"].ToLocalTime(),
                    Status = item["status"].AsString
                });
            }

            return bookingList;
        }

        public async Task<List<Booking>> GetCountBookingsByUserAggregationAsync()
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var documentList =await collection.Aggregate().Group(new BsonDocument
            {
                {"_id", "$user"},
                {"count", new BsonDocument("$sum",1) }
            }).ToListAsync();
            
            var bookingList = new List<Booking>();
            foreach (var item in documentList)
            {
                bookingList.Add(new Booking
                {
                    User = new User {UserName =  database.GetCollection<BsonDocument>("user").Aggregate()
                        .Match(Builders<BsonDocument>.Filter.Eq("_id",item["_id"].AsInt32)).First()["name"].AsString},
                    Count = item["count"].AsInt32
                });
            }

            return bookingList;
        }

        public async Task<List<Booking>> GetBookingByHallAggregationAsync()
        {
            var client = GetConnection();
            var database = client.GetDatabase("photostudio");
            var collection = database.GetCollection<BsonDocument>("booking");
            var filter =
                Builders<BsonDocument>.Filter.In("status", new BsonArray {"Новый", "Ожидает оплаты", "Подтвержден"});
            var documentList =await collection.Aggregate().Match(filter).Group(new BsonDocument
            {
                {"_id","$hall"},
                {"count", new BsonDocument("$sum",1)}
            }).ToListAsync();

            var bookingList = new List<Booking>();
            foreach (var item in documentList)
            {
                bookingList.Add(new Booking
                {
                    RentedHall = new Hall {Name = item["_id"].AsString},
                    Count = item["count"].AsInt32
                });
            }

            return bookingList;
        }
        
    }
}