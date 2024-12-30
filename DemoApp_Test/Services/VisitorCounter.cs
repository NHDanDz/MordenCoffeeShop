using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DemoApp_Test.Services
{
    public static class VisitorCounter
    {
        private static readonly object _lock = new object();
        private const string ONLINE_USERS_KEY = "OnlineUsers";
        private const string TOTAL_VISITORS_KEY = "TotalVisitors";
        private const string VISITOR_SESSION_KEY = "HasBeenCounted";
        private const string LAST_ACTIVITY_TIMES_KEY = "LastActivityTimes";

        public static void IncrementVisitorCount(HttpContext context)
        {
            if (context == null) return;

            lock (_lock)
            {
                var application = context.RequestServices.GetRequiredService<IApplicationState>();
                var sessionId = context.Session.Id;

                if (context.Session.GetString(VISITOR_SESSION_KEY) == null)
                {
                    context.Session.SetString(VISITOR_SESSION_KEY, "true");
                    UpdateLastActivity(context);

                    var totalVisitors = (int)(application.Get(TOTAL_VISITORS_KEY) ?? 0);
                    application.Set(TOTAL_VISITORS_KEY, totalVisitors + 1);

                    var onlineUsers = (HashSet<string>)application.Get(ONLINE_USERS_KEY) ?? new HashSet<string>();
                    if (!onlineUsers.Contains(sessionId))
                    {
                        onlineUsers.Add(sessionId);
                        application.Set(ONLINE_USERS_KEY, onlineUsers);
                    }
                }
                else
                {
                    UpdateLastActivity(context);
                }
            }
        }

        public static void UpdateLastActivity(HttpContext context)
        {
            if (context?.Session == null) return;

            lock (_lock)
            {
                var application = context.RequestServices.GetRequiredService<IApplicationState>();
                var lastActivityDict = (Dictionary<string, DateTime>)application.Get(LAST_ACTIVITY_TIMES_KEY)
                    ?? new Dictionary<string, DateTime>();

                lastActivityDict[context.Session.Id] = DateTime.UtcNow;
                application.Set(LAST_ACTIVITY_TIMES_KEY, lastActivityDict);
            }
        }

        public static void RemoveOnlineUser(HttpContext context)
        {
            if (context?.Session == null) return;

            lock (_lock)
            {
                var application = context.RequestServices.GetRequiredService<IApplicationState>();
                var sessionId = context.Session.Id;

                var onlineUsers = (HashSet<string>)application.Get(ONLINE_USERS_KEY);
                var lastActivityDict = (Dictionary<string, DateTime>)application.Get(LAST_ACTIVITY_TIMES_KEY);

                if (onlineUsers?.Contains(sessionId) == true)
                {
                    onlineUsers.Remove(sessionId);
                    application.Set(ONLINE_USERS_KEY, onlineUsers);
                }

                if (lastActivityDict?.ContainsKey(sessionId) == true)
                {
                    lastActivityDict.Remove(sessionId);
                    application.Set(LAST_ACTIVITY_TIMES_KEY, lastActivityDict);
                }
            }
        }

        public static (int totalVisitors, int onlineUsers) GetVisitorStats(HttpContext context)
        {
            if (context == null) return (0, 0);

            var application = context.RequestServices.GetRequiredService<IApplicationState>();
            var totalVisitors = (int)(application.Get(TOTAL_VISITORS_KEY) ?? 0);
            var onlineUsers = ((HashSet<string>)application.Get(ONLINE_USERS_KEY))?.Count ?? 0;

            return (totalVisitors, onlineUsers);
        }
        public static void DecrementOnlineUser(HttpContext context)
        {
            RemoveOnlineUser(context); // Sử dụng lại logic từ RemoveOnlineUser
        }
    }
}