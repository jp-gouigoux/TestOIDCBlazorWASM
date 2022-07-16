using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestOIDCBlazorWASM.Shared
{
    public class Personne
    {
        public string? ObjectId { get; set; }

        public string? Patronyme { get; set; }

        public string? Prenom { get; set; }

        public string? urlFiche { get; set; }
    }
}
