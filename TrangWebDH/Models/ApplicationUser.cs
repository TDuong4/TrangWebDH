using Microsoft.AspNetCore.Identity;

namespace TrangWebDH.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Customer";
        public string? ShopName { get; set; }
        public string? ShopAddress { get; set; }
        public string? ShopPhone { get; set; }
    }
}
