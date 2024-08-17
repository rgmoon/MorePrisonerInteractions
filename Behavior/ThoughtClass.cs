using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace MorePrisonerInteractions.Behavior
{
    public class ThoughtClass : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
        public void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_Ask_Faction", "dialog_mpi_main_options", "lord_faction_ask_1", "{=Dialog_MPI_PersuadePrisoner_Ask_Faction}Can you tell me about the 'Nine streams and ten families'?", null, null, 100, null, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_Reply_Faction", "lord_faction_ask_1", "lord_faction_ask_2", "The 'Nine streams and ten families' refer to the major schools of thought in ancient China, including Confucianism, Daoism, Legalism, and others.", null, null, 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_Reply_Faction2", "lord_faction_ask_2", "dialog_mpi_main_options", "{=!}{Dialog_MPI_PersuadePrisoner_Reply_Faction}", null, () => AskFaction(Hero.MainHero, Hero.OneToOneConversationHero), 100, null);
        }
        public string DetermineFaction(int valor, int mercy, int honor, int generosity, int calculating)
        {
            var legalist = new Tuple<int, int, int, int, int>(0, -2, -1, -2, 2);
            var mohist = new Tuple<int, int, int, int, int>(2, 2, 0, 2, 0);
            var confucianist = new Tuple<int, int, int, int, int>(0, 1, 2, 1, -1);

            int legalistScore = Math.Abs(valor - legalist.Item1) + Math.Abs(mercy - legalist.Item2) + Math.Abs(honor - legalist.Item3) + Math.Abs(generosity - legalist.Item4) + Math.Abs(calculating - legalist.Item5);
            int mohistScore = Math.Abs(valor - mohist.Item1) + Math.Abs(mercy - mohist.Item2) + Math.Abs(honor - mohist.Item3) + Math.Abs(generosity - mohist.Item4) + Math.Abs(calculating - mohist.Item5);
            int confucianistScore = Math.Abs(valor - confucianist.Item1) + Math.Abs(mercy - confucianist.Item2) + Math.Abs(honor - confucianist.Item3) + Math.Abs(generosity - confucianist.Item4) + Math.Abs(calculating - confucianist.Item5);

            int minScore = Math.Min(legalistScore, Math.Min(mohistScore, confucianistScore));

            if (minScore == legalistScore)
            {
                return "Legalist";
            }
            else if (minScore == mohistScore)
            {
                return "Mohist";
            }
            else if (minScore == confucianistScore)
            {
                return "Confucianist";
            }

            return "Not belong any school of thoughts";
        }

        public string Thought(Hero hero)
        {
            HeroTraitsGetSet herotraits = new HeroTraitsGetSet(hero);
            return DetermineFaction(herotraits.Valor, herotraits.Mercy, herotraits.Honor, herotraits.Generosity, herotraits.Calculating);
        }
        public void AskFaction(Hero player, Hero x)
        {
            MBTextManager.SetTextVariable("Dialog_MPI_PersuadePrisoner_Reply_Faction", "And Mine I think is {NPC_FACTION},And I think you are {PLAYER_FACTION}");
            MBTextManager.SetTextVariable("NPC_FACTION", Thought(Hero.OneToOneConversationHero));
            MBTextManager.SetTextVariable("PLAYER_FACTION", Thought(Hero.MainHero));
        }
        public bool CheckIfExpert(Hero hero, int checkmode)
        {
            if (Thought(hero) == "Legalist" && hero.GetSkillValue(DefaultSkills.Steward) >= 300 && checkmode == 1) return true;
            else if (Thought(hero) == "Mohist" && hero.GetSkillValue(DefaultSkills.Tactics) >= 300 && checkmode == 2) return true;
            else if (Thought(hero) == "Confucianist" && hero.GetSkillValue(DefaultSkills.Charm) >= 300 && checkmode == 3) return true;
            return false;
        }
    }

}
