using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SecretChat
{
    public class VersionModel
    {

        public List<VersionEntity> Version { get; set; } = new List<VersionEntity>();
    }

    public class VersionEntity : TableEntity
    {
        // Set up Partition and Row Key information
        public VersionEntity(string versionID)
        {
            this.PartitionKey = versionID;
        }


        public VersionEntity()
        {

        }

        public string VersionNumber { get; set; }


    }

    public class VersionController
    {
        private HttpClient _client;

        public VersionController()
        {
            _client = new HttpClient();
            _client.BaseAddress = new System.Uri(Common.Constats.ApiBaseAddress);
        }

        public List<VersionEntity> GetVersion()
        {
            List<VersionEntity> _records = new List<VersionEntity>();
            HttpResponseMessage response = _client.GetAsync("Version/VersionNumber").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                IEnumerable<VersionEntity> categories = JsonConvert.DeserializeObject<IEnumerable<VersionEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    var ItemData = new VersionEntity
                    {
                        VersionNumber = entity.VersionNumber
                    };
                    _records.Add(ItemData);
                }
            }

            return _records;
        }

        public List<VersionEntity> ValidInformationTest(string userName, string macAndUser, string secretCode)
        {
            List<VersionEntity> _records = new List<VersionEntity>();
            HttpResponseMessage response = _client.GetAsync($"Version/ValidInformationTest?userName={userName}&macAndUser={macAndUser}&secretCode={secretCode}").Result;
            if (response.IsSuccessStatusCode)
            {
                var Data = response.Content.ReadAsStringAsync();
                IEnumerable<VersionEntity> categories = JsonConvert.DeserializeObject<IEnumerable<VersionEntity>>(Data.Result);
                foreach (var entity in categories)
                {
                    var ItemData = new VersionEntity
                    {
                        VersionNumber = entity.VersionNumber
                    };
                    _records.Add(ItemData);
                }
            }

            return _records;
        }
    }
}
