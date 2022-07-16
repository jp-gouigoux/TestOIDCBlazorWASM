using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Security.Claims;

namespace TestOIDCBlazorWASM.Server
{
    public class ClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
            if (claimsIdentity.IsAuthenticated)
            {
                // Obligé de cloner la liste car sinon, le fait de la modifier défrise .NET
                foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == "user_roles"))
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, c.Value));
                }
            }
            return Task.FromResult(principal);
        }
    }
}
