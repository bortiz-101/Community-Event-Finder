using CommunityEventsApp.Data;

var builder = WebApplication.CreateBuilder(args);

// ======================
// 1️⃣ Register Services
// ======================

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IEventRepository, EventRepository>();

// ======================
// 2️⃣ Build App
// ======================

var app = builder.Build();
Db.Init(builder.Configuration);

// ======================
// 3️⃣ Configure Middleware
// ======================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "start.html" }
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ======================
// 4️⃣ Map Routes
// ======================

// MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API Controllers
app.MapControllers();

// ======================
// 5️⃣ Run
// ======================

app.Run();
