using TinyHouse.Repositories;
using TinyHouse.Models;
using TinyHouse.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Scoped Services for Repositories and Data Services
builder.Services.AddScoped<DurumRepository>();
builder.Services.AddScoped<RezervasyonRepository>();
builder.Services.AddScoped<TinyHouseRepository>();
builder.Services.AddScoped<KonumRepository>();
builder.Services.AddScoped<TinyHouse.Data.SqlConnectionFactory>();
builder.Services.AddScoped<YorumRepository>();
builder.Services.AddScoped<BildirimRepository>();

// Configure EmailSender for dependency injection
builder.Services.AddSingleton<EmailSender>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new EmailSender(configuration);
});

// Add session and distributed memory cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);  // Session Timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;  // Required for session to work in browsers
});
builder.Services.AddHttpContextAccessor();  // Add HTTP Context accessor

var app = builder.Build();

// Environment specific exception handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();  // Strict HTTP headers for production environments
}
else
{
    app.UseDeveloperExceptionPage();  // Detailed exception page in dev mode
}

app.UseHttpsRedirection();  // Force HTTPS redirection
app.UseStaticFiles();  // Serve static files

app.UseRouting();  // Enable routing
app.UseSession();  // Use session state
app.UseAuthorization();  // Enable authorization

// Configure the default route for controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
