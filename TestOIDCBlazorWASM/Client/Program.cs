using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TestOIDCBlazorWASM.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// TODO : voir comment on peut créer une autre HttpClient injecté, car sinon, le FetchData plante, même si on met le serveur en [AllowAnonymous]
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
// Comme expliqué sur https://docs.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-6.0
builder.Services
    .AddHttpClient("WebAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("WebAPI"));

// Fonctionne car Microsoft.AspNetCore.Components.WebAssembly.Authentication a été ajoutée
// Nécessite aussi le contenu du fichier appsettings.json, qui lui aussi doit être créé, mais dans wwwroot
// Pour la liaison avec l'ordre suivant, voir https://medium.com/@marcodesanctis2/role-based-security-with-blazor-and-identity-server-4-aba12da70049
builder.Services.AddOidcAuthentication(options => {
    // Charge les paramètres depuis le fichier de settings
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    //options.UserOptions.NameClaim = "preferred_username"; // La valeur par défaut name est bien
    //options.UserOptions.RoleClaim = "user_roles";

    // Si on ne surcharge pas cette option, .NET cherche le contenu de "roles" et ne passe donc rien dans l'identité
    options.UserOptions.RoleClaim = "resource_access.appli-eni.roles"; // resource_access.${client_id}.roles dans KeyCloak

    //options.UserOptions.ScopeClaim= "scope";
    //options.ProviderOptions.PostLogoutRedirectUri = "/";
});

// Le fait de passer le bon RoleClaim ci-dessus ne suffit pas, car comme il est complexe, .NET ne le comprend pas directement.
// La classe ci-dessous récupère le contenu JSON de resource_access et transforme l'array enfoui dans appli-eni / roles en autant
// d'attributs de type "resource_access.appli-eni.roles"
// https://github.com/javiercn/BlazorAuthRoles pour le code et https://github.com/dotnet/AspNetCore.Docs/issues/17649 pour l'issue
builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<RolesClaimsPrincipalFactory>();

// Pour que ça marche, il faut créer un client dans KeyCloak avec :
// - appli-eni comme client id
// - LivreENI comme nom de Realm
// - Rajouter en Valid Redirect URIs :
//   * https://localhost:7070/authentication/login-callback
//   * https://localhost:7070/authentication/logout-callback
// - Rajouter en Web Origins : (sinon problème de CORS)
//   * https://localhost:7070 (sans / à la fin sinon ça plante)

await builder.Build().RunAsync();
