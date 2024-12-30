# Coffee Shop Management System

## Giới thiệu
Coffee Shop Management System là một nền tảng web quản lý và bán hàng trực tuyến cho các chuỗi cửa hàng cà phê như Highlands, Gongcha và nhiều thương hiệu khác. Hệ thống được xây dựng trên nền tảng .NET Core với kiến trúc MVC, mang đến trải nghiệm mượt mà cho cả người dùng cuối và quản trị viên.

## Tính năng chính

### Dành cho Khách hàng
- **Quản lý tài khoản**
  - Đăng ký, đăng nhập với xác thực JWT
  - Quản lý thông tin cá nhân và địa chỉ giao hàng
  - Lịch sử đơn hàng và trạng thái
  
- **Mua sắm trực tuyến**
  - Hiển thị sản phẩm theo danh mục và thương hiệu
  - Tìm kiếm nâng cao với bộ lọc (giá, loại, thương hiệu)
  - Giỏ hàng với tính năng cập nhật realtime
  - Đặt hàng và theo dõi trạng thái
  
- **Tương tác và đánh giá**
  - Đánh giá sản phẩm với hình ảnh
  - Bình luận và phản hồi
  - Yêu thích sản phẩm

### Dành cho Admin
- **Quản lý sản phẩm**
  - CRUD operations cho sản phẩm và danh mục
  - Quản lý hình ảnh với Azure Blob Storage
  - Cập nhật giá và tồn kho realtime
  
- **Quản lý đơn hàng**
  - Xử lý đơn hàng theo quy trình
  - Cập nhật trạng thái và thông báo cho khách hàng
  - Xuất báo cáo đơn hàng (PDF, Excel)
  
- **Quản lý người dùng**
  - Phân quyền với ASP.NET Core Identity
  - Quản lý roles và permissions
  - Theo dõi hoạt động người dùng

- **Quản lý marketing**
  - CRUD operations cho Voucher
  
- **Báo cáo và thống kê**
  - Dashboard với thống kê realtime
  - Báo cáo doanh thu theo thời gian
  - Phân tích hành vi người dùng

## Công nghệ sử dụng
- **Backend**
  - ASP.NET Core 7.0
  - Entity Framework Core
  - SQL Server 2019
  - Azure Services (Blob Storage, SendGrid)
  
- **Frontend**
  - Razor Views
  - Bootstrap 5
  - jQuery
  - AJAX for async operations
  
- **Authentication & Authorization**
  - ASP.NET Core Identity
  - JWT Authentication
  - Role-based Authorization
  
- **Development Tools**
  - Visual Studio 2022
  - Git for version control
  - Azure DevOps for CI/CD

## Cấu trúc Solution
```
CoffeeShop.sln
├── src/
│   ├── CoffeeShop.Web/                 # Main MVC Project
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── Models/
│   │   ├── wwwroot/
│   │   └── Areas/
│   │       └── Admin/
│   │
│   ├── CoffeeShop.Core/               # Core Business Logic
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   ├── CoffeeShop.Infrastructure/     # Data Access & External Services
│   │   ├── Data/
│   │   ├── Repositories/
│   │   └── Services/
│   │
│   └── CoffeeShop.Common/            # Shared Components
│       ├── DTOs/
│       ├── Helpers/
│       └── Extensions/
│
├── tests/
│   ├── CoffeeShop.UnitTests/
│   └── CoffeeShop.IntegrationTests/
│
└── docs/
    ├── api/
    └── architecture/
```

## Database Schema
```sql
-- Core Tables
CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Price DECIMAL(18,2) NOT NULL,
    CategoryId INT FOREIGN KEY REFERENCES Categories(Id),
    BrandId INT FOREIGN KEY REFERENCES Brands(Id),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2
);

CREATE TABLE Orders (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) FOREIGN KEY REFERENCES AspNetUsers(Id),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2
);

-- More tables defined in /database/schema.sql
```
  - Liên hệ với chúng tôi để có thể lấy database
## Yêu cầu hệ thống
- **Development Environment**
  - Windows 10/11 hoặc macOS
  - Visual Studio 2022 (có hỗ trợ .NET Core)
  - SQL Server 2019 hoặc cao hơn
  - .NET Core SDK 7.0
  - Git

- **Production Environment**
  - Windows Server 2019 hoặc Linux
  - IIS 10 hoặc Nginx
  - SQL Server 2019
  - Azure Account (cho Blob Storage)

## Hướng dẫn cài đặt

1. Clone repository
```bash
git clone https://github.com/NHDanDz/MordenCoffeeShop.git
cd MordenCoffeeShop
```

2. Restore dependencies
```bash
dotnet restore
```

3. Cập nhật database
```bash
cd src/MordenCoffeeShop.Web
dotnet ef database update
```

4. Cấu hình application settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CoffeeShop;Trusted_Connection=True;"
  },
  "AzureStorage": {
    "ConnectionString": "your-azure-storage-connection-string"
  },
  "JWT": {
    "Secret": "your-secret-key",
    "Issuer": "coffee-shop-api",
    "Audience": "coffee-shop-client"
  }
}
```

5. Chạy ứng dụng
```bash
dotnet run
```

## Quản lý Dependencies
### Backend Packages
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="7.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="7.0.0" />
<PackageReference Include="AutoMapper" Version="12.0.0" />
<PackageReference Include="Serilog" Version="2.12.0" />
```

### Frontend Libraries
- Bootstrap 5.2.3
- jQuery 3.6.0
- Font Awesome 6.2.0
- Select2 4.1.0
- DataTables 1.13.1

## API Endpoints

### Products API
```csharp
// GET: api/products
[HttpGet]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] ProductFilter filter)

// GET: api/products/{id}
[HttpGet("{id}")]
public async Task<ActionResult<ProductDto>> GetProduct(int id)

// POST: api/products
[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto productDto)
```

### Orders API
```csharp
// POST: api/orders
[HttpPost]
[Authorize]
public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto orderDto)

// GET: api/orders/{id}
[HttpGet("{id}")]
[Authorize]
public async Task<ActionResult<OrderDto>> GetOrder(int id)
```

## Tài khoản demo
- **Admin**
  - Email: admin 
  - Password: 123456

- **User**: Có thể tự tạo tài khoản

## Deployment
1. Publish ứng dụng
```bash
dotnet publish -c Release -o ./publish
```

2. Cấu hình IIS
- Tạo Application Pool với .NET Core runtime
- Cấu hình website với physical path tới thư mục publish
- Cấu hình SSL certificate

3. Cấu hình Nginx (Linux)
```nginx
server {
    listen 80;
    server_name your-domain.com;
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## Contributing
1. Fork repository
2. Tạo feature branch: `git checkout -b feature/AmazingFeature`
3. Commit changes: `git commit -m 'Add some AmazingFeature'`
4. Push to branch: `git push origin feature/AmazingFeature`
5. Tạo Pull Request

## License
Dự án được phân phối dưới giấy phép MIT. Xem `LICENSE` để biết thêm thông tin.

## Support
Nếu bạn gặp vấn đề, vui lòng tạo issue trong repository hoặc liên hệ:
- Email: nhdandz@gmail.com
 
