namespace TrangWebDH.Models
{
    public class Shop
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public ApplicationUser? User { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
