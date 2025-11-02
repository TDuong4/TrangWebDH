using System.ComponentModel.DataAnnotations;

namespace TrangWebDH.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}