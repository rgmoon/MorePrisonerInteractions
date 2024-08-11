using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Issues;
using System.Reflection;

namespace MorePrisonerInteractions.Behavior
{
    public class PersuadePrisonerToBecomeCompanionBehavior : CampaignBehaviorBase
    {

        List<PersuasionTask> _allReservations;
        float _maximumScoreCap;
        float _successValue = 1f;
        float _failValue = 1f;
        float _criticalSuccessValue = 2f;
        float _criticalFailValue = 2f;
        List<PersuasionAttempt> _previousConversionPersuasionAttempts;
        bool playerleave;
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
        }

        public override void SyncData(IDataStore dataStore)
        {
           dataStore.SyncData("MorePrisonerInteractions_PersuadePrisonerToLord", ref this._previousConversionPersuasionAttempts);
        }


        public void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_start", "dialog_mpi_main_options", "dialog_mpi_persuadeprisoner_startreply", "{=Dialog_MPI_PersuadePrisoner_Start}I want you to reconsider your allegiance.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(CanConvertToCompanion), null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_startreply", "dialog_mpi_persuadeprisoner_startreply", "dialog_mpi_persuadeprisoner_1", "{=Dialog_MPI_PersuadePrisoner_StartReply}What do you mean?", null, null, 100, null);
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_1_continue", "dialog_mpi_persuadeprisoner_1", "dialog_mpi_persuadeprisoner_2", "{=Dialog_MPI_PersuadePrisoner_1_Continue}I want you to reconsider who you are loyal to.", null, null, 100,null, null);
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_1_cancel", "dialog_mpi_persuadeprisoner_1", "dialog_mpi_start", "{=Dialog_MPI_PersuadePrisoner_1_Cancel}Nothing.", null, null, 100,null, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_2_denied", "dialog_mpi_persuadeprisoner_2", "dialog_mpi_start", "{=Dialog_MPI_PersuadePrisoner_2_AlreadyAskedBefore}I have already given you my answer, no.", () => HasPersuasionBeenMadeAndCannotContinue(), null, 100, null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_2_continue", "dialog_mpi_persuadeprisoner_2", "dialog_mpi_persuadeprisoner_persuasionQn", "{=Dialog_MPI_PersuadePrisoner_2_CanContinue}I see...what do you have in mind?", () => !HasPersuasionBeenMadeAndCannotContinue(), ()=>OnConversationCharacacterStartAttemptToConvert(), 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_2_continue", "dialog_mpi_persuadeprisoner_2", "dialog_mpi_persuadeprisoner_persuasionQn", "{=Dialog_MPI_PersuadePrisoner_2_CanContinue}I see...what do you have in mind?", () => !CheckIfStillHaveOpt(), null, 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionfailed", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "dialog_mpi_start", "{=!}{FAILED_PERSUASION_LINE}", () => HasPersuasionFailed(), ()=>OnConversationCharacacterFailToConvert(), 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionattempt", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "dialog_mpi_persuadeprisoner_persuasionQn", "{=!}{PERSUASION_TASK_LINE}", () => PersuasionNextStep(), null, 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionsuccess", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "close_window", "{=Dialog_MPI_PersuadePrisoner_Success}I see. Very well, I will join you!", null, ()=>OnConversationCharacacterSuccessToConvert(), 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionattempt_start", "dialog_mpi_persuadeprisoner_persuasionQn", "dialog_mpi_persuadeprisoner_player_conversion_argument", "{=!}{PERSUASION_TASK_LINE}", new ConversationSentence.OnConditionDelegate(this.PersuasionConversationDialogLine), null, 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionattempt_start_2", "dialog_mpi_persuadeprisoner_persuasionQn", "dialog_mpi_persuadeprisoner_player_conversion_argument_2_1", "{=!}{PERSUASION_TASK_LINE}", new ConversationSentence.OnConditionDelegate(this.PersuasionConversationDialogLine), null, 100, null);


            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_0", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_0}", () => this.PersuasionConversationPlayerLine(0), delegate
            {
                this.PersuasionConversationPlayerLineClique(0);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(0, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(0));

            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_1", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_1}", () => this.PersuasionConversationPlayerLine(1), delegate
            {
                this.PersuasionConversationPlayerLineClique(1);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(1, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(1));
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_2", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_2}", () => this.PersuasionConversationPlayerLine(2), delegate
            {
                this.PersuasionConversationPlayerLineClique(2);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(2, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(2));
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_3", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_3}", () => this.PersuasionConversationPlayerLine(3), delegate
            {
                this.PersuasionConversationPlayerLineClique(3);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(3, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(3));

            
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_4", "dialog_mpi_persuadeprisoner_player_conversion_argument_2_1", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_4}", () => this.PersuasionConversationPlayerLine(4), delegate
            {
                this.PersuasionConversationPlayerLineClique(0);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(0, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(0));

            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_5", "dialog_mpi_persuadeprisoner_player_conversion_argument_2_1", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_5}", () => this.PersuasionConversationPlayerLine(5), delegate
            {
                this.PersuasionConversationPlayerLineClique(1);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(1, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(1));

            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_6", "dialog_mpi_persuadeprisoner_player_conversion_argument_2_1", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_6}", () => this.PersuasionConversationPlayerLine(6), delegate
            {
                this.PersuasionConversationPlayerLineClique(2);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(2, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(2));

            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_7", "dialog_mpi_persuadeprisoner_player_conversion_argument_2_1", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_7}", () => this.PersuasionConversationPlayerLine(7), delegate
            {
                this.PersuasionConversationPlayerLineClique(3);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(3, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(3));
            
            gameStarter.AddPlayerLine("lord_ask_recruit_argument_no_answer", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_start", "{=!}{TRY_HARDER_LINE}", new ConversationSentence.OnConditionDelegate(this.PersuasionConversationPlayerLineTryLater), new ConversationSentence.OnConsequenceDelegate(this.OnConversationCharacacterFailToConvert), 100, null, null);

            gameStarter.AddDialogLine("lord_ask_recruit_argument_failed", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "Leaving_Reaction", "{=!}{PERSUASION_REACTION}", () => HasPersuasionFailed(),null, 99, null);

            gameStarter.AddDialogLine("lord_ask_recruit_argument_failedNLeave", "Leaving_Reaction", "close_window", "{=Dialog_MPI_PersuadePrisoner_CriticalFailed}Now leave me along.", null, () => OnConversationCharacacterFailToConvert(), 100, null);

            gameStarter.AddDialogLine("lord_ask_recruit_argument_reaction", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "{=!}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.PersuasionGoNext), new ConversationSentence.OnConsequenceDelegate(this.PersuasionGoNextClique), 100, null);
        }

        bool CanConvertToCompanion(out TextObject reason)
        {
            if (Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
            {
                reason = new TextObject("{=Dialogs_MPI_ConvertLord_DisabledReason_CompanionLimit}You have reached the companion limit!");
                return false;
            }

            if (Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan.Kingdom != null && Hero.OneToOneConversationHero.Clan.Kingdom.Leader == Hero.OneToOneConversationHero)
            {
                reason = new TextObject("{=Dialogs_MPI_ConvertLord_DisabledReason_LeaderOfKingdom}You cannot convert a leader of a kingdom to a companion!");
                return false;
            }

            if (Hero.OneToOneConversationHero.Clan != null && Hero.OneToOneConversationHero.Clan.Leader == Hero.OneToOneConversationHero)
            {
                reason = new TextObject("{=Dialogs_MPI_ConvertLord_DisabledReason_LeaderOfClan}You cannot convert a leader of a clan to a companion!");
                return false;
            }

            reason = new TextObject();
            return true;
        }

        void ConvertPrisonerToCompanion(Hero hero)
        {
            EndCaptivityAction.ApplyByReleasedByChoice(hero, Hero.MainHero);
            if (hero.PartyBelongedTo != null)
                hero.PartyBelongedTo.RemoveParty();
        


            hero.SetNewOccupation(Occupation.Wanderer);
            CharacterObject elementWithPredicate = TaleWorlds.Core.Extensions.GetRandomElementWithPredicate<CharacterObject>(hero.Culture.NotableAndWandererTemplates, (Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && ((BasicCharacterObject)x).IsFemale == ((BasicCharacterObject)hero.CharacterObject).IsFemale && x.CivilianEquipments != null));
            if (elementWithPredicate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Cannot find an available wanderer template to assign."));
                throw new Exception($"[MorePrisonerInteractions]: {Hero.OneToOneConversationHero.Name}'s culture ({Hero.OneToOneConversationHero.CharacterObject.Culture.Name}) has no wanderer templates. Please contact the author of this culture instead of MorePrisonerInteractions.");

            }
            hero.CharacterObject.StringId = "MPI" + hero.CharacterObject.StringId;
            hero.StringId = hero.CharacterObject.StringId;
            typeof(CharacterObject).GetField("_originCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hero.CharacterObject, elementWithPredicate);
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            //hero.Clan = Clan.PlayerClan;
            hero.ChangeState(Hero.CharacterStates.Active);
            
        }

   
        private Tuple<TraitObject, int>[] GetTraitCorrelations(int valor = 0, int mercy = 0, int honor = 0, int generosity = 0, int calculating = 0)
        {
            return new Tuple<TraitObject, int>[]
            {
                new Tuple<TraitObject, int>(DefaultTraits.Valor, valor),
                new Tuple<TraitObject, int>(DefaultTraits.Mercy, mercy),
                new Tuple<TraitObject, int>(DefaultTraits.Honor, honor),
                new Tuple<TraitObject, int>(DefaultTraits.Generosity, generosity),
                new Tuple<TraitObject, int>(DefaultTraits.Calculating, calculating)
            };
        }

        private List<PersuasionTask> GetPersuasionTasksForConversion()
        {
            StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
            StringHelpers.SetCharacterProperties("INTERLOCUTOR", Hero.OneToOneConversationHero.CharacterObject, null, false);
            List<PersuasionTask> list = new List<PersuasionTask>();

            PersuasionTask persuasionTask = new PersuasionTask(0);

            Tuple<TraitObject, int>[] traitCorrelations = this.GetTraitCorrelations(1, -1, 0, 1, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations);
            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits, false, new TextObject("{=Dialogs_MPI_ConvertLord_LeaderChoice}Your leader is not worthy of you. Pledge your loyalty to me and I will lead you to higher places.", null), traitCorrelations, false, true, false);
            persuasionTask.AddOptionToTask(option);

            Tuple<TraitObject, int>[] traitCorrelations2 = this.GetTraitCorrelations(1, 0, 0, -1, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits2 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations2);
            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits2, false, new TextObject("{=Dialogs_MPI_ConvertLord_CalculateChoice}I am sure you do not like the prisoner treatment. Pledging your loyalty to me will be your best option right now.", null), traitCorrelations2, false, true, false);
            persuasionTask.AddOptionToTask(option2);

            Tuple<TraitObject, int>[] traitCorrelations3 = this.GetTraitCorrelations(0, 1, 1, 0, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits3 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations3);
            PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits3, false, new TextObject("{=Dialogs_MPI_ConvertLord_MercyChoice}I cannot let {?INTERLOCUTOR.GENDER}a beautiful woman{?}a handsome young man{\\?} such as yourself to serve an incompetent leader! Serve me and I'll protect you.", null), traitCorrelations3, false, true, false);
            persuasionTask.AddOptionToTask(option3);

            Tuple<TraitObject, int>[] traitCorrelations4 = this.GetTraitCorrelations(0, -1, -1, -1, 0);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits4 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations4);
            PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Negative, argumentStrengthBasedOnTargetTraits4, false, new TextObject("{=Dialogs_MPI_ConvertLord_GenerosityCharm}It is a beautiful day outside, and {?INTERLOCUTOR.GENDER}an adventurous woman{?}an adventurous man{\\?} like you needs to be free to see it. Serve me instead and you shall be free.", null), traitCorrelations4, false, true, false);
            persuasionTask.AddOptionToTask(option4);

            list.Add(persuasionTask);

            PersuasionTask persuasionTask2 = new PersuasionTask(1);

            Tuple<TraitObject, int>[] traitCorrelations5 = this.GetTraitCorrelations(1, 0, -1, 1, 0);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits5 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations5);
            PersuasionOptionArgs option5 = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits5, false, new TextObject("{=Dialogs_MPI_ConvertLord_TacticsValor}Your valor is wasted under your current leader. Join me, and we will outmaneuver our enemies together", null), traitCorrelations5, false, true, false);
            persuasionTask2.AddOptionToTask(option5);

            Tuple<TraitObject, int>[] traitCorrelations6 = this.GetTraitCorrelations(0, 1, 0, -1, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits6 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations6);
            PersuasionOptionArgs option6 = new PersuasionOptionArgs(DefaultSkills.Trade, DefaultTraits.Mercy, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits6, false, new TextObject("{=Dialogs_MPI_ConvertLord_TradeMercy}Your mercy can bring prosperity. Serve me, and we will create a thriving community together.", null), traitCorrelations6, false, true, false);
            persuasionTask2.AddOptionToTask(option6);

            Tuple<TraitObject, int>[] traitCorrelations7 = this.GetTraitCorrelations(-1, 0, 1, 1, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits7 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations7);
            PersuasionOptionArgs option7 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Honor, TraitEffect.Negative, argumentStrengthBasedOnTargetTraits7, false, new TextObject("{=Dialogs_MPI_ConvertLord_EngineeringHonor}Your honor deserves a leader who values it. Join me, and we will build wonders together.", null), traitCorrelations7, false, true, false);
            persuasionTask2.AddOptionToTask(option7);

            Tuple<TraitObject, int>[] traitCorrelations8 = this.GetTraitCorrelations(0, -1, 0, 1, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits8 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations8);
            PersuasionOptionArgs option8 = new PersuasionOptionArgs(DefaultSkills.Medicine, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits8, false, new TextObject("{=Dialogs_MPI_ConvertLord_MedicineGenerosity}Your generosity can heal many. Serve me, and we will bring health and hope to the people.", null), traitCorrelations8, false, true, false);
            persuasionTask2.AddOptionToTask(option8);

            list.Add(persuasionTask2);

            PersuasionTask persuasionTask3 = new PersuasionTask(2);

            Tuple<TraitObject, int>[] traitCorrelations9 = this.GetTraitCorrelations(1, 0, -1, 1, 0);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits9 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations9);
            PersuasionOptionArgs option9 = new PersuasionOptionArgs(DefaultSkills.Bow, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits9, false, new TextObject("{=Dialogs_MPI_ConvertLord_BowValor}Your valor is needed on the battlefield. Join me, and we will strike down our enemies from afar", null), traitCorrelations9, false, true, false);
            persuasionTask3.AddOptionToTask(option9);

            Tuple<TraitObject, int>[] traitCorrelations10 = this.GetTraitCorrelations(0, 1, -1, 0, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits10 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations10);
            PersuasionOptionArgs option10 = new PersuasionOptionArgs(DefaultSkills.Crossbow, DefaultTraits.Mercy, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits10, false, new TextObject("{=Dialogs_MPI_ConvertLord_CrossBowMercy}Your mercy can protect the innocent. Serve me, and we will defend the weak together.", null), traitCorrelations10, false, true, false);
            persuasionTask3.AddOptionToTask(option10);

            Tuple<TraitObject, int>[] traitCorrelations11 = this.GetTraitCorrelations(-1, 0, 1, 1, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits11 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations11);
            PersuasionOptionArgs option11 = new PersuasionOptionArgs(DefaultSkills.Athletics, DefaultTraits.Honor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits11, false, new TextObject("{=Dialogs_MPI_ConvertLord_AthleticsHonor}Your honor deserves a leader who values it. Join me, and we will achieve great feats together", null), traitCorrelations11, false, true, false);
            persuasionTask3.AddOptionToTask(option11);

            Tuple<TraitObject, int>[] traitCorrelations12 = this.GetTraitCorrelations(0, -1, 0, 1, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits12 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations12);
            PersuasionOptionArgs option12 = new PersuasionOptionArgs(DefaultSkills.OneHanded, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits12, false, new TextObject("{=Dialogs_MPI_ConvertLord_OneHandedGenerosity}Your generosity can inspire others. Serve me, and we will lead by example.", null), traitCorrelations12, false, true, false);
            persuasionTask3.AddOptionToTask(option12);

            list.Add(persuasionTask3);


            PersuasionTask persuasionTask4 = new PersuasionTask(3);

            Tuple<TraitObject, int>[] traitCorrelations13 = this.GetTraitCorrelations(1, -1, 0, 1, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits13 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations13);
            PersuasionOptionArgs option13 = new PersuasionOptionArgs(DefaultSkills.Engineering, DefaultTraits.Valor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits13, false, new TextObject("{=Dialogs_MPI_ConvertLord_EngineeringValor}Your valor is needed for greater challenges. Join me, and we will build a legacy together.", null), traitCorrelations13, false, true, false);
            persuasionTask4.AddOptionToTask(option13);

            Tuple<TraitObject, int>[] traitCorrelations14 = this.GetTraitCorrelations(0, 1, -1, 0, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits14 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations14);
            PersuasionOptionArgs option14 = new PersuasionOptionArgs(DefaultSkills.Medicine, DefaultTraits.Mercy, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits14, false, new TextObject("{=Dialogs_MPI_ConvertLord_MedicineMercy}Your mercy can save many lives. Serve me, and we will heal the wounded and protect the weak.", null), traitCorrelations14, false, true, false);
            persuasionTask4.AddOptionToTask(option14);

            Tuple<TraitObject, int>[] traitCorrelations15 = this.GetTraitCorrelations(-1, 0, 1, -1, 1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits15 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations15);
            PersuasionOptionArgs option15 = new PersuasionOptionArgs(DefaultSkills.Scouting, DefaultTraits.Honor, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits15, false, new TextObject("{=Dialogs_MPI_ConvertLord_ScoutingHonor}Your honor deserves a leader who values it. Join me, and we will explore new horizons together.", null), traitCorrelations15, false, true, false);
            persuasionTask4.AddOptionToTask(option15);

            Tuple<TraitObject, int>[] traitCorrelations16 = this.GetTraitCorrelations(0, -1, 0, 1, -1);
            PersuasionArgumentStrength argumentStrengthBasedOnTargetTraits16 = Campaign.Current.Models.PersuasionModel.GetArgumentStrengthBasedOnTargetTraits(CharacterObject.OneToOneConversationCharacter, traitCorrelations16);
            PersuasionOptionArgs option16 = new PersuasionOptionArgs(DefaultSkills.Crafting, DefaultTraits.Generosity, TraitEffect.Positive, argumentStrengthBasedOnTargetTraits16, false, new TextObject("{=Dialogs_MPI_ConvertLord_CraftingGenerosity}Your generosity can create wonders. Serve me, and we will craft a better future together", null), traitCorrelations16, false, true, false);
            persuasionTask4.AddOptionToTask(option16);

            list.Add(persuasionTask4);


            foreach (PersuasionTask AllTask in list)
            {
                AllTask.FinalFailLine = new TextObject("{=Dialogs_MPI_ConvertLord_FailLine}You cannot convince me. I am not interested.", null);
                AllTask.TryLaterLine = new TextObject("{=Dialogs_MPI_ConvertLord_TryLater}I have no idea what to say. I will come back a later time.", null);
                AllTask.SpokenLine = new TextObject("{=Dialogs_MPI_ConvertLord_SpokenLine}What do you have in mind?", null);
            }
            return list;
        }

        private void OnConversationCharacacterStartAttemptToConvert()
        {
            this._allReservations = this.GetPersuasionTasksForConversion();
            this._maximumScoreCap = (float)this._allReservations.Count<PersuasionTask>() * 1f;
            float initialProgress = 0f;
            ConversationManager.StartPersuasion(this._maximumScoreCap, this._successValue, this._failValue, this._criticalSuccessValue, this._criticalFailValue, initialProgress, PersuasionDifficulty.Impossible);
        }

        private void OnConversationCharacacterFailToConvert()
        {
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
            this._allReservations = null;
            ConversationManager.EndPersuasion();
        }

        private void OnConversationCharacacterSuccessToConvert()
        {
            ConvertPrisonerToCompanion(Hero.OneToOneConversationHero);
            this._allReservations = null;
            ConversationManager.EndPersuasion();
        }

        private void RemoveConversionPersuasionAttempt(Hero forHero)
        {
            if (this._previousConversionPersuasionAttempts != null)
            {
                List<PersuasionAttempt> previousPersuasionAttempts = new List<PersuasionAttempt>(_previousConversionPersuasionAttempts);
                foreach (PersuasionAttempt attempt in previousPersuasionAttempts)
                {
                    if (attempt.PersuadedHero == forHero)
                    {
                        this._previousConversionPersuasionAttempts.Remove(attempt);
                        break;
                    }
                }
            }
        }

        private bool HasPersuasionBeenMadeAndCannotContinue()
        {
            Hero forHero = Hero.OneToOneConversationHero;
            if (this._previousConversionPersuasionAttempts != null)
            {
                PersuasionAttempt persuasionAttempt = this._previousConversionPersuasionAttempts.LastOrDefault((PersuasionAttempt x) => x.PersuadedHero == forHero);
                if (persuasionAttempt == null)
                    return false;

                PersuasionAttempt persuasionAttemptExpired = this._previousConversionPersuasionAttempts.LastOrDefault((PersuasionAttempt x) => x.PersuadedHero == forHero && ((x.Result == PersuasionOptionResult.CriticalFailure && x.GameTime.ElapsedWeeksUntilNow > 2f) || (x.Result == PersuasionOptionResult.Failure && x.GameTime.ElapsedDaysUntilNow > 1f) || (x.Result == PersuasionOptionResult.Success && x.GameTime.ElapsedDaysUntilNow > 1f)));

                bool flag = persuasionAttemptExpired != null;
                if (flag)
                {
                    this.RemoveConversionPersuasionAttempt(forHero);
                    return false;
                }
                else
                    return true;
            }
            return false;
        }
        private bool CheckIfStillHaveOpt()
        {
            try
            {
                PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
                if (currentPersuasionTask.Options.Count > 0)
                {
                    TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                    textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(0), true));
                    textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(0).Line);
                    MBTextManager.SetTextVariable("DEFECTION_PERSUADE_ATTEMPT_1", textObject, false);
                    return true;
                }
                return false;
            }
            catch {
                OnConversationCharacacterStartAttemptToConvert();
            }
            return false;

        }
        private PersuasionTask GetCurrentPersuasionTask()
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                if (!persuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    return persuasionTask;
                }
            }
            return this._allReservations.Last<PersuasionTask>();
        }

        private PersuasionTask FindTaskOfOption(PersuasionOptionArgs optionChosenWithLine)
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                using (List<PersuasionOptionArgs>.Enumerator enumerator2 = persuasionTask.Options.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        if (enumerator2.Current.Line == optionChosenWithLine.Line)
                        {
                            return persuasionTask;
                        }
                    }
                }
            }
            return null;
        }
      

        private bool PersuasionConversationPlayerClickable(int noOption, out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                return !currentPersuasionTask.Options.ElementAt(0).IsBlocked;
            }
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }
        private bool PersuasionConversationPlayerClickable2(int noOption, out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                return !currentPersuasionTask.Options.ElementAt(1).IsBlocked;
            }
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }
        private bool PersuasionConversationPlayerClickable3(int noOption, out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                return !currentPersuasionTask.Options.ElementAt(2).IsBlocked;
            }
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }
        private bool PersuasionConversationPlayerClickable4(int noOption, out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                return !currentPersuasionTask.Options.ElementAt(3).IsBlocked;
            }
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }

        private PersuasionOptionArgs PersuasionConversationPlayerGetOptionArgs(int noOption)
        {
            return this.GetCurrentPersuasionTask().Options.ElementAt(noOption);
        }

  
        private bool HasPersuasionFailed()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
            {
                MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", currentPersuasionTask.ImmediateFailLine, false);
                return true;
            }
            return false;
        }


        private bool PersuasionNextStep()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask == this._allReservations[this._allReservations.Count - 1])
            {
                if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", currentPersuasionTask.FinalFailLine, false);
                    return false;
                }
            }

            if (!ConversationManager.GetPersuasionProgressSatisfied())
            {
                MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine, false);
                return true;
            }
            return false;
        }


        private bool PersuasionConversationDialogLine()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask == this._allReservations.Last<PersuasionTask>())
            {
                if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    return false;
                }
            }
            if (!ConversationManager.GetPersuasionProgressSatisfied())
            {
                MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine, false);
                return true;
            }
            return false;
        }

        private bool PersuasionConversationPlayerLine(int noOption)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count<PersuasionOptionArgs>() > noOption)
            {
                TextObject textObject = new TextObject("{=bSo9hKwr}{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(noOption), true));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(noOption).Line);
                MBTextManager.SetTextVariable("CONVERSION_PERSUADE_ATTEMPT_" + noOption.ToString(), textObject, false);
                return true;
            }
            return false;
        }

        private void PersuasionConversationPlayerLineClique(int noOption)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                currentPersuasionTask.Options[noOption].BlockTheOption(true);
            }
        }

        private bool PersuasionConversationPlayerLineTryLater()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            MBTextManager.SetTextVariable("TRY_HARDER_LINE", currentPersuasionTask.TryLaterLine, false);
            return true;
        }

        private void PersuasionGoNextClique()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();
            float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Impossible);
            float moveToNextStageChance;
            float blockRandomOptionChance;
            Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out moveToNextStageChance, out blockRandomOptionChance, difficulty);
            this.FindTaskOfOption(tuple.Item1).ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
            PersuasionAttempt item = new PersuasionAttempt(Hero.OneToOneConversationHero, CampaignTime.Now, tuple.Item1, tuple.Item2, currentPersuasionTask.ReservationType);
            if (this._previousConversionPersuasionAttempts == null)
            {
                this._previousConversionPersuasionAttempts = new List<PersuasionAttempt>();
            }
            this._previousConversionPersuasionAttempts.Add(item);
        }

        private bool PersuasionGoNext()
        {
            PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
            this.GetCurrentPersuasionTask().ImmediateFailLine = new TextObject("{=Dialogs_MPI_ConvertLord_ImmediateFailLine}You Had failed. Now get the fuck out of here.");
            if ((item == PersuasionOptionResult.Failure || item == PersuasionOptionResult.CriticalFailure) && this.GetCurrentPersuasionTask().ImmediateFailLine != null)
            {
                if (item != PersuasionOptionResult.CriticalFailure)
                {
                    MBTextManager.SetTextVariable("PERSUASION_REACTION", this.GetCurrentPersuasionTask().FinalFailLine, false);
                    return true;
                }
                else
                {
                    MBTextManager.SetTextVariable("PERSUASION_REACTION", this.GetCurrentPersuasionTask().ImmediateFailLine, false);
                    using (List<PersuasionTask>.Enumerator enumerator = this._allReservations.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            PersuasionTask persuasionTask = enumerator.Current;
                            persuasionTask.BlockAllOptions();
                        }
                        return true;

                    }
                }
            }
            MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
            return true;
        }
    }
}
