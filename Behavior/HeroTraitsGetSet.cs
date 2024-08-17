using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;


namespace MorePrisonerInteractions.Behavior
{
    public class HeroTraitsGetSet
    {
        [SaveableProperty(3)]
        public int Valor { get; set; }
        [SaveableProperty(4)]
        public int Mercy { get; set; }
        [SaveableProperty(5)]
        public int Honor { get; set; }
        [SaveableProperty(6)]
        public int Generosity { get; set; }
        [SaveableProperty(7)]
        public int Calculating { get; set; }

        public HeroTraitsGetSet(Hero hero)
        {
            Valor = hero.GetTraitLevel(DefaultTraits.Valor);
            Mercy = hero.GetTraitLevel(DefaultTraits.Mercy);
            Honor = hero.GetTraitLevel(DefaultTraits.Honor);
            Generosity = hero.GetTraitLevel(DefaultTraits.Generosity);
            Calculating = hero.GetTraitLevel(DefaultTraits.Calculating);
        }
    }

}
