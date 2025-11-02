using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using TrangWebDH.Data;
using TrangWebDH.Models;

var builder = WebApplication.CreateBuilder(args);

// ✅ CẤU HÌNH DATABASE - BỎ QUA MIGRATION
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // ✅ BẬT LOG ĐỂ DEBUG
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());

    // ⚠️ QUAN TRỌNG: KHÔNG TỰ ĐỘNG TẠO/SỬA DATABASE
    // Không gọi EnsureCreated() hay Migrate()
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB max
});

var app = builder.Build();

// ✅ SEED DATA - CHỈ TẠO ROLES VÀ ADMIN
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // ⚠️ KHÔNG GỌI Database.Migrate() hay Database.EnsureCreated()
        // var context = services.GetRequiredService<ApplicationDbContext>();
        // context.Database.Migrate(); ← XÓA DÒNG NÀY NẾU CÓ

        // Tạo roles
        string[] roles = { "Admin", "ShopOwner", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Tạo admin
        var adminEmail = "admin@trangwebdh.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Admin TrangWebDH",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        Console.WriteLine("✅ Seed data completed!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error seeding data: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();