using DemoApp_Test.Models;
using Newtonsoft.Json;

public interface ICartService
{
    Task<List<Product>> GetCartItemsAsync(ISession session);
    Task AddToCartAsync(ISession session, string productId);
}

public class CartService : ICartService
{
    private const string CartSessionKey = "CartItems";

    public async Task<List<Product>> GetCartItemsAsync(ISession session)
    {
        var cartJson = session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(cartJson))
        {
            return new List<Product>();
        }

        return JsonConvert.DeserializeObject<List<Product>>(cartJson);
    }

    public async Task AddToCartAsync(ISession session, string productId)
    {
        var cartItems = await GetCartItemsAsync(session);

        var existingItem = cartItems.Find(item => item.Product_id == productId);
        cartItems.Add(new Product
        {
            Product_id = productId,
            ProductName = "Sản phẩm mẫu", // Tùy chỉnh tên sản phẩm  
        });

        session.SetString(CartSessionKey, JsonConvert.SerializeObject(cartItems));
    }
}
