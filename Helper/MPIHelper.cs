using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace MorePrisonerInteractions.Helper
{
    public static class MPIHelper
    {

        private static IEnumerable<Hero> DiscoverAncestors(Hero hero, int n)
        {
            if (hero != null)
            {
                yield return hero;
                if (n > 0)
                {
                    foreach (Hero hero2 in DiscoverAncestors(hero.Mother, n - 1))
                    {
                        yield return hero2;
                    }
                    IEnumerator<Hero> enumerator = null;
                    foreach (Hero hero3 in DiscoverAncestors(hero.Father, n - 1))
                    {
                        yield return hero3;
                    }
                    enumerator = null;
                }
            }
            yield break;
        }
    }
}
