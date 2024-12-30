using Microsoft.AspNetCore.Mvc;

namespace DemoApp_Test.Services
{
    public interface IApplicationState
    {
        object Get(string key);
        void Set(string key, object value);
        void Remove(string key);
    }
}
