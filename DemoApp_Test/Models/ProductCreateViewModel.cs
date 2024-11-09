using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace DemoApp_Test.Models
{
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        [StringLength(10, ErrorMessage = "Mã sản phẩm không được vượt quá 10 ký tự")]
        public string Product_id { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public float Price { get; set; } // Khớp với kiểu FLOAT trong bảng

        public string? BrandId { get; set; } // Thay đổi thành string vì CHAR(10)

        [Range(0, 5, ErrorMessage = "Đánh giá phải từ 0 đến 5")]
        public float? Rating { get; set; }

        public int? ReviewCount { get; set; }

        [Range(0, 100, ErrorMessage = "Giảm giá phải từ 0 đến 100%")]
        public int? Discount { get; set; } // INT

        public IFormFile? ImageFile { get; set; } // Dùng để upload ảnh

        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        [StringLength(10, ErrorMessage = "Mã sản phẩm không được vượt quá 10 ký tự")]
        public string Type_id { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? Date { get; set; } // DATE trong bảng
    }
}
