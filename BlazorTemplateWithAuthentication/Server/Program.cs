using BlazorTemplateWithAuthentication.Server.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// start swagger
builder.Services.AddSwaggerGen();
//end swagger
//start setting database
builder.Services.AddDbContext<AppDbContext>(options =>
                                            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedEmail = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtIssuer"],
        ValidAudience = builder.Configuration["JwtAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSecurityKey"]!))
    };
});
//end setting database
var app = builder.Build();

var scope = app.Services.CreateScope();

#region ApplyingMigrationsAndAddingRoles
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.MigrateAsync();

var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//Check if "Admin" Role exists and create if it doesn't
if (!await roleManager.RoleExistsAsync("Admin"))
{
    var adminRole = new IdentityRole("Admin");
    await roleManager.CreateAsync(adminRole);
}

// Check if the "User" role exists and create it if it doesn't
if (!await roleManager.RoleExistsAsync("User"))
{
    var userRole = new IdentityRole("User");
    await roleManager.CreateAsync(userRole);
}

//adding admin account
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
string adminEmail = "admin@test.com";
string adminPassword = "Admin@123";
var adminUser = await userManager.FindByEmailAsync(adminEmail);
if (adminUser == null)
{
    adminUser = new AppUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true,
        LockoutEnabled = false,
    };
    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

//Enable Authentication
app.UseAuthentication();
app.UseAuthorization();

//Enabale swagger
app.UseSwagger();
app.UseSwaggerUI();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
