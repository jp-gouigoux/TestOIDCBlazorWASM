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
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
            if (claimsIdentity.IsAuthenticated)
            {
                // Obligé de cloner la liste car sinon, le fait de la modifier défrise .NET
                //foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == "user_roles"))
                foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == "resource_access"))
                {
                    //claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, c.Value));
                    JsonDocument doc = JsonDocument.Parse(c.Value);
                    foreach (JsonElement elem in doc.RootElement.GetProperty("appli-eni").GetProperty("roles").EnumerateArray())
                        claimsIdentity.AddClaim(new Claim("user_roles", elem.GetString() ?? String.Empty));
                }
            }
            return Task.FromResult(principal);
        }
    }
}
