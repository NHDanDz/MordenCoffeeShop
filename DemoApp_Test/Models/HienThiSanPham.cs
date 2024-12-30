using System.ComponentModel.DataAnnotations;
using static DemoApp_Test.Controllers.HomeController;

namespace DemoApp_Test.Models
{
    public class HienThiSanPham
    {
        [Key]
        public string Ma_Sanpham { get; set; }

        [Display(Name = "Tên sản phẩm")]
        public string Ten_Sanpham { get; set; }

        [Display(Name = "Tên thương hiệu")]
        public string Ten_Nhasanxuat { get; set; }

        [Display(Name = "Loại")]
        public string Ten_loai { get; set; }

        [Display(Name = "Giá gốc")]
        public double Giagoc { get; set; }

        [Display(Name = "Đánh giá")]
        public double? Rating { get; set; }

        [Display(Name = "Số lượng đánh giá")]
        public int? ReviewCount { get; set; }

        [Display(Name = "Giảm giá")]
        public int? Discount { get; set; }

        [Display(Name = "Hình ảnh")]
        public string Link1 { get; set; }

        public DateTime Date { get; set; }

        // Thêm các trường để hỗ trợ filter
        public string Brand_id { get; set; }
        public string Type_id { get; set; }

        // Có thể thêm các trường bổ sung nếu cần
        [Display(Name = "Các size")]
        public string All_Size { get; set; }

        [Display(Name = "Số lượng")]
        public int So_Luong { get; set; }
        public int SoldQuantity { get; set; }  // Add this property
        public bool status { get; set; }

        public string? Description { get; set; }
        // Thêm thuộc tính tính toán nếu cần
        public double GiaSauGiam
        {
            get
            {
                if (Discount.HasValue)
                    return Giagoc * (100 - Discount.Value) / 100;
                return Giagoc;
            }
        }
        public List<FeedbackViewModel> Feedbacks { get; set; }

    }
}