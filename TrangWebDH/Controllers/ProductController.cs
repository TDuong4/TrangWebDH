using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering; 
using System.Security.Claims;
using TrangWebDH.Data;
using TrangWebDH.Models;

namespace TrangWebDH.Controllers
{
    [Authorize(Roles = "ShopOwner")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Create
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null) return NotFound("Bạn chưa có shop.");

            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            List<IFormFile> imageFile,
            bool hasQuantity = true,
            bool hasSizeWeight = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null) return NotFound();

            product.ShopId = shop.Id;

            if (!hasQuantity) product.Quantity = null;
            if (!hasSizeWeight) product.SizeOrWeight = null;

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // LƯU ẢNH
            if (imageFile != null && imageFile.Any(f => f.Length > 0))
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                foreach (var file in imageFile.Where(f => f.Length > 0))
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImagePath = "/uploads/" + fileName
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Dashboard", "Shop");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            Product product,
            List<IFormFile> imageFile,
            bool hasQuantity = true,
            bool hasSizeWeight = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (shop == null) return NotFound("Bạn chưa có shop.");

            product.ShopId = shop.Id;

            if (!hasQuantity) product.Quantity = null;
            if (!hasSizeWeight) product.SizeOrWeight = null;

            if (!ModelState.IsValid)
            {
                ViewBag.ProductTypes = GetProductTypes(); // Đảm bảo ViewBag được khởi tạo
                return View(product);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Xử lý upload ảnh
            if (imageFile != null && imageFile.Any(f => f.Length > 0))
            {
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                foreach (var file in imageFile)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImagePath = $"/uploads/{fileName}"
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Sản phẩm đã được thêm thành công!";
            return RedirectToAction("Products");
        }

        // CÁC ACTION CŨ (GIỮ NGUYÊN)
        [AllowAnonymous]
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .Include(p => p.Shop)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        public IActionResult Chat(int productId)
        {
            var messages = _context.ChatMessages
                .Where(m => m.ProductId == productId)
                .OrderBy(m => m.SentDate)
                .ToList();

            return PartialView("_Chat", messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int productId, string message)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _context.Products
                .Include(p => p.Shop)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Shop == null)
                return BadRequest("Sản phẩm hoặc cửa hàng không tồn tại.");

            var chat = new ChatMessage
            {
                CustomerId = userId,
                ShopOwnerId = product.Shop.UserId,
                ProductId = productId,
                Message = message,
                Sender = "Customer",
                SentDate = DateTime.Now
            };

            _context.ChatMessages.Add(chat);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private List<SelectListItem> GetProductTypes()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "DienTu", Text = "Điện tử" },
                new SelectListItem { Value = "ThoiTrang", Text = "Thời trang" },
                new SelectListItem { Value = "NoiThat", Text = "Nội thất" },
                new SelectListItem { Value = "Sach", Text = "Sách" }
            };
        }
    }
}