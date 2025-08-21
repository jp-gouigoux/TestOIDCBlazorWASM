using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Cryptography.X509Certificates;
using TestOIDCBlazorWASM.API;
using TestOIDCBlazorWASM.Work;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Il est nécessaire d'avoir deux serveurs d'API séparés si on veut ne pas être obligés d'être en MutualTLS,
// car c'est activé au niveau de l'entrée du serveur, comme expliqué sur https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth?view=aspnetcore-6.0
// Toutefois, l'avantage est qu'on n'aura pas à supporter CORS, puisque ce sera un user-agent simple
// et pas un navigateur qui passera par cette exposition (et l'autre est sur le même hôte).

// Ajout pour gérer le format JSONPatch, pas encore pris complètement en compte en natif dans System.Text.Json
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, CustomJPIF.GetJsonPatchInputFormatter());
}).AddNewtonsoftJson();

//builder.Services.AddTransient<CertificateValidation>();
builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
{
    options.AllowedCertificateTypes = CertificateTypes.All;
    options.Events = new CertificateAuthenticationEvents
    {
        OnCertificateValidated = context =>
        {
            string empreinteReference = builder.Configuration["Securite__EmpreinteCertificatClient"];
            string empreinteRecue = context.ClientCertificate.Thumbprint;
#if DEBUG
            Console.WriteLine("Empreinte reçue : {0}", empreinteRecue);
            Console.WriteLine("Empreinte attendue : {0}", empreinteReference);
#endif
            if (string.Compare(empreinteRecue, empreinteReference, true) == 0)
                context.Success();
            else
                context.Fail("Invalid certificate");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            context.Fail("Invalid certificate made authentication fail");
            return Task.CompletedTask;
        }
    };
});

// Si ça ne marche pas derrière un ingress K8S nginx, il faudra jeter un oeil à https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth?view=aspnetcore-6.0
// Mode fonctionnant OK pour le client navigateur, mais pas avec Postman
// La transformation du PFX en PEM change tout de même le message d'erreur de Postman de "Unable to verify the first certificate" à "socket hang up"
builder.Services.Configure<KestrelServerOptions>(options =>
{
    string MotDePasseCertificatClient = builder.Configuration["Securite__MotDePasseCertificatClient"];
    string FichierMotDePasse = builder.Configuration["Securite__FichierMotDePasseCertificatClient"];
    if (!string.IsNullOrEmpty(FichierMotDePasse))
        MotDePasseCertificatClient = File.ReadAllText(FichierMotDePasse);
#if DEBUG
    Console.WriteLine("Début du mot de passe reçu : {0}", MotDePasseCertificatClient.Substring(0, 3));
    Console.WriteLine("Fichier de mot de passe : {0}", FichierMotDePasse);
    Console.WriteLine("Existence du fichier de mot de passe : {0}", File.Exists(FichierMotDePasse));
    Console.WriteLine("Fichier de certificat : {0}", builder.Configuration["Securite__CheminFichierCertificatClient"]);
    Console.WriteLine("Existence du fichier de certificat : {0}", File.Exists(builder.Configuration["Securite__CheminFichierCertificatClient"]));
#endif
    var cert = new X509Certificate2(
        builder.Configuration["Securite__CheminFichierCertificatClient"],
        MotDePasseCertificatClient);

    options.ConfigureHttpsDefaults(o =>
    {
        o.ServerCertificate = cert;
        o.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        o.AllowAnyClientCertificate();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();

// Côté autorisations, on ne fait pas dans le détail sur cette exposition d'API : si le client
// a le bon certificat, il a droit à tous les accès
app.UseAuthorization();

app.MapControllers();

app.Run();
