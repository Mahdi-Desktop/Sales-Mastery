using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using Firebase.Auth;
using Firebase.Auth.Providers;
using AspnetCoreMvcFull.Interfaces;
using AspnetCoreMvcFull.Services;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;
using Google.Api;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Connect to the database
builder.Services.AddDbContext<AspnetCoreMvcFullContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AspnetCoreMvcFullContext") ?? throw new InvalidOperationException("Connection string 'AspnetCoreMvcFullContext' not found.")));

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Firebase app authentication setup
var credentialsFileLocation = builder.Configuration.GetValue<string>("Firebase:ServiceAccountKeyPath");
var firebaseProjectName = builder.Configuration.GetValue<string>("Firebase:ProjectId");
var firebaseApiKey = builder.Configuration.GetValue<string>("Firebase:ApiKey");

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsFileLocation);
builder.Services.AddSingleton(FirebaseApp.Create());
// Add this after the FirebaseApp.Create() line
builder.Services.AddSingleton(provider => {
  return FirestoreDb.Create(firebaseProjectName);
});


// Authentication setup
builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig
{
  ApiKey = firebaseApiKey,
  AuthDomain = $"{firebaseProjectName}.firebaseapp.com",
  Providers = new FirebaseAuthProvider[]
    {
        new EmailProvider(),
        new GoogleProvider()
    }
}));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.Authority = $"https://securetoken.google.com/{firebaseProjectName}";
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidIssuer = $"https://securetoken.google.com/{firebaseProjectName}",
        ValidateAudience = true,
        ValidAudience = firebaseProjectName,
        ValidateLifetime = true
      };
    });

builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<ProductService>(); // Add this line
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<AffiliateService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<BrandService>();
builder.Services.AddScoped<CommissionService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<AuthService>();
// Add Firebase Storage service
builder.Services.AddSingleton<FirebaseStorageService>();

builder.Services.AddSession();

var app = builder.Build();

// Create a service scope to get an AspnetCoreMvcFullContext instance using DI and seed the database.
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

app.Use(async (context, next) =>
{
  var token = context.Session.GetString("token");
  if (!string.IsNullOrEmpty(token))
  {
    context.Request.Headers.Append("Authorization", "Bearer " + token);
  }
  await next();
});


app.UseStatusCodePages(async contextAccessor =>
{
  var response = contextAccessor.HttpContext.Response;
  if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
  {
    await Task.Run(() => response.Redirect("/Pages/MiscError"));
  }
});


app.UseRouting();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

//app.MapControllerRoute(
//    name: "Dashbords",
//    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Auth}/{action=LoginBasic}/{id?}");

//

app.MapControllerRoute(
    name: "Dashbords",
   pattern: "Dashbords",
   defaults: new { controller = "Dashbords", action = "Index" });

//app.MapControllerRoute(
//   //name: "default",
//   //pattern: "{controller=Login}/{action=Index}/{id?}"
//   name: "companyRoute",
//   pattern: "{Company}",
//   defaults: new { controller = "Login", action = "Index" });

app.MapControllerRoute(
name: "default",
pattern: "{controller=Auth}/{action=LoginBasic}/{id?}");


app.Run();
