using System.ComponentModel.DataAnnotations;

namespace TrangWebDH.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } // ĐÚNG TÊN

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
