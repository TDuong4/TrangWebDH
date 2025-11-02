namespace TrangWebDH.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerId { get; set; } = "";
        public int ShopId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Pending";
        public string? TrackingCode { get; set; }
        public string? ShippingCompany { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public ApplicationUser? Customer { get; set; }
        public Shop? Shop { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
