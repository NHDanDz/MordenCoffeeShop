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
        public DbSet<Feedback> Feedback { get; set; }

        public DbSet<Ice> Ice { get; set; }

        public DbSet<Product_Ice> Product_Ice { get; set; }

        public DbSet<Size> Size { get; set; }
        public DbSet<Product_Size> Product_Size { get; set; }
        public DbSet<Bill_Voucher> Bill_Voucher { get; set; }
        public DbSet<DemoApp_Test.Models.TypeCoffee> TypeCoffee { get; set; }
        public DbSet<AdminActivity> AdminActivity { get; set; }

        public DbSet<Shipping> Shipping { get; set; }
        public DbSet<Voucher> Voucher { get; set; }
        public DbSet<ShoppingCart> ShoppingCart { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItem { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product_Bill>(entity =>
            {
                // Khóa chính gồm 5 trường
                entity.HasKey(e => new {
                    e.Product_id,
                    e.Bill_id,
                    e.Size_id,
                    e.Ice_id,
                    e.Sugar_id
                });

                // Quan hệ với Bill
                entity.HasOne(pb => pb.Bill)
                    .WithMany(b => b.Product_Bill)
                    .HasForeignKey(pb => pb.Bill_id);

                // Quan hệ với Product
                entity.HasOne(pb => pb.Product)
                    .WithMany(p => p.Product_Bill)
                    .HasForeignKey(pb => pb.Product_id);

                // Quan hệ với Ice
                entity.HasOne(pb => pb.Ice)
                    .WithMany()
                    .HasForeignKey(pb => pb.Ice_id);  // Bỏ IsRequired(false) vì đã là required

                // Quan hệ với Sugar
                entity.HasOne(pb => pb.Sugar)
                    .WithMany()
                    .HasForeignKey(pb => pb.Sugar_id);  // Bỏ IsRequired(false) vì đã là required

                // Quan hệ với Size
                entity.HasOne(pb => pb.Size)
                    .WithMany()
                    .HasForeignKey(pb => pb.Size_id);  // Bỏ IsRequired(false) vì đã là required
            });
        }

    }
}
