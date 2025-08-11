using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Models.Enums
{
    public enum ActivityCategory
    {
        Activity,           // Toutes les activités sous-marines
        Structure,          // Clubs, centres, bases fédérales
        Shop,               // Boutiques et magasins
        Other,              // Autres types
        
        // Anciennes valeurs gardées pour compatibilité temporaire
        [Obsolete("Use Activity instead")]
        Diving = Activity,
        [Obsolete("Use Activity instead")]
        Freediving = Activity,
        [Obsolete("Use Activity instead")]
        Snorkeling = Activity,
        [Obsolete("Use Activity instead")]
        UnderwaterPhotography = Activity
    }
}
