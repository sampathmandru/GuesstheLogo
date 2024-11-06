using WiseWork.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Linq;

namespace WiseWork.Services
{
    public class RoomService
    {
        private readonly IMongoCollection<Room> _rooms;
        private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int PinLength = 6;

        public RoomService(MongoDBService mongoDBService)
        {
            _rooms = mongoDBService.GetCollection<Room>("Rooms");
        }

        public (Room, string) CreateRoom(string playerId, string playerName)
        {
            var room = new Room
            {
                GamePin = GenerateGamePin(),
                Members = new Dictionary<string, string> { { playerId, playerName } },
                CreatorId = playerId,
                GameStartTime = DateTime.UtcNow,
                GameEndTime = DateTime.UtcNow.AddMinutes(15)
            };
            _rooms.InsertOne(room);
            return (room, playerName);
        }

        public (bool, string) JoinRoom(string gamePin, string playerId, string playerName)
        {
            var room = _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
            if (room == null)
                return (false, null);
            room.Members[playerId] = playerName;
            _rooms.ReplaceOne(r => r.GamePin == gamePin, room);
            return (true, playerName);
        }

        public Room GetRoom(string gamePin)
        {
            return _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
        }

        public void DeleteRoom(string gamePin)
        {
            _rooms.DeleteOne(r => r.GamePin == gamePin);
        }

        public void RemoveMember(string gamePin, string playerName)
        {
            var room = _rooms.Find(r => r.GamePin == gamePin).FirstOrDefault();
            if (room != null)
            {
                var memberIdToRemove = room.Members.FirstOrDefault(m => m.Value == playerName).Key;
                if (memberIdToRemove != null)
                {
                    room.Members.Remove(memberIdToRemove);
                    _rooms.ReplaceOne(r => r.GamePin == gamePin, room);
                }
            }
        }



        private string GenerateGamePin()
        {
            var random = new Random();
            var pinBuilder = new StringBuilder(PinLength);
            for (int i = 0; i < PinLength; i++)
            {
                pinBuilder.Append(AllowedCharacters[random.Next(AllowedCharacters.Length)]);
            }
            return pinBuilder.ToString();
        }

        public void UpdatePlayerScore(string gamePin, string playerId, int score)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update
                .Set($"PlayerScores.{playerId}", score)
                .Set(r => r.Players.FirstMatchingElement().Score, score);

            _rooms.UpdateOne(filter & Builders<Room>.Filter.ElemMatch(r => r.Players, p => p.Id == playerId), update);
        }



        public List<Room.Player> GetTopPlayers(string gamePin, int count)
        {
            var room = GetRoom(gamePin);
            return room?.Players.OrderByDescending(p => p.Score).Take(count).ToList() ?? new List<Room.Player>();
        }



        // Add this method to update the current question index
        public void UpdateCurrentQuestionIndex(string gamePin, int index)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update
                .Set(r => r.CurrentQuestionIndex, index)
                .Set(r => r.TimeLeft, 30) // Reset timer for new question
                .Set(r => r.LastUpdateTime, DateTime.UtcNow);
            _rooms.UpdateOne(filter, update);
        }

        public void UpdateTimerState(string gamePin, int timeLeft)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update
                .Set(r => r.TimeLeft, timeLeft)
                .Set(r => r.LastUpdateTime, DateTime.UtcNow);
            _rooms.UpdateOne(filter, update);
        }

        

        public Dictionary<string, int> GetFinalLeaderboard(string gamePin)
        {
            var room = GetRoom(gamePin);
            if (room != null)
            {
                return room.Members.ToDictionary(
                    m => m.Value,
                    m => room.PlayerScores.ContainsKey(m.Key) ? room.PlayerScores[m.Key] : 0
                );
            }
            return new Dictionary<string, int>();
        }
        public void UpdateRoom(string gamePin, DateTime gameEndTime)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update
                .Set(r => r.GameEndTime, gameEndTime);
            _rooms.UpdateOne(filter, update);
        }

        public DateTime GetGameEndTime(string gamePin)
        {
            var room = GetRoom(gamePin);
            return room?.GameEndTime ?? DateTime.UtcNow;
        }
        public DateTime GetOrSetGameStartTime(string gamePin)
        {
            var room = GetRoom(gamePin);
            if (room.GameStartTime == default)
            {
                room.GameStartTime = DateTime.UtcNow;
                var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
                var update = Builders<Room>.Update.Set(r => r.GameStartTime, room.GameStartTime);
                _rooms.UpdateOne(filter, update);
            }
            return room.GameStartTime;
        }
        public void ResetGame(string gamePin)
        {
            var filter = Builders<Room>.Filter.Eq(r => r.GamePin, gamePin);
            var update = Builders<Room>.Update
                .Set(r => r.PlayerScores, new Dictionary<string, int>())
                .Set(r => r.CurrentQuestionIndex, 0);
            _rooms.UpdateOne(filter, update);
        }


    }
}





