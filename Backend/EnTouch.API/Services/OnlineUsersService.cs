using System.Collections.Concurrent;

namespace EnTouch.API.Services
{
    public class OnlineUsersService
    {
        private readonly ConcurrentDictionary<string, string> _onlineUsers = new();

        public void UserConnected(string userId, string connectionId)
        {
            _onlineUsers[userId] = connectionId;
        }

        public void UserDisconnected(string userId)
        {
            _onlineUsers.TryRemove(userId, out _);
        }

        public string GetConnectionId(string userId)
        {
            _onlineUsers.TryGetValue(userId, out var connectionId);
            return connectionId;
        }

        public List<string> GetOnlineUsers()
        {
            return _onlineUsers.Keys.ToList();
        }
    }
}