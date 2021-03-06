using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using TestOIDCBlazorWASM.Server;
using TestOIDCBlazorWASM.Work;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorPages();
//builder.Services.AddCors();

// Ajout pour g?rer le format JSONPatch, pas encore pris compl?tement en compte en natif dans System.Text.Json
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, CustomJPIF.GetJsonPatchInputFormatter());
}).AddNewtonsoftJson();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = "http://localhost:8080/realms/LivreENI/";
    o.Audience = "account";
    // Les deux options ? suivre ne sont ? faire qu'en mode DEVELOPMENT, mais depuis que l'app doit ?tre build?e
    // avant qu'on puisse avoir acc?s ? app.Environment.IsDevelopment(), on ne peut plus utiliser ces codes
    //o.BackchannelHttpHandler = new HttpClientHandler()
    //{
    //    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    //};
    o.RequireHttpsMetadata = false;
    //o.TokenValidationParameters.RoleClaimType = "user_roles";
    o.TokenValidationParameters.RoleClaimType = "resource_access.appli-eni.roles";
    o.TokenValidationParameters.NameClaimType = "preferred_username"; // Fait sens ici car c?t? serveur, on utiliserait le nom pour la tra?abilit?
    o.TokenValidationParameters.ValidateIssuer = true;
    //o.SaveToken = true; // A voir dans la doc pour l'utilisation pr?cise
    //o.Events = new JwtBearerEvents()
    //{
    //    OnAuthenticationFailed = c =>
    //    {
    //        c.NoResult();
    //        c.Response.StatusCode = 500;
    //        c.Response.ContentType = "text/plain";
    //        return c.Response.WriteAsync(c.Exception.ToString()); // A ne laisser que pour le mode DEVELOPMENT
    //    }
    //};
});

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

//builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<ClaimsTransformer>();

// Solution trouv?e sur https://stackoverflow.com/questions/53702555/cant-access-roles-in-jwt-token-net-core
// qui permet de g?n?rer des policies, et aussi d'extraire des r?les avec un ClaimsTransformer (redondant
// avec la sp?cification plus haut des RoleClaimType = "user_roles", mais permet de g?rer des cas plus complexes.
// Pour des cas encore plus sophistiqu?s, voir https://referbruv.com/blog/role-based-and-claims-based-authorization-in-aspnet-core-using-policies-hands-on/
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("admin", policy => policy.RequireClaim("user_roles", "administrateur"));
    o.AddPolicy("lecteur", policy => policy.RequireClaim("user_roles", "lecteur"));
});

// Pour info, on peut brancher Keycloak comme proxy d'un IDP Microsoft OIDC comme Azure DC, mais pour r?cup?rer
// les claims pour aller sur Graph, il faut faire une bidouille expliqu?e sur https://keycloak.discourse.group/t/is-it-possible-to-use-an-keycloak-accesstoken-to-get-access-to-the-microsoft-graph/6831/3

var app = builder.Build();

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

// Ajout? pour activer l'authentification et la gestion des autorisations int?gr?es
app.UseAuthentication();
app.UseAuthorization();

//app.UseCors(cors => cors.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
