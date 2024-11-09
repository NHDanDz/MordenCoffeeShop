using DemoApp_Test.Models; // Thêm dòng này để sử dụng SANPHAM

namespace DemoApp_Test.Services
{
    public interface ICartService
    {
        Task<List<Product>> GetCartItemsAsync(ISession session);
        Task AddToCartAsync(ISession session, Product product);
        Task<List<Product_Bill>> GetPhieuSPsAsync(ISession session);
        Task AddPhieuSPAsync(ISession session, Product_Bill phieu);
    }

}