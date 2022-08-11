using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.Json;

namespace TestOIDCBlazorWASM.Server
{
    /// <summary>
    /// Référence sur https://docs.microsoft.com/fr-fr/aspnet/core/security/authentication/claims?view=aspnetcore-6.0
    /// </summary>
    public class ClaimsTransformer : IClaimsTransformation
    {
        private string PrefixeRoleClaim { get; init; }
        private string OIDCClientId { get; init; }
        private string SuffixeRoleClaim { get; init; }
        private string TargetUserRolesClaimName { get; init; }

        public ClaimsTransformer(IConfiguration config)
        {
            string ModelePourRoleClaim = config.GetSection("OIDC")["ModelePourRoleClaim"];
            PrefixeRoleClaim = ModelePourRoleClaim.Substring(0, ModelePourRoleClaim.IndexOf("."));
            SuffixeRoleClaim = ModelePourRoleClaim.Substring(ModelePourRoleClaim.LastIndexOf(".") + 1);
            OIDCClientId = config["OIDC__ClientId"];
            TargetUserRolesClaimName = config.GetSection("OIDC").GetValue<string>("TargetUserRolesClaimName");
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
            if (claimsIdentity.IsAuthenticated)
            {
                // Obligé de cloner la liste car sinon, le fait de la modifier défrise .NET
                //foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == "user_roles"))
                foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == PrefixeRoleClaim))
                {
                    //claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, c.Value));
                    JsonDocument doc = JsonDocument.Parse(c.Value);
                    foreach (JsonElement elem in doc.RootElement.GetProperty(OIDCClientId).GetProperty(SuffixeRoleClaim).EnumerateArray())
                        claimsIdentity.AddClaim(new Claim(this.TargetUserRolesClaimName, elem.GetString() ?? String.Empty));
                }
            }
            return Task.FromResult(principal);
        }
    }
}
