using Community_Event_Finder.Data;
using Community_Event_Finder.Data.ExternalProviders;
using Community_Event_Finder.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Community_Event_Finder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                    sqlOptions.EnableRetryOnFailure()));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();

            // Register and validate external providers configuration
            var providersSection = builder.Configuration.GetSection(ExternalProvidersSettings.SectionName);
            var providersSettings = providersSection.Get<ExternalProvidersSettings>() ?? new ExternalProvidersSettings();
            ValidateExternalProvidersConfiguration(providersSettings);
            builder.Services.Configure<ExternalProvidersSettings>(providersSection);

            // Register HTTP client factory and repository
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IEventRepository, EventRepository>();

            // Register external event providers
            builder.Services.AddScoped<PredictHQProvider>();
            builder.Services.AddScoped<TicketmasterProvider>();
            builder.Services.AddScoped<SeatGeekProvider>();
            builder.Services.AddScoped<IExternalEventProviderFactory, ExternalEventProviderFactory>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Serve static files with custom default file
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { "start.html" }
            });
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }

        // Validates external providers configuration and throws if required settings are missing for enabled providers
        private static void ValidateExternalProvidersConfiguration(ExternalProvidersSettings settings)
        {
            var allErrors = new List<string>();

            // Validate each provider
            allErrors.AddRange(settings.PredictHQ.Validate());
            allErrors.AddRange(settings.Ticketmaster.Validate());
            allErrors.AddRange(settings.SeatGeek.Validate());

            // If there are any validation errors, throw an exception
            if (allErrors.Count > 0)
            {
                var message = "External Providers Configuration Errors:\n" + string.Join("\n", allErrors);
                throw new InvalidOperationException(message);
            }
        }
    }
}
