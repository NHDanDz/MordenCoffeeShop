using Microsoft.EntityFrameworkCore;

namespace DemoApp_Test.Repository
{
    public class SeedData
    {
        public static void SeedingData(DataContext _context)
        {
            _context.Database.Migrate();
        }
    }
}
