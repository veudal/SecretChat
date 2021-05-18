using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;

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
            string dorl = "dark";
            if (File.Exists(path + "\\secretchat\\darkorlight.txt"))
            {
                dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
            }
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

        public string SecretCode { get; set; }

        public string StoredMac { get; set; }

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

    public class OnlineUsersController : INotifyPropertyChanged
    {
        readonly string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\secretchat\\";

        private readonly HttpClient _client;

        public event PropertyChangedEventHandler PropertyChanged;

        public string HoverColor { get; set; }

        public string StillFocusedColor { get; set; }

        private Visibility _visibility;
        public Visibility ChatVisibility 
        { 
            get
            {
                return _visibility;
            }
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    OnPropertyChange("ChatVisibility");
                }
            }
        }

        protected void OnPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public OnlineUsersController()
        {

            _client = new HttpClient
            {
                BaseAddress = new Uri(Common.Constats.ApiBaseAddress)
            };
            ChatVisibility = Visibility.Collapsed;
            //Visibility = Visibility.Collapsed;
            if (System.IO.File.ReadAllText(settingsPath + "darkorlight.txt") == "light")
            {
                StillFocusedColor = "#FFafcdf2";
                HoverColor = "#FFafcdf2";
            }
            else
            {
                StillFocusedColor = "#FF393C43";
                HoverColor = "#FF34373C";
            }

        }

        public string InsertOnlineUser(string user, string onlineOrAbsent, DateTime? time, string macAndUser, string secretCode)
        {

            OnlineUserEntity Message = new OnlineUserEntity
            {
                userName = user,
                Status = onlineOrAbsent,
                Time = time,
                StoredMac = macAndUser,
                SecretCode = secretCode
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"OnlineUsers/Insert", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = Data.Result;
                return res;
            }
            return "ErrorStatusCode: " + response.StatusCode;
        }

        public List<OnlineUserEntity> GetAllOnlineAndAbsentUsers(string userName, string macAndUser, string secretCode)
        {
            List<OnlineUserEntity> _records = new List<OnlineUserEntity>();

            HttpResponseMessage response = _client.GetAsync($"OnlineUsers/AllOnlineAndAbsentUsers?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
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
                        Status = entity.Status
                        //Timestamp = entity.Timestamp.ToLocalTime(),
                    };
                    _records.Add(ItemData);
                }
            }
            return _records;
        }

        //public List<OnlineUserEntity> GetAllAbsentUsers(string userName, string macAndUser, string secretCode)
        //{
        //    List<OnlineUserEntity> _records = new List<OnlineUserEntity>();

        //    HttpResponseMessage response = _client.GetAsync($"OnlineUsers/AllAbsentUsers?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var Data = response.Content.ReadAsStringAsync();
        //        List<OnlineUserEntity> categories = JsonConvert.DeserializeObject<List<OnlineUserEntity>>(Data.Result);
        //        foreach (var entity in categories)
        //        {
        //            var ItemData = new OnlineUserEntity
        //            {
        //                Time = entity.Time,
        //                userName = entity.userName,
        //                //Timestamp = entity.Timestamp.ToLocalTime(),
        //            };
        //            _records.Add(ItemData);
        //        }
        //    }
        //    return _records;
        //}

        public string DeleteOnlineUser(string user, string macAndUser, string secretCode)
        {

            OnlineUserEntity Message = new OnlineUserEntity
            {
                PartitionKey = user,
                StoredMac = macAndUser,
                SecretCode = secretCode
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"OnlineUsers/Delete", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = Data.Result;
                return res;
            }
            else
                return "ErrorStatusCode: " + response.StatusCode;
        }
    }
}
