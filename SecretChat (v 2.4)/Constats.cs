namespace SecretChat.Common
{
    public static partial class Constats
    {
        public const string MessagesTableName = "Messages";
        public const string OnlineUsersTableName = "OnlineUsers";
        public const string StoredUsersTableName = "StoredUsers";
        public const string VersionTableName = "Version";
        public const string ApiBaseAddress = devPublic;
        const string devPublic = "http://secretchatapi-dev.azurewebsites.net/api/";
        const string Public = "https://secretchatapi.azurewebsites.net/api/";
        const string local = "https://localhost:44361/api/";
    }
}
