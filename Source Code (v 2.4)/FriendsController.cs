using System.Collections.Generic;

namespace SecretChat
{
    public class FriendsModel
    {
        public List<MessageEntity> Friends { get; set; } = new List<MessageEntity>();

        public List<MessageEntity> Messages { get; set; }
        //public List<List<MessageEntity>> AllMessages { get; set; }
    }
}
