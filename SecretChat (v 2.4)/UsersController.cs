using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

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

        public class FriendsModel
        {
            public List<MessageEntity> FriendsList { get; set; } = new List<MessageEntity>();
        }

        public StoredUserEntity()
        {

        }


        public string storedMac { get; set; }

        public string UserID { get; set; }

        public bool Blocked { get; set; }

        public string Picture { get; set; }

        public string Verification { get; set; }

        public string SecretCode { get; set; }

        public string oldName { get; set; }

        public string Username { get; set; }

        public string ProfilePicture { get; set; }

        public string Info { get; set; }
    }
    public class UsersController
    {
        private readonly HttpClient _client;

        public UsersController()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(Common.Constats.ApiBaseAddress)
            };
        }

        public string InsertUser(string user, string macAndUser, string picture, string vertification, string code, string userID, string info)
        {
            try
            {

                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = user,
                    storedMac = macAndUser,
                    Picture = picture,
                    Verification = vertification,
                    SecretCode = code,
                    UserID = userID,
                    Info = info
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync($"Users/Insert", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    return res;
                }
                else
                    return "ErrorStatusCode: " + response.StatusCode;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public string UpdateUser(string user, string macAndUser, string picture, string vertification, string code, string oldName, string userID)
        {
            try
            {

                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = user,
                    storedMac = macAndUser,
                    Picture = picture,
                    Verification = vertification,
                    SecretCode = code,
                    oldName = oldName,
                    UserID = userID
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync($"Users/Insert", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    return res;
                }
                else
                    return "ErrorStatusCode: " + response.StatusCode;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }



        public string InsertNewUser(string name, string macAndUser, string picture, string vertification, string code, string userID)
        {
            try
            {

                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = "<NewUserHere>",
                    RowKey = name,
                    storedMac = macAndUser,
                    Picture = picture,
                    Verification = vertification,
                    SecretCode = code,
                    UserID = userID
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync($"Users/Insert", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    return res;
                }
                else
                    return "ErrorStatusCode: " + response.StatusCode;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }



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
                            Picture = entity.Picture,
                            Verification = entity.Verification,
                            Blocked = entity.Blocked,
                            UserID = entity.UserID,
                            Info = entity.Info
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

        public List<StoredUserEntity> GetSpecificUser(string userName, string macAndUser, string code)
        {
            List<StoredUserEntity> _records = new List<StoredUserEntity>();

            HttpResponseMessage response = _client.GetAsync("Users/SpecificUser?userName=" + userName + "&macAndUser=" + macAndUser + "&secretCode=" + code).Result;
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
                            Verification = entity.Verification,
                            Blocked = entity.Blocked,
                            UserID = entity.UserID,
                            Info = entity.Info
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

        public List<StoredUserEntity> GetSpecificUserName(string macAndUser, string code)
        {
            List<StoredUserEntity> _records = new List<StoredUserEntity>();

            HttpResponseMessage response = _client.GetAsync("Users/SpecificUser?macAndUser=" + macAndUser + "&secretCode=" + code).Result;
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
                            Verification = entity.Verification,
                            Blocked = entity.Blocked,
                            UserID = entity.UserID
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

        public string DeleteStoredUser(string user, string macAndUser, string code)
        {
            try
            {
                StoredUserEntity User = new StoredUserEntity
                {
                    PartitionKey = user,
                    storedMac = macAndUser,
                    SecretCode = code
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(User));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = _client.PostAsync("Users/Delete", content).Result;
                if (response.IsSuccessStatusCode)
                {
                    var Data = response.Content.ReadAsStringAsync();
                    var res = Data.Result;
                    return res;
                }
                else
                    return "ErrorStatusCode: " + response.StatusCode;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}

