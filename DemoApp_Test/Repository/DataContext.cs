using DemoApp_Test.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoApp_Test.Repository
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<Bill> Bill { get; set; }
        public DbSet<Client> Client { get; set; }
        public DbSet<Product_Bill> Product_Bill { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Account> Account { get; set; }
        public DbSet<Brand> Brand { get; set; }
        public DbSet<Sugar> Sugar { get; set; }
        public DbSet<Product_Sugar> Product_Sugar { get; set; }

        public DbSet<Ice> Ice { get; set; }

        public DbSet<Product_Ice> Product_Ice { get; set; }

        public DbSet<Size> Size { get; set; }
        public DbSet<Product_Size> Product_Size { get; set; }
        public DbSet<Bill_Voucher> Bill_Voucher { get; set; }
        public DbSet<DemoApp_Test.Models.TypeCoffee> TypeCoffee { get; set; }

        public DbSet<Shipping> Shipping { get; set; }
        public DbSet<Voucher> Voucher { get; set; }




    }
}
