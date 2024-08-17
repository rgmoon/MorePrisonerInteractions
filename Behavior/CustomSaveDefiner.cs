using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace MorePrisonerInteractions.Behavior
{
    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        public CustomSaveDefiner() : base(2_33389_64) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(BridePriceInfo), 1);
            AddClassDefinition(typeof(HeroTraitsGetSet), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Hero, BridePriceInfo>));
        }
    }
}
