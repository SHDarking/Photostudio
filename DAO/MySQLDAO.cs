using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Photostudio.Models;


namespace Photostudio.DAO
{
    internal class MySQLDAO : DAOFactory,IMyDAO
    {
        private string ConnectionLink { get; }
        public MySQLDAO(IConfiguration configuration)
        {
            ConnectionLink = configuration["ConnectionStrings:MySqlCloudConnection"];
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionLink);
        }
        
        public async Task CreateUserAsync(User user)
        {
            string query = "insert into user(username, surname, phone_number, email, password,fk_role) " +
                           $"value('{user.UserName}','{user.Surname}','{user.PhoneNumber}', '{user.Email}', '{user.Password}', 1)";

            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query,connection);
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public async Task<List<Hall>> GetAllHallsAsync()
        {
            List<Hall> halls = new List<Hall>();
            string query = "select * from hall;";
            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query,connection);
                await using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Hall hall = new Hall();
                            hall.IdHall = reader.GetInt32(0).ToString();
                            hall.Name = reader.GetString(1);
                            hall.Description = reader.GetString(2);
                            hall.Price = reader.GetInt32(3);
                            halls.Add(hall);
                        }
                    }
                }
                connection.Close();
            }

            return halls;
        }

        public async Task<bool> CreateBookingAsync(Booking booking)
        {
            int idUser = await GetUserId(booking.User.Email);
            string querySelect = $"select 1 from booking where '{booking.StartHallReserving:s}' < end_hall_reserving " +
                                 $"and '{booking.EndHallReserving:s}' > start_hall_reserving and fk_hall = {booking.RentedHall.IdHall};";
            string querySelectStatus = "select id_status from status where status_name = 'Новый';";
            await using MySqlConnection connection = GetConnection();
            connection.Open();
            MySqlTransaction transaction = connection.BeginTransaction();
            MySqlCommand cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            try
            {
                cmd.CommandText = querySelect;
                await using(DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        throw new Exception("This date and time reserving for this hall is booked.");
                    }
                }

                cmd.CommandText = querySelectStatus;
                int statusId = 0;
                await using(DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    statusId = reader.GetInt32(0);
                }
                string queryInsert =
                    "insert into booking(fk_user, creating_date,fk_hall, start_hall_reserving, end_hall_reserving,fk_status) " +
                    $"value({idUser}, current_timestamp(), {booking.RentedHall.IdHall}, '{booking.StartHallReserving:s}', '{booking.EndHallReserving:s}',{statusId});";
                cmd.CommandText = queryInsert;
                cmd.ExecuteNonQuery();
                await transaction.CommitAsync();
                connection.Close();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                connection.Close();
                return false;
            }
        }

        public async Task UpdateBookingStatusAsync(int idBooking)
        {
            string query = $"select status_name from booking join status on id_status = fk_status where id_booking = {idBooking};";
            
            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                string status;
                await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    status = reader.GetString(0);
                }

                string newStatus = "";
                switch (status)
                {
                    case "Новый" : newStatus = "Ожидает оплаты";
                        break;
                    case "Ожидает оплаты": newStatus = "Подтвержден"; 
                        break;
                    case "Подтвержден": newStatus = "Выполнен";
                        break;
                }

                cmd.CommandText = $"select id_status from status where status_name = '{newStatus}'";
                int statusId;
                await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    statusId = reader.GetInt32(0);
                }
                cmd.CommandText = $"update booking set fk_status = {statusId} where id_booking = {idBooking};";
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public async Task DeleteBookingAsync(int idBooking)
        {
            string query = $"delete from booking where id_booking = {idBooking};";
            using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                await connection.CloseAsync();
            }
        }

        public async Task CancelBookingAsync(int idBooking)
        {
            string query = "select id_status from status where status_name = 'Отменен'";
            
            using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                int statusId;
                await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    statusId = reader.GetInt32(0);
                }
                cmd.CommandText =$"update booking set fk_status = {statusId} where id_booking = {idBooking};";
                cmd.ExecuteNonQuery();
                await connection.CloseAsync();
            }
        }

        public async Task<User> FindUserByEmailAndPassword(string email, string password)
        {
            User user = null;
            
            string query = "select id_user, username, surname, phone_number, email, password, role_name from user join role on id_role = fk_role where email = @email and password = @password;";
            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query,connection);
                MySqlParameter emailParameter = new MySqlParameter("@email", email);
                cmd.Parameters.Add(emailParameter);
                MySqlParameter passwordParameter = new MySqlParameter("@password", password);
                cmd.Parameters.Add(passwordParameter);
                await using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        user = new User(reader.GetInt32(0).ToString(), reader.GetString(1),reader.GetString(2),
                            reader.GetString(3), reader.GetString(4), reader.GetString(5),
                            reader.GetString(6));
                    }
                }
                connection.Close();
            }
            return user;
        }

        public async Task<bool> FindUserByEmail(string email)
        {
            string query = $"select 1 from user where email = '{email}';";
            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                await using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return true;
                    }
                }
                connection.Close();
            }

            return false;
        }
        public async Task<List<Booking>> GetAllBookingsByUserEmailAsync(string email)
        {
            List<Booking> bookings = new List<Booking>();
            
            string query = $"call GetHistoryBookingsForUser('{email}');";

            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query,connection);
                await using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Booking booking = new Booking();
                            booking.IdBooking = reader.GetInt32(0).ToString();
                            booking.CreatingDateTime = reader.GetDateTime(1);
                            Hall hall = new Hall();
                            hall.Name = reader.GetString(2);
                            booking.RentedHall = hall;
                            booking.StartHallReserving = reader.GetDateTime(3);
                            booking.EndHallReserving = reader.GetDateTime(4);
                            booking.TotalCost = reader.GetInt32(5);
                            booking.Status = reader.GetString(6);
                            bookings.Add(booking);
                        }
                    }
                }
                connection.Close();
            }

            return bookings;
        }

        public async Task<List<Booking>> GetAllBookingsByStatusAsync()
        {
            List<Booking> bookings = new List<Booking>();
            
            string query = "call GetAllBookingsByStatus();";

            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query,connection);
                await using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Booking booking = new Booking();
                            booking.IdBooking = reader.GetInt32(0).ToString();
                            User user = new User
                            {
                                UserName = reader.GetString(1),
                                Surname = reader.GetString(2),
                                PhoneNumber = reader.GetString(3)
                            };
                            booking.User = user;
                            booking.CreatingDateTime = reader.GetDateTime(4);
                            Hall hall = new Hall {Name = reader.GetString(5)};
                            booking.RentedHall = hall;
                            booking.StartHallReserving = reader.GetDateTime(6);
                            booking.EndHallReserving = reader.GetDateTime(7);
                            booking.TotalCost = reader.GetInt32(8);
                            booking.Status = reader.GetString(9);
                            bookings.Add(booking);
                        }
                    }
                }
                connection.Close();
            }

            return bookings;
        }

        public async Task<List<DateTime[]>> GetCalendarHall(int id, DateTime dayStartFindWeek, DateTime dayEndFindWeek)
        {
            List<DateTime[]> listDates= new List<DateTime[]>();
            string query = "select start_hall_reserving,end_hall_reserving from booking " +
                           $"where fk_hall = {id} " +
                           "and fk_status in (1,2,3) " +
                           $"and start_hall_reserving between '{dayStartFindWeek:u}' and '{dayEndFindWeek:u}';";
            
            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                await using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            listDates.Add(new[] { reader.GetDateTime(0), reader.GetDateTime(1) });
                        }
                    }
                }
                connection.Close();
            }
            return listDates;
        }

        public async Task<List<Hall>> GetAllHalls()
        {
            string query = "select id_hall, hall_name from hall;";
            List<Hall> list = new List<Hall>();
            using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Hall hall = new Hall {IdHall = reader.GetInt32(0).ToString(), Name = reader.GetString(1)};
                            list.Add(hall);
                        }
                    }
                }

                await connection.CloseAsync();
            }

            return list;
        }

        private async Task<int> GetUserId(string email)
        {
            string query = $"select id_user from user where email = '{email}';";
            int idUser;
            using (MySqlConnection connection = GetConnection())
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    idUser = reader.GetInt32(0);
                }

                connection.Close();
            }

            return idUser;
        }
        
        public async Task<Dictionary<string,List<Dictionary<string,string>>>> ReadDataForMigrationToOtherDb()
        {
            var userList = new List<Dictionary<string, string>>();
            var hallList = new List<Dictionary<string, string>>();
            var bookingList = new List<Dictionary<string, string>>();
            
            await using MySqlConnection connection = GetConnection();
            connection.Open();
            string query = "select id_user, username, surname, phone_number, email, password, role_name from user join role on id_role = fk_role order by id_user;";
            MySqlCommand cmd = new MySqlCommand(query, connection);
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        userList.Add(new Dictionary<string, string>
                        {
                            {"Id",reader.GetInt32(0).ToString()},
                            {"Username",reader.GetString(1)},
                            {"Surname",reader.GetString(2)},
                            {"PhoneNumber",reader.GetString(3)},
                            {"Email",reader.GetString(4)},
                            {"Password",reader.GetString(5)},
                            {"Role",reader.GetString(6)}
                        });
                    }
                }
            }
            query = "select id_hall, hall_name, description, price from hall order by id_hall;";
            cmd = new MySqlCommand(query, connection);
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        hallList.Add(new Dictionary<string, string>
                        {
                            {"Id",reader.GetInt32(0).ToString()},
                            {"Name",reader.GetString(1)},
                            {"Description",reader.GetString(2)},
                            {"Price",reader.GetInt32(3).ToString()}
                        });
                    }
                }
            }

            query = "select id_booking, fk_user, creating_date, hall_name, start_hall_reserving, end_hall_reserving, status_name from booking join" +
                    " hall on id_hall = fk_hall join status on id_status = fk_status order by id_booking;";
            cmd = new MySqlCommand(query, connection);
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync())
            {
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        bookingList.Add(new Dictionary<string, string>
                        {
                            {"Id", reader.GetInt32(0).ToString()},
                            {"User", reader.GetInt32(1).ToString()},
                            {"CreatingDate",reader.GetDateTime(2).ToString(CultureInfo.InvariantCulture)},
                            {"Hall",reader.GetString(3)},
                            {"StartHallReserving",reader.GetDateTime(4).ToString(CultureInfo.InvariantCulture)},
                            {"EndHallReserving",reader.GetDateTime(5).ToString(CultureInfo.InvariantCulture)},
                            {"Status",reader.GetString(6)}
                        });
                    }
                }
            }

            return new Dictionary<string, List<Dictionary<string, string>>>
            {
                {"user", userList},
                {"hall", hallList},
                {"booking", bookingList}
            };
        }

        public async Task WriteDataToDbAfterRead(Dictionary<string,List<Dictionary<string, string>>> tables)
        {
            var userList = tables["user"];
            var hallList = tables["hall"];
            var bookingList = tables["booking"];
            var statusList = tables["status"];
            var roleList = tables["role"];
            
            await using (MySqlConnection connection = GetConnection())
            {
                connection.Open();

                foreach (var item in roleList)
                {
                    string query = $"insert into role value({item["Id"]},'{item["Role"]}');";
                    await using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                foreach (var item in statusList)
                {
                    string query = $"insert into status value({item["Id"]}, '{item["Status"]}');";
                    await using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                foreach (var item in hallList)
                {
                    string query = $"insert into hall value({item["Id"]}, '{item["Name"]}', '{item["Description"]}', {item["Price"]});";
                    await using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    } 
                }
                foreach (var item in userList)
                {
                    string query =
                        $"insert into user value({item["Id"]}, '{item["Name"]}', '{item["Surname"]}', '{item["PhoneNumber"]}', '{item["Email"]}'," +
                        $" '{item["Password"]}', {item["Role"]});";
                    await using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    } 
                }
                foreach (var item in bookingList)
                {
                    string query =
                        $"insert into booking value({item["Id"]}, {item["User"]}, '{item["CreatingDate"]}', {item["Hall"]}, '{item["StartHallReserving"]}'," +
                        $" '{item["EndHallReserving"]}', {item["Status"]});";
                    await using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    } 
                }
                connection.Close();
                
                
            }
        }
    }
}