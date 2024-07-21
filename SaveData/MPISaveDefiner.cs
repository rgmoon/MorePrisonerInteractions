using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace MorePrisonerInteractions.SaveData
{
    internal class MPISaveDefiner : SaveableTypeDefiner
    {
        public MPISaveDefiner() : base(256242336) { }
        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Hero, CampaignTime>));
            ConstructContainerDefinition(typeof(Dictionary<Hero, Equipment>));
        }
    }
}
