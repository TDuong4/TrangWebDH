using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrangWebDH.Data;
using TrangWebDH.Models;

namespace TrangWebDH.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cart - Xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)
                .Include(c => c.Product.Shop)
                .Where(c => c.CustomerId == userId)
                .ToListAsync();

            var total = cartItems.Sum(c => c.Product.Price * c.Quantity);
            ViewBag.Total = total;

            return View(cartItems);
        }

        // POST: Cart/Add - Thêm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra sản phẩm có tồn tại
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound("Sản phẩm không tồn tại");

            // Kiểm tra đã có trong giỏ chưa
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.CustomerId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                // Cập nhật số lượng
                existingItem.Quantity += quantity;
                _context.Update(existingItem);
            }
            else
            {
                // Thêm mới
                var cartItem = new CartItem
                {
                    CustomerId = userId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm vào giỏ hàng!";

            return RedirectToAction("Detail", "Product", new { id = productId });
        }

        // POST: Cart/Update - Cập nhật số lượng
        [HttpPost]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == userId);

            if (cartItem == null)
                return NotFound();

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
                _context.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST: Cart/Remove - Xóa khỏi giỏ
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa khỏi giỏ hàng!";
            }

            return RedirectToAction("Index");
        }

        // POST: Cart/Checkout - Đặt hàng
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Shop)
                .Where(c => c.CustomerId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index");
            }

            // Nhóm theo shop
            var ordersByShop = cartItems.GroupBy(c => c.Product.ShopId);

            foreach (var shopGroup in ordersByShop)
            {
                var order = new Order
                {
                    CustomerId = userId,
                    ShopId = shopGroup.Key,
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    TotalPrice = shopGroup.Sum(c => c.Product.Price * c.Quantity)
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Thêm OrderItems
                foreach (var item in shopGroup)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }
            }

            // Xóa giỏ hàng
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đặt hàng thành công!";
            return RedirectToAction("MyOrders");
        }

        // GET: Cart/MyOrders - Đơn hàng đã mua
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Include(o => o.Shop)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Where(o => o.CustomerId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}