using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrangWebDH.Data;
using TrangWebDH.Models;

namespace TrangWebDH.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();
            return View(users);
        }

        // POST: Admin/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Không cho phép khóa admin
                if (user.Email == "admin@trangwebdh.com")
                {
                    TempData["Error"] = "Không thể khóa tài khoản Admin!";
                    return RedirectToAction("Users");
                }

                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["Success"] = $"Đã khóa người dùng {user.Email}";
            }
            return RedirectToAction("Users");
        }

        // POST: Admin/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = $"Đã mở khóa người dùng {user.Email}";
            }
            return RedirectToAction("Users");
        }

        // GET: Admin/Dashboard (Optional)
        public IActionResult Dashboard()
        {
            ViewBag.TotalUsers = _userManager.Users.Count();
            ViewBag.TotalShops = _context.Shops.Count();
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalOrders = _context.Orders.Count();

            return View();
        }
    }
}