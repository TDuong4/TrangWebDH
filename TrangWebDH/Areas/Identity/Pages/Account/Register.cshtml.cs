using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using TrangWebDH.Models;
using TrangWebDH.Data; // THÊM DÒNG NÀY

namespace TrangWebDH.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db; // THÊM ĐỂ TẠO SHOP

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db) // THÊM DbContext
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required][EmailAddress] public string Email { get; set; }
            [Required] public string FullName { get; set; }
            [Required][StringLength(100, MinimumLength = 6)][DataType(DataType.Password)] public string Password { get; set; }
            [DataType(DataType.Password)][Compare("Password")] public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var role = Request.Form["Role"].ToString();
            var shopName = Request.Form["ShopName"].ToString();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                Role = role
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);

                if (role == "ShopOwner" && !string.IsNullOrEmpty(shopName))
                {
                    _db.Shops.Add(new Shop
                    {
                        UserId = user.Id,
                        Name = shopName,
                        Address = "",
                        Phone = ""
                    });
                    await _db.SaveChangesAsync();
                }

                await _signInManager.SignInAsync(user, isPersistent: false);

                // REDIRECT THEO ROLE
                if (role == "ShopOwner")
                {
                    return RedirectToAction("Dashboard", "Shop"); // VÀO DASHBOARD SHOP
                }

                return RedirectToPage("/Index"); // KHÁCH HÀNG VÀO TRANG CHỦ
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return Page();
        }
    }
}