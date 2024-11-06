using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WiseWork.Models
{
    public class Room
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime GameStartTime { get; set; }
        public DateTime GameEndTime { get; set; }
        public string GamePin { get; set; }
        public Dictionary<string, string> Members { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, int> PlayerScores { get; set; } = new Dictionary<string, int>();

        public List<Player> Players { get; set; } = new List<Player>();
        public string CreatorId { get; set; }

        public int CurrentQuestionIndex { get; set; } = 0;
        public int TimeLeft { get; set; } = 30; 
        public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
        public string AddMember(string memberId, string playerName)
        {
            Members[memberId] = playerName;
            Players.Add(new Player { Id = memberId, Name = playerName, Score = 0 });
            return playerName;
        }

        public void UpdateMembers(Dictionary<string, string> newMembers)
        {
            Members.Clear();
            foreach (var member in newMembers)
            {
                Members.Add(member.Key, member.Value);
            }
        }

        public class Player
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Score { get; set; }

         //   public bool HasAnswered { get; set; }
        }
    }
}