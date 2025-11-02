using System.ComponentModel.DataAnnotations;

namespace TrangWebDH.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1000, int.MaxValue, ErrorMessage = "Giá phải lớn hơn 1000đ")]
        public decimal Price { get; set; }

        public int? Quantity { get; set; }

        public string? SizeOrWeight { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm")]
        public string ProductType { get; set; }

        public int ShopId { get; set; }
        public Shop Shop { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
