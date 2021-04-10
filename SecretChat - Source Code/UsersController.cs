using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SecretChat
{
    //public class UsersModel
    //{

    //    public List<StoredUserEntity> OnlineUsers { get; set; } = new List<StoredUserEntity>();
    //    public List<StoredUserEntity> AbsentUsers { get; set; } = new List<StoredUserEntity>();

    //}


    public class StoredUserEntity : TableEntity
    {
        // Set up Partition and Row Key information
        public StoredUserEntity(string storedUserID)
        {
            this.PartitionKey = storedUserID;
        }


        public StoredUserEntity()
        {

        }


        public string storedMac { get; set; }

        public bool Blocked { get; set; }

        public string Picture { get; set; }

        public string Verification { get; set; }

        public string SecretCode { get; set; }

    }
    public class UsersController
    {
        private HttpClient _client;

        public UsersController()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(Common.Constats.ApiBaseAddress);
        }

        public Boolean InsertUser(string user, string macAndUser, string picture, string vertification, string code)
        {
            Boolean bSuccess = false;          
            try
            {

                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = user,
                    storedMac = macAndUser,
                    Picture = picture,
                    Verification = vertification,
                    SecretCode = code
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync($"Users/Insert?userName={user}&macAndUser={macAndUser}&secretCode={code}" , content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    bSuccess = true;
                }
                else
                    bSuccess = false;
            }
            catch
            {
                bSuccess = false;
            }
            return bSuccess;
        }

        public Boolean UpdateUser(string user, string macAndUser, string picture, string vertification, string code, string oldName)
        {
            Boolean bSuccess = false;
            try
            {

                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = user,
                    storedMac = macAndUser,
                    Picture = picture,
                    Verification = vertification,
                    SecretCode = code
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync($"Users/Insert?userName={user}&macAndUser={macAndUser}&secretCode={code}&oldName={oldName}", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    bSuccess = true;
                }
                else
                    bSuccess = false;
            }
            catch
            {
                bSuccess = false;
            }
            return bSuccess;
        }

        public Boolean InsertNewUser(string user, string macAndUser, string picture, string vertification, string code)
        {
            Boolean bSuccess = false;
            try
            {

                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = user,
                    storedMac = macAndUser,
                    Picture = picture,
                    Verification = vertification,
                    SecretCode = code
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync($"Users/Insert?userName=<NewUserHere>&macAndUser={macAndUser}&secretCode={code}", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    bSuccess = true;
                }
                else
                    bSuccess = false;
            }
            catch
            {
                bSuccess = false;
            }
            return bSuccess;
        }

        //public List<StoredUserEntity> GetAllStoredUsers(string userName, string macAndUser, string secretCode)
        //{
        //    List<StoredUserEntity> _records = new List<StoredUserEntity>();

        //    HttpResponseMessage response = _client.GetAsync($"Users/AllStoredUsers?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var Data = response.Content.ReadAsStringAsync();
        //        List<StoredUserEntity> categories = JsonConvert.DeserializeObject<List<StoredUserEntity>>(Data.Result);
        //        if (categories != null)
        //        {
        //            foreach (var entity in categories)
        //            {
        //                var ItemData = new StoredUserEntity
        //                {
        //                    PartitionKey = entity.PartitionKey,
        //                    storedMac = entity.storedMac,
        //                    Picture = entity.Picture,
        //                    Verification = entity.Verification
        //                    //Timestamp = entity.Timestamp.ToLocalTime(),
        //                };
        //                _records.Add(ItemData);
        //            }
        //        }
        //        else
        //            return null;
        //    }
        //    return _records;
        //}

        public List<StoredUserEntity> GetStoredUsers(string macAndUser, string code)
        {
            List<StoredUserEntity> _records = new List<StoredUserEntity>();

            HttpResponseMessage response = _client.GetAsync("Users/StoredUser?macAndUser=" + macAndUser + "&secretCode=" + code).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                List<StoredUserEntity> categories = JsonConvert.DeserializeObject<List<StoredUserEntity>>(Data.Result);
                if (categories != null)
                {
                    foreach (var entity in categories)
                    {
                        var ItemData = new StoredUserEntity
                        {
                            PartitionKey = entity.PartitionKey,
                            storedMac = entity.storedMac,
                            Picture = entity.Picture,
                            Verification = entity.Verification
                            //Timestamp = entity.Timestamp.ToLocalTime(),
                        };
                        _records.Add(ItemData);
                    }
                }
                else
                    return null;
            }
            return _records;
        }

        public Boolean DeleteStoredUser(string user, string macAndUser, string code)
        {
            Boolean bSuccess = false;    
            try
            {
                StoredUserEntity User = new StoredUserEntity
                {
                     PartitionKey = user,
                     storedMac = macAndUser
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync("Users/Delete?userName=" + user + "&macAndUser=" + macAndUser + "&secretCode=" + code, content).Result;
                bSuccess = true;
            }
            catch
            {
                bSuccess = false;
            }
            return bSuccess;
        }
    }
}

