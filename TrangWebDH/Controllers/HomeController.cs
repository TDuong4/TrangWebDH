using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrangWebDH.Data;


    namespace TrangWebDH.Controllers
    {
        public class HomeController : Controller
        {
            private readonly ApplicationDbContext _context;

            public HomeController(ApplicationDbContext context)
            {
                _context = context;
            }

            // [SỬA] XÓA [Authorize] ĐỂ AI CŨNG VÀO ĐƯỢC
            public IActionResult Index(string q)
            {
                try
                {
                    var query = _context.Products
                        .Include(p => p.Images)
                        .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(q))
                    {
                        var term = q.Trim();
                        query = query.Where(p =>
                            (p.Name != null && p.Name.Contains(term)) ||
                            (p.Description != null && p.Description.Contains(term))
                        );
                    }

                    var products = query.Take(8).ToList();
                    return View(products);
                }
                catch (Exception ex)
                {
                    ViewBag.Error = ex.Message;
                    return View(new List<Models.Product>());
                }
            }

            public IActionResult Search(string q)
            {
                return RedirectToAction(nameof(Index), new { q });
            }
        }
    }