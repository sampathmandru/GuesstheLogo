using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using WiseWork.Services;
using System.Linq;

namespace WiseWork.Hubs
{
    public class RoomHub : Hub
    {
        private readonly RoomService _roomService;
        private string winnerName = null;
        private static Dictionary<string, (int TimeLeft, DateTime LastUpdate)> roomTimers = new Dictionary<string, (int, DateTime)>();

        public RoomHub(RoomService roomService)
        {
            _roomService = roomService;
        }

        public async Task JoinRoom(string gamePin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            var gameEndTime = _roomService.GetGameEndTime(gamePin);
            await Clients.Caller.SendAsync("SyncGameEndTime", gameEndTime);
        }
        public async Task GetConnectionId()
        {
            await Clients.Caller.SendAsync("ReceiveConnectionId", Context.ConnectionId);
        }

        public async Task LeaveRoom(string gamePin, string playerName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gamePin);
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                string playerIdToRemove = room.Members.FirstOrDefault(x => x.Value == playerName).Key;
                if (playerIdToRemove != null)
                {
                    if (playerIdToRemove == room.CreatorId)
                    {
                        // Creator is leaving, delete the room
                        _roomService.DeleteRoom(gamePin);
                        await Clients.Group(gamePin).SendAsync("RoomDeleted");
                    }
                    else
                    {
                        // Regular member leaving
                        _roomService.RemoveMember(gamePin, playerName);

                        // Update the room object after removing the member
                        room = _roomService.GetRoom(gamePin);

                        // Send updated members list to all clients in the room
                        await SendMembersUpdate(gamePin, room.Members);
                    }
                }
            }
        }
        public async Task AnnounceWinner(string gamePin, string playerName)
        {
            await Clients.Group(gamePin).SendAsync("WinnerAnnounced", playerName);
        }
        public async Task SendMembersUpdate(string gamePin, Dictionary<string, string> members)
        {
            await Clients.Group(gamePin).SendAsync("ReceiveMembersUpdate", members);
        }

        public async Task StartGame(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("StartGame", gamePin);
        }

        public async Task RoomDeleted(string gamePin)
        {
            await Clients.Group(gamePin).SendAsync("RoomDeleted");
        }
        public async Task UpdatePlayerName(string gamePin, string playerName)
        {
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                room.Members[Context.ConnectionId] = playerName;
                await Clients.Group(gamePin).SendAsync("ReceiveMembersUpdate", room.Members);
            }
        }

        public async Task UpdatePlayerScores(string gamePin)
    {
        var room = _roomService.GetRoom(gamePin);
        if (room != null)
        {
            var playersWithScores = room.Members.ToDictionary(
                kvp => kvp.Value,
                kvp => room.PlayerScores.ContainsKey(kvp.Key) ? room.PlayerScores[kvp.Key] : 0
            );
            await Clients.Group(gamePin).SendAsync("PlayerScoresUpdated", playersWithScores);
        }
    }

        public async Task EndGame(string gamePin)
        {
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                await Clients.Group(gamePin).SendAsync("GameOver", new List<string> { room.Members.FirstOrDefault(x => x.Value == winnerName).Value });
                _roomService.ResetGame(gamePin);
            }
        }

        public async Task SyncGameEndTime(string gamePin)
        {
            var room = _roomService.GetRoom(gamePin);
            if (room != null)
            {
                await Clients.Group(gamePin).SendAsync("SyncGameEndTime", room.GameEndTime);
            }
        }

        public async Task UpdateTimerState(string gamePin, int timeLeft)
        {
            if (!roomTimers.ContainsKey(gamePin) ||
                (DateTime.UtcNow - roomTimers[gamePin].LastUpdate).TotalSeconds >= 1)
            {
                roomTimers[gamePin] = (timeLeft, DateTime.UtcNow);
                await Clients.Group(gamePin).SendAsync("SyncTimer", timeLeft);
            }
        }

        public async Task NextQuestion(string gamePin, int questionIndex)
        {
            _roomService.UpdateCurrentQuestionIndex(gamePin, questionIndex);
            await Clients.Group(gamePin).SendAsync("NextQuestion", questionIndex);
        }
        //public async Task SetGameEndTime(string gamePin, DateTime endTime)
        //{
        //    _roomService.UpdateRoom(gamePin, endTime);
        //    await Clients.Group(gamePin).SendAsync("SyncGameEndTime", endTime);
        //}
        public async Task<DateTime> GetGameStartTime(string gamePin)
        {
            return _roomService.GetOrSetGameStartTime(gamePin);
        }


    }
}





