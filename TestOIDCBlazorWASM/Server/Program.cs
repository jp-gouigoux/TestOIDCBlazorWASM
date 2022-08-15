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

// Ajout pour gérer le format JSONPatch, pas encore pris complètement en compte en natif dans System.Text.Json
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, CustomJPIF.GetJsonPatchInputFormatter());
}).AddNewtonsoftJson();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    // On doit pouvoir faire mieux avec un binder de configuration
    IConfigurationSection ConfigOIDC = builder.Configuration.GetSection("OIDC");
    o.Authority = builder.Configuration["OIDC__Authority"];
    o.Audience = ConfigOIDC["Audience"];
    // Les deux options à suivre ne sont à faire qu'en mode DEVELOPMENT, mais depuis que l'app doit être buildée
    // avant qu'on puisse avoir accès à app.Environment.IsDevelopment(), on ne peut plus utiliser ces codes
    //o.BackchannelHttpHandler = new HttpClientHandler()
    //{
    //    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    //};
    o.RequireHttpsMetadata = false;
    //o.TokenValidationParameters.RoleClaimType = "user_roles";

    o.TokenValidationParameters.RoleClaimType = ConfigOIDC["ModelePourRoleClaim"].Replace("${client_id}", builder.Configuration["OIDC__ClientId"]);
    o.TokenValidationParameters.NameClaimType = ConfigOIDC["NameClaimType"]; // Fait sens ici car côté serveur, on utiliserait le nom pour la traçabilité
    o.TokenValidationParameters.ValidateIssuer = false;

    //o.SaveToken = true; // A voir dans la doc pour l'utilisation précise
    o.Events = new JwtBearerEvents()
    {
        OnAuthenticationFailed = c =>
        {
            c.NoResult();
            c.Response.StatusCode = 500;
            c.Response.ContentType = "text/plain";
            return c.Response.WriteAsync(c.Exception.ToString()); // A ne laisser que pour le mode DEVELOPMENT
        }
    };
}).AddCookie(options =>
{
    // NE SERT APPAREMMENT PAS A REGLER LE SOUCI
    options.Cookie.SameSite = SameSiteMode.None;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    //options.Cookie.HttpOnly = true;
});

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

//builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<ClaimsTransformer>();

// Solution trouvée sur https://stackoverflow.com/questions/53702555/cant-access-roles-in-jwt-token-net-core
// qui permet de générer des policies, et aussi d'extraire des rôles avec un ClaimsTransformer (redondant
// avec la spécification plus haut des RoleClaimType = "user_roles", mais permet de gérer des cas plus complexes.
// Pour des cas encore plus sophistiqués, voir https://referbruv.com/blog/role-based-and-claims-based-authorization-in-aspnet-core-using-policies-hands-on/
builder.Services.AddAuthorization(o =>
{
    string TargetUserRolesClaimName = builder.Configuration.GetSection("OIDC")["TargetUserRolesClaimName"];
    o.AddPolicy("administrateur", policy => policy.RequireClaim(TargetUserRolesClaimName, "administrateur"));
    o.AddPolicy("lecteur", policy => policy.RequireClaim(TargetUserRolesClaimName, "lecteur"));
});

//builder.WebHost.UseKestrel(opts =>
//{
//    opts.ListenAnyIP(5000);
//});

builder.WebHost.UseUrls("http://+:5000");

// Pour info, on peut brancher Keycloak comme proxy d'un IDP Microsoft OIDC comme Azure DC, mais pour récupérer
// les claims pour aller sur Graph, il faut faire une bidouille expliquée sur https://keycloak.discourse.group/t/is-it-possible-to-use-an-keycloak-accesstoken-to-get-access-to-the-microsoft-graph/6831/3

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

//app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Ajouté pour activer l'authentification et la gestion des autorisations intégrées
app.UseAuthentication();
app.UseAuthorization();

//app.UseCors(cors => cors.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
