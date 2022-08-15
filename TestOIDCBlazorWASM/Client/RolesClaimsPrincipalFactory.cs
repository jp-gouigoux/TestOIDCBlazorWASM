using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestOIDCBlazorWASM.Client
{
    /// <summary>
    /// Récupéré sur https://github.com/javiercn/BlazorAuthRoles, pour transformer
    /// 
    /// "user_roles": [
    ///   "administrateur",
    ///   "lecteur",
    ///   "manage-account",
    ///   "manage-account-links",
    ///   "view-profile"
    /// ],
    ///
    /// en
    ///
    /// "user_roles": "administrateur",
    /// "user_roles": "lecteur",
    /// "user_roles": "manage_account",
    /// "user_roles": "manage-account-links",
    /// "user_roles": "view-profile",
    /// 
    /// </summary>
    public class RolesClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        private string PrefixeRoleClaim { get; init; }
        private string OIDCClientId { get; init; }
        private string SuffixeRoleClaim { get; init; }

        public RolesClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor, IConfiguration config) : base(accessor)
        {
            string ModelePourRoleClaim = config.GetSection("OIDC")["ModelePourRoleClaim"];
            PrefixeRoleClaim = ModelePourRoleClaim.Substring(0, ModelePourRoleClaim.IndexOf("."));
            SuffixeRoleClaim = ModelePourRoleClaim.Substring(ModelePourRoleClaim.LastIndexOf(".") + 1);
            OIDCClientId = config.GetSection("OIDC")["ClientId"];
        }

        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
        {
            var user = await base.CreateUserAsync(account, options);
            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                // Si on ne spécifie rien dans Program.cs, ce sera 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'.
                // Mais si on met [options.UserOptions.RoleClaim = "resource_access.appli-eni.roles";] dans Program.cs,
                // alors on a la même valeur ici. Mais comme de toute façon on est obligé de repartir de la structure
                // dans cette classe, finalement il n'y a pas d'intérêt à le spécifier. Ca marcherait toutefois si on avait
                // un nombre de variable simple et en plus un contenu qui ne soit pas une liste.

                var identity = (ClaimsIdentity)user.Identity;
                if (identity.RoleClaimType == PrefixeRoleClaim + "." + OIDCClientId + "." + SuffixeRoleClaim)
                {
                    // On est dans le mode où KeyCloak envoie le contenu sous forme d'un texte JSON (le fait de passer l'option Claim JSON Type à JSON n'a pour l'instant rien donné de probant)
                    // TODO : Il reste un bug non bloquant sur le fait que les claims rajoutés sont systématiquement doublés, sans que le débogueur ne passe plusieurs fois ici. A regarder avec des logs, potentiellement plus fiables.
                    var resourceaccess = identity.FindAll(PrefixeRoleClaim);
                    string Contenu = resourceaccess.First().Value;
                    try
                    {
                        JsonDocument json = JsonDocument.Parse(resourceaccess.First().Value);
                        JsonElement elem = json.RootElement;
                        if (json.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement e in json.RootElement.EnumerateArray())
                                if (e.TryGetProperty(OIDCClientId, out var prop))
                                {
                                    elem = e;
                                    break;
                                }
                        }
                        foreach (JsonElement role in elem.GetProperty(OIDCClientId).GetProperty(SuffixeRoleClaim).EnumerateArray())
                        {
                            Console.WriteLine("Ajout du roleclaim {0} = {1}", options.RoleClaim, role.GetString() ?? String.Empty);
                            identity.AddClaim(new Claim(options.RoleClaim, role.GetString() ?? String.Empty));
                        }
                    }
                    catch { Console.WriteLine("Erreur dans la récupération des rôles liés au client OIDC"); }
                }
                else
                {
                    // On est dans le mode standard proposé par le code récupéré sur GitHub, et qui fonctionne sur user_roles
                    // Pas sûr que ce soit utile de garder ce code, car le parti-pris ici est bien d'utiliser les rôles du client,
                    // pour avoir une bonne séparation des responsabilités d'autorisation métier par métier, avec des clients IAM différents
                    var roleClaims = identity.FindAll(identity.RoleClaimType);
                    if (roleClaims != null && roleClaims.Any())
                    {
                        foreach (var existingClaim in roleClaims.ToList())
                            identity.RemoveClaim(existingClaim);

                        var rolesElem = account.AdditionalProperties[identity.RoleClaimType];
                        if (rolesElem is JsonElement roles)
                        {
                            if (roles.ValueKind == JsonValueKind.Array)
                                foreach (var role in roles.EnumerateArray())
                                    identity.AddClaim(new Claim(options.RoleClaim, role.GetString() ?? string.Empty));
                            else
                                identity.AddClaim(new Claim(options.RoleClaim, roles.GetString() ?? String.Empty));
                        }
                    }
                }
            }
            return user;
        }
    }
}
