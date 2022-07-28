using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace CapstoneAPI.Controllers
{
    [ApiController]
    public class SoundController : ControllerBase
    {
        private const string DataSource = "";
        private const string UserID = "";
        private const string Password = "";
        private const string InitialCatalog = "";

        [HttpGet("sounds/all")]
        [EnableCors("CorsAllowAll")]
        public IEnumerable<object[]> Get([FromQuery] string userID)  
        {
            List<object[]> result = new List<object[]>();
           
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = DataSource;
            builder.UserID = UserID;
            builder.Password = Password;
            builder.InitialCatalog = InitialCatalog;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                String sql = $"SELECT * FROM Capstone WHERE UserID = '{userID}'";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    int cols = 3;//how many columns in sql
                    while (reader.Read())
                    {
                        object[] row = new object[cols];
                        for (int i = 0; i < cols; i++)
                        {
                            row[i] = reader[i];
                        }
                        result.Add(row);
                    }
                }
            }
            return result;
        }

        [HttpGet("sounds/post")]
        [EnableCors("CorsAllowAll")]
        public ActionResult<Sound> Post([FromQuery] int soundValue, int messageNumber, string userID)
        {
            Sound result = new Sound();
            result.soundValue = soundValue;
            result.messageNumber = messageNumber;
            result.userID = userID;

            int Threshold;
            long PhoneNumber;
            string Message;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = DataSource;
            builder.UserID = UserID;
            builder.Password = Password;
            builder.InitialCatalog = InitialCatalog;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string sql = $"BEGIN TRANSACTION; " +
                    $"INSERT INTO Capstone(SensorValue, MessageNumber, UserID) VALUES({soundValue},{messageNumber},'{userID}'); " +
                    $"SELECT m.Threshold, m.Message, u.PhoneNumber FROM Messages m JOIN Users u ON m.UserID = u.UserID WHERE m.UserID = '{userID}'; " +
                    $"COMMIT;";
                using SqlCommand command = new SqlCommand(sql, connection);
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Threshold = (int)reader[0];
                        Message = (string)reader[1];
                        PhoneNumber = (long)reader[2];
                        if (soundValue >= Threshold)
                        {
                            SendSMS(PhoneNumber.ToString(), Message);
                        }
                    }
                }
            }
            return result;
        }

        [HttpGet("sounds/clear")]
        [EnableCors("CorsAllowAll")]
        public ActionResult<Sound> Clear()
        {
            Sound result = new Sound();
            result.soundValue = 1;

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = DataSource;
            builder.UserID = UserID;
            builder.Password = Password;
            builder.InitialCatalog = InitialCatalog;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                String sql = "DELETE FROM Capstone";
                using SqlCommand command = new SqlCommand(sql, connection);
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            return result;
        }

        [HttpGet("sounds/setSMS")]
        [EnableCors("CorsAllowAll")]
        public ActionResult<Sound> SetSMS([FromQuery] long phoneNumber, [FromQuery] string message, [FromQuery] int threshold, [FromQuery] string userID)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = DataSource;
            builder.UserID = UserID;
            builder.Password = Password;
            builder.InitialCatalog = InitialCatalog;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                string sql = $"BEGIN TRANSACTION; " +
                    $"UPDATE Users " +
                    $"SET PhoneNumber = {phoneNumber} " +
                    $"WHERE UserID = '{userID}'; " +

                    $"UPDATE Messages " +
                    $"SET Message = '{message}', Threshold = {threshold} " +
                    $"WHERE UserID = '{userID}'; " +
                    $"COMMIT;";
                using SqlCommand command = new SqlCommand(sql, connection);
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            Sound result = new Sound();
            result.soundValue = 1;
            return result;
        }

        /* 
        * if not working install Twilio from NuGet
        * select "Tools," "NuGet Package Manager," and "Package Manager Console"
        * from that console type this command:
        * Install-Package Twilio​
        * 
        * more details: https://www.twilio.com/docs/sms/tutorials/how-to-send-sms-messages-csharp
        */
        private static void SendSMS(string phoneNumber /* numbers only */, string smsMessage)
        {
            // change phoneNumber to E.164 format, contry code US
            phoneNumber = "+1" + phoneNumber.Trim();

            // Find your Account SID and Auth Token at twilio.com/console
            // and set the environment variables. See http://twil.io/secure
            string accountSid = "accountSid";
            string authToken = "authToken";

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
                body: smsMessage,
                from: new Twilio.Types.PhoneNumber("+12223334444"), // Replace with Twilio number
                to: new Twilio.Types.PhoneNumber(phoneNumber)
            );
        }

    }
}