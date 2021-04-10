using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretChat.Common
{
    public static partial class Constats
    {
        public const string MessagesTableName= "Messages";
        public const string OnlineUsersTableName = "OnlineUsers";
        public const string StoredUsersTableName = "StoredUsers";
        public const string VersionTableName = "Version";
        public const string ApiBaseAddress = "https://secretchatapi.azurewebsites.net/api/";
    }
}
