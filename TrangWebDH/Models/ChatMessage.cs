namespace TrangWebDH.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime SentDate { get; set; }
        public string Sender { get; set; } // "Customer" or "Shop"

        // THÊM: Navigation
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; } // THÊM DÒNG NÀY

        public string ShopOwnerId { get; set; }
        public ApplicationUser ShopOwner { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
