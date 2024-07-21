using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace MorePrisonerInteractions.Behavior
{
    public class ForcePrisonerToMarryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }

        void AddDialogs(CampaignGameStarter gameStarter)
        {
            // Unmarried
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_unmarried_0", "dialog_mpi_main_options", "dialog_mpi_forcemarriage_unmarried_1", "{=Dialog_MPI_ForceMarriage_Unmarried_Start}I want you to marry one of my clan members! In exchange, I will set you free from your current chains.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(CanForceMarry));
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_1", "dialog_mpi_forcemarriage_unmarried_1", "dialog_mpi_forcemarriage_unmarried_2", "{=Dialog_MPI_ForceMarriage_Unmarried_AskWho}Fine...but who do you have in mind?", null, null);
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_unmarried_2", "dialog_mpi_forcemarriage_unmarried_2", "dialog_mpi_forcemarriage_unmarried_3", "{=Dialog_MPI_ForceMarriage_Unmarried_AskWhoReply}Let's see...", null, () => OpenInquiryMenuForMarryableMembers());
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_4", "...", null, null);
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_4", "dialog_mpi_forcemarriage_unmarried_4", "lord_pretalk", "{=Dialog_MPI_ForceMarriage_Unmarried_Finished}Very well. I shall accept your proposal. Now set me free...", null, null);


        }

        void OpenInquiryMenuForMarryableMembers()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            foreach (Hero hero in Clan.PlayerClan.Heroes)
            {
                if (CanMarry(Hero.OneToOneConversationHero, hero) && hero.IsAlive && hero.Clan == Clan.PlayerClan)
                {
                    list.Add(new InquiryElement(hero.Id, hero.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(hero.CharacterObject))));
                }
            }
            TextObject menuTitle = new TextObject("{=GameMenu_MPI_MarriageSelectionTitle}Who should {HERO_NAME} marry?");
            menuTitle.SetTextVariable("HERO_NAME", Hero.OneToOneConversationHero.FirstName);
            TextObject menuDesc = new TextObject("{=GameMenu_MPI_MarriageSelectionDesc}Select a clan member that {HERO_NAME} should marry.");
            menuDesc.SetTextVariable("HERO_NAME", Hero.OneToOneConversationHero.FirstName);
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(menuTitle.ToString(), menuDesc.ToString(), list, true, 1, 1, new TextObject("{=GameMenu_MPI_MarriageSelectionConfirmationButton}Confirm").ToString(), new TextObject("{=GameMenu_MPI_MarriageSelectionCancelButton}Cancel").ToString(), HandleSuccessfulInquiryMenuForMarriage, HandleFailedInquiryMenuForMarriage, null), false, false);
        }

        void HandleSuccessfulInquiryMenuForMarriage(List<InquiryElement> inqury)
        {
            InquiryElement inquiryElement = inqury[inqury.Count - 1];
            Hero first = Hero.FindFirst(H => H.Id == (MBGUID)inquiryElement.Identifier);

            MarriageAction.Apply(first, Hero.OneToOneConversationHero);
            //Hero.OneToOneConversationHero.Spouse = first;
            //first.Spouse = Hero.OneToOneConversationHero;
            Hero.OneToOneConversationHero.SetNewOccupation(first.Occupation);
            if (Hero.OneToOneConversationHero.Occupation == Occupation.Wanderer)
            {

                CharacterObject elementWithPredicate = TaleWorlds.Core.Extensions.GetRandomElementWithPredicate<CharacterObject>(Hero.OneToOneConversationHero.Culture.NotableAndWandererTemplates, (Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && ((BasicCharacterObject)x).IsFemale == ((BasicCharacterObject)Hero.OneToOneConversationHero.CharacterObject).IsFemale && x.CivilianEquipments != null));
                if (elementWithPredicate == null)
                {
                    throw new Exception($"[MorePrisonerInteractions]: {Hero.OneToOneConversationHero.Name}'s culture ({Hero.OneToOneConversationHero.CharacterObject.Culture.Name}) has no wanderer templates. Please contact the author of this culture instead of MorePrisonerInteractions.");

                }
                Hero.OneToOneConversationHero.CharacterObject.StringId = "MPI" + Hero.OneToOneConversationHero.CharacterObject.StringId;
                typeof(CharacterObject).GetField("_originCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(Hero.OneToOneConversationHero.CharacterObject, elementWithPredicate);


            }

            Hero.OneToOneConversationHero.CharacterObject.StringId = "MPI" + Hero.OneToOneConversationHero.CharacterObject.StringId;
            Hero.OneToOneConversationHero.StringId = Hero.OneToOneConversationHero.CharacterObject.StringId;
            EndCaptivityAction.ApplyByReleasedByChoice(Hero.OneToOneConversationHero, Hero.MainHero);
            AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty);
            if (Hero.OneToOneConversationHero.Occupation == Occupation.Wanderer)
                AddCompanionAction.Apply(Clan.PlayerClan, Hero.OneToOneConversationHero);
            else
                Hero.OneToOneConversationHero.Clan = first.Clan;


            TextObject announcement = new TextObject("{=Announcement_MPI_CompanionsMarried}{HERO_NAME} and {HERO_NAME2} are now married!");
            announcement.SetTextVariable("HERO_NAME", Hero.OneToOneConversationHero.Name);
            announcement.SetTextVariable("HERO_NAME2", first.Name);
            MBInformationManager.AddQuickInformation(announcement, 0, null, "event:/ui/notification/relation");
            Campaign.Current.ConversationManager.ContinueConversation();
        }

        void HandleFailedInquiryMenuForMarriage(List<InquiryElement> inqury)
        {
            Campaign.Current.ConversationManager.ContinueConversation();
            Campaign.Current.ConversationManager.ContinueConversation();
        }

        bool CanForceMarry(out TextObject reason)
        {
            Hero hero = Hero.OneToOneConversationHero;
            if (hero.Spouse != null)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_MarriedAlready}The prisoner is already married to {HERO_NAME}...");
                reason.SetTextVariable("HERO_NAME", hero.Spouse.Name);
                return false;
            }

            int clanMembersCanMarryCount = Clan.PlayerClan.Heroes.Where(x => CanMarry(x, hero)).Count();
            if (clanMembersCanMarryCount == 0)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_NoMarryableClanMembers}You have no clan members that can be married to {CANDIDATE_NAME}!");
                reason.SetTextVariable("CANDIDATE_NAME", hero.Name);
                return false;
            }

            reason = new TextObject();
            return true;
        }

        bool CanMarry(Hero x, Hero hero)
        {
            return Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(x, hero) || ((x.IsWanderer || hero.IsWanderer) && (x.IsFemale != hero.IsFemale));
        }
    }
}
