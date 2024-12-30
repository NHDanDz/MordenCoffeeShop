using System.Collections.Concurrent;

namespace DemoApp_Test.Services
{
    public class ApplicationState : IApplicationState
    {
        private readonly ConcurrentDictionary<string, object> _state = new ConcurrentDictionary<string, object>();

        public object Get(string key) => _state.TryGetValue(key, out var value) ? value : null;
        public void Set(string key, object value) => _state.AddOrUpdate(key, value, (_, _) => value);
        public void Remove(string key) => _state.TryRemove(key, out _);
    }
}
