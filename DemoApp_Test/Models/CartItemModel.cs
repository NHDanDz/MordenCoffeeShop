using DemoApp_Test.Models;
public class CartItemModel
{
    public string Product_id { get; set; }
    public string ProductName { get; set; }
    public double Price { get; set; }
    public int Quantity { get; set; }
    public string Brand { get; set; }
    public string TypeCoffee { get; set; }
    public string Image { get; set; }

    // Thêm thuộc tính cho Size, Ice, Sugar
    public string SizeId { get; set; }
    public string SizeDetail { get; set; }
    public string IceId { get; set; }
    public string IceDetail { get; set; }
    public string SugarId { get; set; }
    public string SugarDetail { get; set; }

    // Constructor trống
    public CartItemModel() { }

    // Constructor từ Product với các tùy chọn mặc định
    public CartItemModel(Product product)
    {
        Product_id = product.Product_id;
        ProductName = product.ProductName;
        Price = product.Price;
        Quantity = 1;
        Brand = product.Brand?.BrandName ?? "default";
        TypeCoffee = product.TypeCoffee?.TypeName ?? "default";
        Image = product.Image ?? "default.jpg";

        // Giá trị mặc định cho Size, Ice, Sugar
        SizeId = "Size01";
        SizeDetail = "S";
        IceId = "Ice01";
        IceDetail = "50%";
        SugarId = "Sugar01";
        SugarDetail = "100%";
    }

    // Constructor đầy đủ với các tùy chọn
    public CartItemModel(Product product, string sizeId, string sizeDetail,
                        string iceId, string iceDetail,
                        string sugarId, string sugarDetail)
    {
        Product_id = product.Product_id;
        ProductName = product.ProductName;
        Price = product.Price;
        Quantity = 1;
        Brand = product.Brand?.BrandName ?? "default";
        TypeCoffee = product.TypeCoffee?.TypeName ?? "default";
        Image = product.Image ?? "default.jpg";

        SizeId = sizeId;
        SizeDetail = sizeDetail;
        IceId = iceId;
        IceDetail = iceDetail;
        SugarId = sugarId;
        SugarDetail = sugarDetail;
    }
}