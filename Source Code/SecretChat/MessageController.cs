using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TableEntity = Microsoft.Azure.Cosmos.Table.TableEntity;

namespace SecretChat
{

    public class MessagesModel
    {

        public List<MessageEntity> Messages { get; set; } = new List<MessageEntity>();

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
            string dorl = System.IO.File.ReadAllText(path + "\\secretchat\\darkorlight.txt");
            if (dorl == "light")
            {
                this.TextColor = "Black";
                this.TextColor2 = "Black";
            }
            else
            {
                this.TextColor = "White";
                this.TextColor2 = "White";
            }
            UnderlinedOrNot = "None";
            MessageOrInfo = "Visible";
            LeftOrCenter = "Left";
        }

        public string From { get; set; }

        public string Message { get; set; }

        public new DateTime Timestamp { get; set; }

        public string MessageID { get; set; }

        public string TextColor { get; set; }

        public string TextColor2 { get; set; }

        public string UnderlinedOrNot { get; set; }

        public string MessageOrInfo { get; set; }

        public string LeftOrCenter { get; set; }

        public StoredUserEntity User { get; set; }

        public StoredUserEntity AbsentUser { get; set; }


        public string endMessage
        {
            get
            {
                //return Timestamp + "       " + From + ":        " + Message;
                return $"{Timestamp.ToShortDateString()}  {Timestamp.ToShortTimeString()}      {From}:  ";
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
        private HttpClient _client;

        public  MessageController()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(Common.Constats.ApiBaseAddress);
        }

        public Boolean InsertMessage(string messageID, string From, string message, string userName, string macAndUser, string secretCode)
        {
            List<MessageEntity> _records = new List<MessageEntity>();
            
            MessageEntity Message = new MessageEntity
            {
                Message = message,
                MessageID = messageID,
                From = From
            };
            
            HttpContent content = new StringContent(JsonConvert.SerializeObject(Message));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = _client.PostAsync($"Messages/Insert?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<bool>(Data.Result);
               
                return res;

            }
            else
            {
                return false;
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
                    var ItemData = new MessageEntity
                    {
                        From = entity.From,
                        Message = entity.Message,
                        Timestamp = entity.Timestamp.ToLocalTime(),
                        MessageID = entity.MessageID,
                    };
                    _records.Add(ItemData);
                }
            }
            return _records;
            //   List<MessageEntity> _records = new List<MessageEntity>();
            //var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(Common.Constats.AzureStorageConnectionString);
            //var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            //var _linkTable = tableClient.GetTableReference(Common.Constats.MessagesTableName);
            //TableQuery<MessageEntity> query = new TableQuery<MessageEntity>();
            //query.Where($"Timestamp gt datetime'{DateTime.UtcNow.AddMinutes(-2):yyyy-MM-ddTHH:mm:ss}'");
            //TableContinuationToken token = null;

            //do
            //{
            //    TableQuerySegment<MessageEntity> resultSegment = Task.Run(async () => await _linkTable.ExecuteQuerySegmentedAsync(query, token)).Result;
            //    token = resultSegment.ContinuationToken;

            //    foreach (var entity in resultSegment.Results)
            //    {
            //        MessageEntity _summary = new MessageEntity
            //        {
            //            MessageID = entity.MessageID,
            //            From = entity.From,
            //            Message = entity.Message,
            //            Timestamp = ((TableEntity)entity).Timestamp.UtcDateTime.ToLocalTime(),
            //        };

            //        _records.Add(_summary);
            //    }
            //} while (token != null);


            //return _records;
        }

        public List<MessageEntity> GetAllMessages(string userName, string macAndUser, string secretCode)
        {
            List<MessageEntity> _records = new List<MessageEntity>();
            HttpResponseMessage response =  _client.GetAsync($"Messages/AllMessages?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data =  response.Content.ReadAsStringAsync();
                IEnumerable<MessageEntity> categories = JsonConvert.DeserializeObject<IEnumerable<MessageEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    var ItemData = new MessageEntity
                    {
                        From = entity.From,
                        Message = entity.Message,
                        Timestamp = entity.Timestamp.ToLocalTime(),
                        MessageID = entity.MessageID
                    };
                    _records.Add(ItemData);
                }
            }

            return _records;
        }
    }
}

