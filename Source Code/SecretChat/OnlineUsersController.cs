using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecretChat
{
    //public class OnlineUsersModel
    //{

    //    public List<UserEntity> OnlineUsers { get; set; } = new List<UserEntity>();
    //}

    //public class AbsentUsersModel
    //{
    //    public List<UserEntity> AbsentUsers { get; set; } = new List<UserEntity>();
    //}

    public class OnlineUserEntity : TableEntity
    {
        // Set up Partition and Row Key information
        public OnlineUserEntity(string userID)
        {
            this.PartitionKey = userID;
        }


        public OnlineUserEntity()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
            if (dorl == "light")
            {
                this.TextColor = "Black";
            }
            else
            {
                this.TextColor = "White";
            }
            this.Visibility = "Visible";
        }

        public string userName { get; set; }

        public string Visibility { get; set; }

        public bool IsTyping { get; set; }

        public string Status { get; set; }

        public StoredUserEntity User { get; set; }

        public StoredUserEntity AbsentUser { get; set; }

        public string TextColor { get; set; }

        public DateTime? Time { get; set; }

        public string Dot1Font { get; set; }

        public string Dot2Font { get; set; }

        public string Dot3Font { get; set; }





    }

    public class OnlineUsersController
    {
        private HttpClient _client;

        public OnlineUsersController()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(Common.Constats.ApiBaseAddress);
        }

        public void InsertOnlineUser(string user, string onlineOrAbsent, DateTime? time, string macAndUser, string secretCode)
        {
            List<OnlineUserEntity> _records = new List<OnlineUserEntity>();

            OnlineUserEntity Message = new OnlineUserEntity
            {
                userName = user,
                Status = onlineOrAbsent,
                Time = time
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"OnlineUsers/Insert?userName={user}&macAndUser={macAndUser}&secretCode={secretCode}", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = Data.Result;

            }
        }

        public List<OnlineUserEntity> GetAllOnlineUsers(string userName, string macAndUser, string secretCode)
        {
            List<OnlineUserEntity> _records = new List<OnlineUserEntity>();

            HttpResponseMessage response = _client.GetAsync($"OnlineUsers/AllOnlineUsers?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                List<OnlineUserEntity> categories = JsonConvert.DeserializeObject<List<OnlineUserEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    var ItemData = new OnlineUserEntity
                    {
                        Time = entity.Time,
                        userName = entity.userName,
                        //Timestamp = entity.Timestamp.ToLocalTime(),
                    };
                    _records.Add(ItemData);
                }
            }
            return _records;
        }

        public List<OnlineUserEntity> GetAllAbsentUsers(string userName, string macAndUser, string secretCode)
        {
            List<OnlineUserEntity> _records = new List<OnlineUserEntity>();

            HttpResponseMessage response = _client.GetAsync($"OnlineUsers/AllAbsentUsers?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                List<OnlineUserEntity> categories = JsonConvert.DeserializeObject<List<OnlineUserEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    var ItemData = new OnlineUserEntity
                    {
                        Time = entity.Time,
                        userName = entity.userName,
                        //Timestamp = entity.Timestamp.ToLocalTime(),
                    };
                    _records.Add(ItemData);
                }
            }
            return _records;
        }   

        public Boolean DeleteOnlineUser(string user, string macAndUser, string secretCode)
        {
            List<OnlineUserEntity> _records = new List<OnlineUserEntity>();

            OnlineUserEntity Message = new OnlineUserEntity
            {
                PartitionKey = user
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"OnlineUsers/Delete?userName={user}&macAndUser={macAndUser}&secretCode={secretCode}", content).Result;
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
                return false;
        }
    }
}
