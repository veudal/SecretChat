using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using TableEntity = Microsoft.Azure.Cosmos.Table.TableEntity;

namespace SecretChat
{

    public class MessagesModel
    {
        public List<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
    }
    public class Channel1Model
    {
        public List<MessageEntity> Channel1Messages { get; set; } = new List<MessageEntity>();
    }



    public class MessageEntity : TableEntity
    {
        // Set up Partition and Row Key information
        public MessageEntity(string messageID)
        {
            this.PartitionKey = messageID;
            this.RowKey = messageID;
        }


        public MessageEntity()
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
                this.TextColor2 = "Black";
                HoverColor = "#FFafcdf2";
                StillFocusedColor = "#FFafcdf2";
            }
            else
            {
                this.TextColor = "White";
                this.TextColor2 = "White";
                HoverColor = "#FF34373C";
                StillFocusedColor = "#FF34373C";
            }

            EmoteVisibility = "Collapsed";
            EmoteSource = "pack://application:,,,/SecretChat;component/resources/placeholder.png";
            UnderlinedOrNot = "None";
            MessageOrInfo = "Visible";
            LeftOrCenter = "Left";
            MessageWidth = "900";
            TextWeight = "DemiBold";
            PointVisibility = "Hidden";

        }


        public string StillFocusedColor { get; set; }

        public string HoverColor { get; set; }

        public string from { get; set; }

        public string To { get; set; }

        public string Message { get; set; }

        public string SecretCode { get; set; }

        public string Channel { get; set; }

        public DateTime Time { get; set; }

        public string MessageID { get; set; }

        public string TextColor { get; set; }

        public string TextColor2 { get; set; }

        public string EmoteSource { get; set; }

        public string MessageWidth { get; set; }

        public string UnderlinedOrNot { get; set; }

        public string EmoteVisibility { get; set; }

        public string StoredMac { get; set; }

        public string MessageOrInfo { get; set; }

        public string LeftOrCenter { get; set; }

        public StoredUserEntity User { get; set; }

        //public StoredUserEntity AbsentUser { get; set; }

        public string TextWeight { get; set; }

        public string PointVisibility { get; set; }

        public string Friend
        {
            get
            {
                return $"{from}";
            }
            set { }
        }

        public string TimeString
        {
            get
            {
                return $"{Time.ToShortDateString()}  {Time.ToShortTimeString()}";
            }
            set { }
        }

    }

    public class MessageController
    {
      
        //public string _accountName { get; private set; } = "test";
        //private string _privateAccountName = "storage4cams";
        //public string _accountName 
        //{
        //    get
        //    {
        //        return _privateAccountName;
        //    } 
        //    set
        //    {
        //        _privateAccountName = value;
        //    }
        //}
        private readonly HttpClient _client;

        public MessageController()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(Common.Constats.ApiBaseAddress)
            };
          
        }

        public string InsertMessage(string messageID, string message, string userName, string macAndUser, string secretCode, string channel, string to)
        {
            MessageEntity Message = new MessageEntity
            {
                MessageID = messageID,
                Message = message,
                PartitionKey = userName,
                StoredMac = macAndUser,
                SecretCode = secretCode,
                Channel = channel,
                To = to,
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"Messages/Insert", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = Data.Result;
                return res;

            }
            else
            {
                return "ErrorStatusCode: " + response.StatusCode;
            }

        }

        public string DeleteAllMessages(string userName, string macAndUser, string secretCode)
        {
            MessageEntity Message = new MessageEntity
            {
                PartitionKey = userName,
                StoredMac = macAndUser,
                SecretCode = secretCode,
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"Messages/DeleteAll", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = Data.Result;
                return res;

            }
            else
            {
                return "ErrorStatusCode: " + response.StatusCode;
            }

        }

        public string ReplaceAllMessages(string oldUsername, string newUsername, string macAndUser, string secretCode)
        {
            MessageEntity Message = new MessageEntity
            {
                PartitionKey = oldUsername,
                RowKey = newUsername,
                StoredMac = macAndUser,
                SecretCode = secretCode,
            };

            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"Messages/ReplaceAll", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = Data.Result;
                return res;

            }
            else
            {
                return "ErrorStatusCode: " + response.StatusCode;
            }

        }

        public List<MessageEntity> GetMessages(string userName, string macAndUser, string secretCode)
        {
            List<MessageEntity> _records = new List<MessageEntity>();

            HttpResponseMessage response = _client.GetAsync($"Messages/Messages?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                List<MessageEntity> categories = JsonConvert.DeserializeObject<List<MessageEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    MessageEntity ItemData = new MessageEntity();
                    DateTime atLeastDate = new DateTime(2021, 05, 15);
                    if (entity.Time.Date > atLeastDate)
                        ItemData.Message = Encrypt.DecryptString(entity.Message, "fJx82E@$48!L");
                    else
                        ItemData.Message = entity.Message;

                    ItemData.from = entity.from;
                    ItemData.Time = entity.Time.ToLocalTime();
                    ItemData.MessageID = entity.MessageID;
                    ItemData.Channel = entity.Channel;
                    ItemData.To = entity.To;
                    _records.Add(ItemData);

                }
            }
            return _records;

        }

        public List<MessageEntity> GetAllMessages(string userName, string macAndUser, string secretCode)
        {
            List<MessageEntity> _records = new List<MessageEntity>();
            HttpResponseMessage response = _client.GetAsync($"Messages/AllMessages?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                IEnumerable<MessageEntity> categories = JsonConvert.DeserializeObject<IEnumerable<MessageEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    MessageEntity ItemData = new MessageEntity();
                    DateTime atLeastDate = new DateTime(2021, 05, 16);
                    if (entity.Time.Date > atLeastDate)
                        ItemData.Message = Encrypt.DecryptString(entity.Message, "fJx82E@$48!L");
                    else
                        ItemData.Message = entity.Message;

                    ItemData.from = entity.from;
                    ItemData.Time = entity.Time.ToLocalTime();
                    ItemData.MessageID = entity.MessageID;
                    ItemData.Channel = entity.Channel;
                    ItemData.To = entity.To;
                    _records.Add(ItemData);

                }
            }

            return _records;
        }

        public List<MessageEntity> FriendMessages(string userName, string macAndUser, string secretCode, string to)
        {
            List<MessageEntity> _records = new List<MessageEntity>();
            HttpResponseMessage response = _client.GetAsync($"Messages/FriendMessages?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}&to={to}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                IEnumerable<MessageEntity> categories = JsonConvert.DeserializeObject<IEnumerable<MessageEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    MessageEntity ItemData = new MessageEntity();
                    DateTime atLeastDate = new DateTime(2021, 05, 16);
                    if (entity.Time.Date > atLeastDate)
                        ItemData.Message = Encrypt.DecryptString(entity.Message, "fJx82E@$48!L");
                    else
                        ItemData.Message = entity.Message;

                    ItemData.from = entity.from;
                    ItemData.Time = entity.Time.ToLocalTime();
                    ItemData.MessageID = entity.MessageID;
                    ItemData.Channel = entity.Channel;
                    ItemData.To = entity.To;
                    _records.Add(ItemData);

                }
            }

            return _records;
        }

        public List<MessageEntity> GetAllMessagesNoQuery(string userName, string macAndUser, string secretCode, string friendName)
        {
            List<MessageEntity> _records = new List<MessageEntity>();
            HttpResponseMessage response = _client.GetAsync($"Messages/AllMessagesNoQuery?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}&friendname={friendName}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                IEnumerable<MessageEntity> categories = JsonConvert.DeserializeObject<IEnumerable<MessageEntity>>(Data.Result);
                if (categories != null)
                {
                    foreach (var entity in categories)
                    {
                        var ItemData = new MessageEntity
                        {
                            from = entity.from,
                            Channel = entity.Channel,
                            To = entity.To
                        };
                        _records.Add(ItemData);

                    }
                }
            }

            return _records;
        }
    }
}

