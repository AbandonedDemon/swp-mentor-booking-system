using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SwpMentorBooking.Application.Common.Interfaces;
using SwpMentorBooking.Infrastructure.Configuration;
using SwpMentorBooking.Infrastructure.Data;
using SwpMentorBooking.Infrastructure.Repository;
using SwpMentorBooking.Infrastructure.Utils;
using SwpMentorBooking.Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add DbContext

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Register services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICSVFileService, CSVFileService>();
builder.Services.AddScoped<IAutoMapperService, AutoMapperService>();
builder.Services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();
builder.Services.AddScoped<IMIMEFileService, MIMEFileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IStringManipulationService, StringManipulationService>();
builder.Services.AddScoped<IUtilService, UtilService>();
// Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, GroupLeaderHandler>();
// Add Auto-mapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
//
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option =>
    {
        option.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        option.LoginPath = $"/Account/Login";
        option.LogoutPath = $"/Account/Logout";
        option.AccessDeniedPath = $"/Account/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GroupLeaderOnly", policy =>
    policy.Requirements.Add(new GroupLeaderRequirement()));

    options.AddPolicy("StudentInGroupOnly", policy =>
    policy.Requirements.Add(new StudentInGroupRequirement()));
});
// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 52428800; // 50 MB
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
