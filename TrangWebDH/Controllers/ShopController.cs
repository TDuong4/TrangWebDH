using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrangWebDH.Data;
using TrangWebDH.Models;

[Authorize(Roles = "ShopOwner")]
public class ShopController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShopController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _env = env;
        _userManager = userManager;
    }

    // ================================
    // DASHBOARD
    // ================================
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var shop = await _context.Shops
            .Include(s => s.Products)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null)
        {
            shop = new Shop
            {
                UserId = userId,
                Name = "Shop của " + User.Identity?.Name,
                Address = "",
                Phone = ""
            };
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
        }

        var ordersCount = await _context.Orders.CountAsync(o => o.ShopId == shop.Id);
        var revenue = await _context.Orders.Where(o => o.ShopId == shop.Id).SumAsync(o => o.TotalPrice);
        var stock = await _context.Products.Where(p => p.ShopId == shop.Id).SumAsync(p => p.Quantity ?? 0);

        ViewBag.OrdersCount = ordersCount;
        ViewBag.Revenue = revenue;
        ViewBag.Stock = stock;
        ViewBag.ProductCount = shop.Products.Count;
        ViewBag.Shop = shop;

        return View(shop);
    }

    // ================================
    // ADD PRODUCT - GET
    // ================================
    [HttpGet]
    public async Task<IActionResult> AddProduct()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null) return NotFound("Bạn chưa có shop!");

        ViewBag.ProductTypes = GetProductTypes();
        return View(new Product());
    }

    // ================================
    // ADD PRODUCT - POST
    // ================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(
    [Bind("Name,Description,Price,Quantity,SizeOrWeight,ProductType")] Product product,
    IFormFile[]? imageFile,
    bool hasQuantity = false,
    bool hasSizeWeight = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null) return NotFound("Bạn chưa có shop!");

        // ✅ XÓA validation lỗi cho Shop (vì không submit từ form)
        ModelState.Remove("Shop");
        ModelState.Remove("Reviews");
        ModelState.Remove("Images");

        if (ModelState.IsValid)
        {
            product.ShopId = shop.Id;
            product.Quantity = hasQuantity && product.Quantity > 0 ? product.Quantity : null;
            product.SizeOrWeight = hasSizeWeight ? product.SizeOrWeight : null;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Xử lý ảnh
            if (imageFile != null && imageFile.Length > 0 && imageFile.Any(f => f.Length > 0))
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "uploads/products");
                Directory.CreateDirectory(uploadPath);

                foreach (var file in imageFile)
                {
                    if (file.Length > 0)
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
                            ImagePath = "/uploads/products/" + fileName
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                // Thêm ảnh placeholder
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImagePath = "/images/placeholder.jpg" // ✅ Đổi tên file cho khớp
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Thêm sản phẩm thành công!";
            return RedirectToAction("Products");
        }

        ViewBag.ProductTypes = GetProductTypes();
        return View(product);
    }

    // ================================
    // PRODUCTS LIST
    // ================================
    [HttpGet]
    public async Task<IActionResult> Products()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (shop == null) return NotFound("Bạn chưa có shop!");

        var products = await _context.Products
            .Where(p => p.ShopId == shop.Id)
            .Include(p => p.Images)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Quantity,
                p.SizeOrWeight,
                p.ProductType,
                ImagePath = p.Images.FirstOrDefault() != null
                    ? p.Images.FirstOrDefault().ImagePath
                    : "/images/no-image.png"
            })
            .ToListAsync();

        return View(products);
    }

    // ================================
    // HELPER: GET PRODUCT TYPES
    // ================================
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

  
// EDIT INFO - GET
// ================================
[HttpGet]
    public async Task<IActionResult> EditInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null) return NotFound("Bạn chưa có shop!");

        ViewBag.Shop = shop;
        return View();
    }

    // ================================
    // EDIT INFO - POST
    // ================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditInfo(string Name, string Address, string Phone)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null) return NotFound();

        // Cập nhật thông tin
        shop.Name = Name;
        shop.Address = Address ?? "";
        shop.Phone = Phone ?? "";

        _context.Shops.Update(shop);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật thông tin shop thành công!";
        return RedirectToAction("Dashboard");
    }

    // ================================
    // REPORT, INVENTORY, CHAT...
    // ================================
    public async Task<IActionResult> Report(int? year, int? month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null) return NotFound();

        var query = _context.Orders.Where(o => o.ShopId == shop.Id);
        if (year.HasValue) query = query.Where(o => o.OrderDate.Year == year);
        if (month.HasValue) query = query.Where(o => o.OrderDate.Month == month);

        var orders = await query.ToListAsync();
        var revenue = orders.Sum(o => o.TotalPrice);

        ViewBag.Year = year ?? DateTime.Now.Year;
        ViewBag.Month = month;
        ViewBag.Revenue = revenue;
        ViewBag.OrderCount = orders.Count;

        return View(orders);
    }


    public async Task<IActionResult> Inventory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null) return NotFound("Bạn chưa có shop!");

        // ✅ RETURN TRỰC TIẾP MODEL PRODUCT
        var products = await _context.Products
            .Where(p => p.ShopId == shop.Id)
            .Include(p => p.Images)
            .ToListAsync();

        return View(products);
    }

    public async Task<IActionResult> Chat()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var messages = await _context.ChatMessages
            .Where(m => m.ShopOwnerId == userId)
            .Include(m => m.Customer)
            .OrderByDescending(m => m.SentDate)
            .ToListAsync();

        return View(messages);
    }
}