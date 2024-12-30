using Microsoft.AspNetCore.Session;

namespace DemoApp_Test.Services.BackgroundServices
{
    public class SessionCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(20); // Thời gian timeout cho session

        public SessionCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var applicationState = scope.ServiceProvider.GetRequiredService<IApplicationState>();
                    var onlineUsers = (HashSet<string>)applicationState.Get("OnlineUsers");

                    if (onlineUsers != null)
                    {
                        var currentTime = DateTime.UtcNow;
                        var lastActivityDict = (Dictionary<string, DateTime>)applicationState.Get("LastActivityTimes")
                            ?? new Dictionary<string, DateTime>();

                        // Lọc các session hết hạn
                        var expiredSessions = onlineUsers.Where(sessionId =>
                            IsSessionExpired(sessionId, lastActivityDict, currentTime)).ToList();

                        if (expiredSessions.Any())
                        {
                            foreach (var sessionId in expiredSessions)
                            {
                                onlineUsers.Remove(sessionId);
                                lastActivityDict.Remove(sessionId);
                            }

                            applicationState.Set("OnlineUsers", onlineUsers);
                            applicationState.Set("LastActivityTimes", lastActivityDict);
                        }
                    }
                }
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }

        private bool IsSessionExpired(string sessionId, Dictionary<string, DateTime> lastActivityDict, DateTime currentTime)
        {
            if (lastActivityDict.TryGetValue(sessionId, out DateTime lastActivity))
            {
                return currentTime - lastActivity > _sessionTimeout;
            }
            return true; // Nếu không tìm thấy lastActivity, coi như session đã hết hạn
        }
    }
}
