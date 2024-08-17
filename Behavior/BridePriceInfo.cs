using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace MorePrisonerInteractions.Behavior
{
    public class BridePriceInfo
    {
        [SaveableProperty(1)]
        public int PlayerBridePrice { get; set; }
        [SaveableProperty(2)]
        public bool AlreadyGeneratedBridePrice { get; set; }
        public BridePriceInfo(int price, bool AlreadyGenerated)
        {
            PlayerBridePrice = price;
            AlreadyGeneratedBridePrice = AlreadyGenerated;
        }
    }
}
