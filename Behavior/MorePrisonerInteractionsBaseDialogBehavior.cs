using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;

namespace MorePrisonerInteractions.Behavior
{
    public class MorePrisonerInteractionsBaseDialogBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            //throw new NotImplementedException();
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }

        public void AddDialogs(CampaignGameStarter gameStarter) 
        {
            gameStarter.AddPlayerLine("dialog_mpi_start_trigger", "hero_main_options", "dialog_mpi_start", "{=Dialog_MPI_Start}As my prisoner, I have something to talk to you about!", new ConversationSentence.OnConditionDelegate(this.CanSpeakToPrisoner), null, 100, null, null);
            gameStarter.AddPlayerLine("dialog_mpi_start_trigger", "CEPrisonerInParty", "dialog_mpi_start", "{=Dialog_MPI_Start}As my prisoner, I have something to talk to you about!", new ConversationSentence.OnConditionDelegate(this.CanSpeakToPrisoner), null, 100, null, null);
            gameStarter.AddPlayerLine("dialog_mpi_start_trigger", "prisoner_recruit_start_player", "dialog_mpi_start", "{=Dialog_MPI_Start}As my prisoner, I have something to talk to you about!", new ConversationSentence.OnConditionDelegate(this.CanSpeakToPrisoner), null, 100, null, null);
            gameStarter.AddDialogLine("dialog_mpi_start", "dialog_mpi_start", "dialog_mpi_main_options", "{=Dialog_MPI_StartReply}So, what do you want now?", null, null, 100, null);
            gameStarter.AddPlayerLine("dialog_mpi_start_trigger", "dialog_mpi_main_options", "lord_pretalk", "{=Dialog_MPI_Close}Nothing.", null, null, 99, null, null);
        }

        bool CanSpeakToPrisoner()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsPrisoner && Campaign.Current.CurrentConversationContext != ConversationContext.CapturedLord && Campaign.Current.CurrentConversationContext != ConversationContext.FreedHero && ((Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner != null && Hero.OneToOneConversationHero.PartyBelongedToAsPrisoner.Owner.Clan == Clan.PlayerClan) || (Hero.OneToOneConversationHero.CurrentSettlement != null && Hero.OneToOneConversationHero.CurrentSettlement.OwnerClan == Clan.PlayerClan));
        }
    }
}
