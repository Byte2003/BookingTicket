﻿using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using SWP_BookingTicket.DataAccess.Data;
using SWP_BookingTicket.DataAccess.Repositories;
using SWP_BookingTicket.Models;
using SWP_BookingTicket.Services;
using System.Configuration;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
//DbContext Configuration
builder.Services.AddDbContextFactory<AppDbContext>(
	   options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<AppDbContext>(option => {
	option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
	option.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
//Identity Configuration
builder.Services.AddIdentity<AppUser, IdentityRole>()
		.AddEntityFrameworkStores<AppDbContext>()
		.AddDefaultTokenProviders()
		.AddDefaultUI();
builder.Services.Configure<IdentityOptions>(options =>
{
	options.Password.RequireDigit = false; 
	options.Password.RequireLowercase = false; 
	options.Password.RequireNonAlphanumeric = false; 
	options.Password.RequireUppercase = false; 
	options.Password.RequiredLength = 3; 
	options.Password.RequiredUniqueChars = 1; 
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); 
	options.Lockout.MaxFailedAccessAttempts = 5; 
	options.Lockout.AllowedForNewUsers = true;
	options.User.AllowedUserNameCharacters = 
		"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
	options.User.RequireUniqueEmail = true;  
	options.SignIn.RequireConfirmedEmail = true;            
	options.SignIn.RequireConfirmedPhoneNumber = false;     
});
//Google Authen
builder.Services.AddAuthentication()
				 .AddGoogle(option => {
	// Đọc thông tin Authentication:Google từ appsettings.json
	IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");

	// Thiết lập ClientID và ClientSecret để truy cập API google
	option.ClientId = googleAuthNSection["ClientId"];
	option.ClientSecret = googleAuthNSection["ClientSecret"];
	// Cấu hình Url callback lại từ Google (không thiết lập thì mặc định là /signin-google)
	option.CallbackPath = "/GoogleLogin";
});
//Cookie
builder.Services.ConfigureApplicationCookie(option =>
{
	option.LoginPath = $"/Identity/Account/Login";
	option.LogoutPath = $"/Identity/Account/Logout";
	option.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});
//Logger Configuration
var logger = new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File("Logs/BookingTicket_Logs.txt", rollingInterval: RollingInterval.Day)
			.MinimumLevel.Warning()
			.CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
//Mail Configuration
var mailsetttings = builder.Configuration.GetSection("MailSettings");
builder.Services.Configure<MailSettings>(mailsetttings);
builder.Services.AddTransient<IEmailSender, SendMailService>();

// Initialize database
builder.Services.AddScoped<DbInitialize>();
// UnitOfWork 
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// Upload image
builder.Services.AddScoped<UploadImageService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.MapRazorPages();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
SeedData();
app.MapControllerRoute(
	name: "default",
	pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();
async void SeedData()
{
	using (var scope = app.Services.CreateScope())
	{
		var dbInit = scope.ServiceProvider.GetRequiredService<DbInitialize>();
		await dbInit.SeedAdminAccountsAsync();
		await dbInit.SeedCinemaManagerAsync();
	}
}
