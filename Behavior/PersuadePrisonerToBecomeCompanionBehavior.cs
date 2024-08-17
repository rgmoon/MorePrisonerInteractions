using System;
using System.Collections.Generic;
using System.Linq;
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
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Encyclopedia;
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.GameComponents;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;
using HarmonyLib;
using MorePrisonerInteractions.Properties;
using System.Reflection;
using TaleWorlds.Engine;
using static TaleWorlds.CampaignSystem.Actions.ChangeRelationAction;


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
        int RelationAdjust = 0;
        List<PersuasionAttempt> _previousConversionPersuasionAttempts;
        int LegalistAttempt, MohistAttempt, Confucianistattempt;
        bool playerleave;
        Random rnd = new Random();
        ThoughtClass Thought = new ThoughtClass();
        GiveGift relationchange = new GiveGift();
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this,OnDailyTick);
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
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_2_continue", "dialog_mpi_persuadeprisoner_2", "dialog_mpi_persuadeprisoner_persuasionQn", "{=Dialog_MPI_PersuadePrisoner_2_CanContinue}I see...what do you have in mind?", () => !HasPersuasionBeenMadeAndCannotContinue() || !CheckIfStillHaveOpt(), ()=>OnConversationCharacacterStartAttemptToConvert(), 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionfailed", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "close_window", "{=!}{FAILED_PERSUASION_LINE}", () => HasPersuasionFailed(), ()=>OnConversationCharacacterFailToConvert(), 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionattempt", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "dialog_mpi_persuadeprisoner_persuasionQn", "{=Persuation_Go_Next}Hmmm...Let's go to next question.", () => PersuasionNextStep(), null, 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionsuccess", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "close_window", "{=Dialog_MPI_PersuadePrisoner_Success}I see. Very well, I will join you!", null, ()=>OnConversationCharacacterSuccessToConvert(), 100, null);

            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_persuasionattempt_start", "dialog_mpi_persuadeprisoner_persuasionQn", "dialog_mpi_persuadeprisoner_player_conversion_argument", "{=!}{PERSUASION_TASK_LINE}", new ConversationSentence.OnConditionDelegate(this.PersuasionConversationDialogLine), null, 100, null);

            gameStarter.AddDialogLine("lord_ask_recruit_argument_reaction", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "dialog_mpi_persuadeprisoner_nextPersuasionQn", "{=!}{PERSUASION_REACTION}", new ConversationSentence.OnConditionDelegate(this.PersuasionGoNext), new ConversationSentence.OnConsequenceDelegate(this.PersuasionGoNextClique), 100, null);

            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_0", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_0}", () => this.PersuasionConversationPlayerLine(0), delegate
            {
                this.PersuasionConversationPlayerLineClique(0,1);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable(0, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(0));

            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_1", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_1}", () => this.PersuasionConversationPlayerLine(1), delegate
            {
                this.PersuasionConversationPlayerLineClique(1,2);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable2(1, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(1));
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_2", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_2}", () => this.PersuasionConversationPlayerLine(2), delegate
            {
                this.PersuasionConversationPlayerLineClique(2,3);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable3(2, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(2));
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_player_conversion_argument_3", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_persuadeprisoner_player_conversion_argument_3_reaction", "{=!}{CONVERSION_PERSUADE_ATTEMPT_3}", () => this.PersuasionConversationPlayerLine(3), delegate
            {
                this.PersuasionConversationPlayerLineClique(3,0);
            }, 100, delegate (out TextObject explanation)
            {
                return this.PersuasionConversationPlayerClickable4(3, out explanation);
            }, () => this.PersuasionConversationPlayerGetOptionArgs(3));
            gameStarter.AddPlayerLine("lord_ask_recruit_argument_no_answer", "dialog_mpi_persuadeprisoner_player_conversion_argument", "dialog_mpi_start", "{=TRY_HARDER_LINE} Well...I will come back later.", new ConversationSentence.OnConditionDelegate(this.PersuasionConversationPlayerLineTryLater), new ConversationSentence.OnConsequenceDelegate(this.OnConversationCharacacterFailToConvert), 100, null, null);
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
            if (hero.PartyBelongedTo != null)hero.PartyBelongedTo.RemoveParty();
            hero.SetNewOccupation(Occupation.Wanderer);
            CharacterObject elementWithPredicate = TaleWorlds.Core.Extensions.GetRandomElementWithPredicate<CharacterObject>(hero.Culture.NotableAndWandererTemplates, (Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && ((BasicCharacterObject)x).IsFemale == ((BasicCharacterObject)hero.CharacterObject).IsFemale && x.CivilianEquipments != null));
            if (elementWithPredicate == null)
            {
                elementWithPredicate = TaleWorlds.Core.Extensions.GetRandomElementWithPredicate<CharacterObject>(hero.Culture.NotableAndWandererTemplates, (Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer  && x.CivilianEquipments != null));
                if (elementWithPredicate == null)
                {
                    throw new Exception($"[MorePrisonerInteractions]: {Hero.OneToOneConversationHero.Name}'s culture ({Hero.OneToOneConversationHero.CharacterObject.Culture.Name}) has no wanderer or lords templates. Please contact the author of this culture instead of MorePrisonerInteractions.");
                }
            }
            hero.CharacterObject.StringId = "MPI" + hero.CharacterObject.StringId;
            hero.StringId = hero.CharacterObject.StringId;
            typeof(CharacterObject).GetField("_originCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hero.CharacterObject, elementWithPredicate);
            AddCompanionAction.Apply(Clan.PlayerClan, hero);
            AddHeroToPartyAction.Apply(hero, MobileParty.MainParty);
            //hero.Clan = Clan.PlayerClan;
            hero.ChangeState(Hero.CharacterStates.Active);
            
        }

        private PersuasionArgumentStrength CalculatePersuationStrength(Hero player, Hero x)
        {
            HeroTraitsGetSet PlayerTrait = new HeroTraitsGetSet(player);
            HeroTraitsGetSet OnetoOneTrait = new HeroTraitsGetSet(x);
            string playerFaction = Thought.Thought(Hero.MainHero);
            string npcFaction = Thought.Thought(Hero.OneToOneConversationHero);

            int traitDifference = Math.Abs(PlayerTrait.Valor - OnetoOneTrait.Valor) + Math.Abs(PlayerTrait.Valor - OnetoOneTrait.Valor) + Math.Abs(PlayerTrait.Honor - OnetoOneTrait.Honor) + Math.Abs(PlayerTrait.Generosity - OnetoOneTrait.Generosity) + Math.Abs(PlayerTrait.Calculating - OnetoOneTrait.Calculating);

            if (!playerFaction.Contains("Not belong any school of thoughts") && playerFaction == npcFaction)
            {
                if (traitDifference <= 1)
                {
                    return PersuasionArgumentStrength.VeryEasy;
                }
                else if (traitDifference <= 3)
                {
                    return PersuasionArgumentStrength.Easy;
                }
                else
                {
                    return PersuasionArgumentStrength.Normal;
                }
            }
            else if (playerFaction.Contains("Legalist") && npcFaction.Contains("Mohist")  || playerFaction.Contains("Mohist") && npcFaction.Contains("Legalist") || playerFaction.Contains("Confucianist") && npcFaction.Contains("Mohist")|| playerFaction.Contains("Mohist") && npcFaction.Contains("Confucianist"))
            {
                if (traitDifference <= 1)
                {
                    return PersuasionArgumentStrength.VeryHard;
                }
                else if (traitDifference <= 3)
                {
                    return PersuasionArgumentStrength.ExtremelyHard;
                }
                else
                {
                    return PersuasionArgumentStrength.ExtremelyHard - 1 ;
                }
            }
            else
            {
                return PersuasionArgumentStrength.Hard;
            }
        }
        private PersuasionArgumentStrength TotalStrength(Hero player, Hero x, int type)
        {
            /*
            type 1 :Legalist
            type 2 :Mohist
            type 3 :Confucianist
            */

            PersuasionArgumentStrength ThoughtStrength = CalculatePersuationStrength(Hero.MainHero, Hero.OneToOneConversationHero);
            PersuasionArgumentStrength RelationStrength;
            PersuasionArgumentStrength TotalStrength;
            string playerFaction = Thought.Thought(Hero.MainHero);
            string npcFaction = Thought.Thought(Hero.OneToOneConversationHero);
            if (player.GetRelation(x) >= 70)
            {
                RelationStrength = (PersuasionArgumentStrength)2;
            }
            else if (player.GetRelation(x) >= 20)
            {
                RelationStrength = (PersuasionArgumentStrength)1;
            }
            else if (player.GetRelation(x) <= -20 && player.GetRelation(x) > -70)
            {
                RelationStrength = (PersuasionArgumentStrength)(-1);
                RelationAdjust = 1;
            }
            else if (player.GetRelation(x) <= -70)
            {
                RelationStrength = (PersuasionArgumentStrength)(-2);
                RelationAdjust = 2;
            }
            else RelationStrength = 0;
            TotalStrength = (int)ThoughtStrength + RelationStrength;
            if (type == 1)
            {
                if (playerFaction.Contains("Legalist") && npcFaction.Contains("Mohist"))
                {
                    TotalStrength += 2;
                }
                else if (playerFaction.Contains("Legalist") && npcFaction.Contains("Legalist"))
                {
                    TotalStrength += 1;
                }
                else if (playerFaction.Contains("Confucianist") && npcFaction.Contains("Legalist"))
                {
                    TotalStrength -= 1;
                }
                else if (playerFaction.Contains("Mohist") && npcFaction.Contains("Legalist"))
                {
                    TotalStrength -= 2;
                }
            }
            else if (type == 2)
            {
                if (playerFaction.Contains("Mohist") && npcFaction.Contains("Confucianist"))
                {
                    TotalStrength += 2;
                }
                else if (playerFaction.Contains("Mohist") && npcFaction.Contains("Mohist"))
                {
                    TotalStrength += 1;
                }
                else if (playerFaction.Contains("Legalist") && npcFaction.Contains("Mohist"))
                {
                    TotalStrength -= 1;
                }
                else if (playerFaction.Contains("Confucianist") && npcFaction.Contains("Mohist"))
                {
                    TotalStrength -= 2;
                }
            }
            else if (type == 3)
            {
                if (playerFaction.Contains("Confucianist") && npcFaction.Contains("Legalist"))
                {
                    TotalStrength += 2;
                }
                else if (playerFaction.Contains("Confucianist") && npcFaction.Contains("Confucianist"))
                {
                    TotalStrength += 1;
                }
                else if (playerFaction.Contains("Mohist") && npcFaction.Contains("Confucianist"))
                {
                    TotalStrength -= 1;
                }
                else if (playerFaction.Contains("Legalist") && npcFaction.Contains("Confucianist"))
                {
                    TotalStrength -= 2;
                }
            }
            else
            {
                TotalStrength = (PersuasionArgumentStrength)rnd.Next(-3,2);
            }
            if ((int)TotalStrength > 3)
            {
                TotalStrength = (PersuasionArgumentStrength)3;
            }
            else if ((int)TotalStrength < -3)
            {
                TotalStrength = (PersuasionArgumentStrength)(-3);
            }
            return TotalStrength;
        }


        private List<PersuasionTask> GetPersuasionTasksForConversion()
        {
            StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, null, false);
            StringHelpers.SetCharacterProperties("INTERLOCUTOR", Hero.OneToOneConversationHero.CharacterObject, null, false);
            List<PersuasionTask> list = new List<PersuasionTask>();

            PersuasionTask persuasionTask = new PersuasionTask(0);

            PersuasionOptionArgs option = new PersuasionOptionArgs(DefaultSkills.Steward, DefaultTraits.Valor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero,1), Thought.CheckIfExpert(Hero.MainHero,1), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Legalist_1}Emphasize laws and order, ensuring everyone follows the rules", null), null, false, true, false);
            persuasionTask.AddOptionToTask(option);

            PersuasionOptionArgs option2 = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Mercy, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero,2), Thought.CheckIfExpert(Hero.MainHero, 2), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Mohist_1}Advocate for universal love and non-aggression, pursuing peace and kindness.", null), null, false, true, false);
            persuasionTask.AddOptionToTask(option2);

            PersuasionOptionArgs option3 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero,3), Thought.CheckIfExpert(Hero.MainHero, 3), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Confucianist_1}Value morality and ethics, cultivating the virtues of the people.", null), null, false, true, false);
            persuasionTask.AddOptionToTask(option3);

            PersuasionOptionArgs option4 = new PersuasionOptionArgs(DefaultSkills.Leadership, DefaultTraits.Generosity, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero,0), false, new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_1}This is a complex issue that requires more consideration and discussion.", null), null, false, true, false);
            persuasionTask.AddOptionToTask(option4);

            list.Add(persuasionTask);

            PersuasionTask persuasionTask2 = new PersuasionTask(1);

            PersuasionOptionArgs option5 = new PersuasionOptionArgs(DefaultSkills.Steward, DefaultTraits.Valor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 1), Thought.CheckIfExpert(Hero.MainHero, 1), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Legalist_2}Discipline and rules, ensuring every member follows the team's regulations.", null), null, false, true, false);
            persuasionTask2.AddOptionToTask(option5);

            PersuasionOptionArgs option6 = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Mercy, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 2), Thought.CheckIfExpert(Hero.MainHero, 2), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Mohist_2}Unity and cooperation, promoting harmony and mutual assistance among members.", null), null, false, true, false);
            persuasionTask2.AddOptionToTask(option6);

            PersuasionOptionArgs option7 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 3), Thought.CheckIfExpert(Hero.MainHero, 3), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Confucianist_2}Morality and example, leading by example and inspiring members to strive for excellence.", null), null, false, true, false);
            persuasionTask2.AddOptionToTask(option7);

            PersuasionOptionArgs option8 = new PersuasionOptionArgs(DefaultSkills.Medicine, DefaultTraits.Generosity, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 0), false, new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_2}This is a complex issue that requires more consideration and discussion.", null), null, false, true, false);
            persuasionTask2.AddOptionToTask(option8);

            list.Add(persuasionTask2);

            PersuasionTask persuasionTask3 = new PersuasionTask(2);

            PersuasionOptionArgs option9 = new PersuasionOptionArgs(DefaultSkills.Steward, DefaultTraits.Valor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 1), Thought.CheckIfExpert(Hero.MainHero, 1), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Legalist_3}Based on laws and rules, ensuring the legality and fairness of the decision.", null), null, false, true, false);
            persuasionTask3.AddOptionToTask(option9);

            PersuasionOptionArgs option10 = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Mercy, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 2), Thought.CheckIfExpert(Hero.MainHero, 2), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Mohist_3}Consider everyone's interests, pursuing the maximization of common good.", null), null, false, true, false);
            persuasionTask3.AddOptionToTask(option10);

            PersuasionOptionArgs option11 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 3), Thought.CheckIfExpert(Hero.MainHero, 3), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Confucianist_3}Based on morality and ethics, making choices that align with moral standards.", null), null, false, true, false);
            persuasionTask3.AddOptionToTask(option11);

            PersuasionOptionArgs option12 = new PersuasionOptionArgs(DefaultSkills.OneHanded, DefaultTraits.Generosity, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 0), false, new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_3}This is a complex issue that requires more consideration and discussion.", null), null, false, true, false);
            persuasionTask3.AddOptionToTask(option12);

            list.Add(persuasionTask3);


            PersuasionTask persuasionTask4 = new PersuasionTask(3);

            PersuasionOptionArgs option13 = new PersuasionOptionArgs(DefaultSkills.Steward, DefaultTraits.Valor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 1), Thought.CheckIfExpert(Hero.MainHero, 1), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Legalist_4}Use laws and rules to resolve conflicts, ensuring justice and order.", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option13);

            PersuasionOptionArgs option14 = new PersuasionOptionArgs(DefaultSkills.Tactics, DefaultTraits.Mercy, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 2), Thought.CheckIfExpert(Hero.MainHero, 2), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Mohist_4}Resolve conflicts through dialogue and negotiation, pursuing peace and understanding.", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option14);

            PersuasionOptionArgs option15 = new PersuasionOptionArgs(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 3), Thought.CheckIfExpert(Hero.MainHero, 3), new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_Confucianist_4}Rely on morality and ethics to resolve conflicts, emphasizing benevolence and tolerance.", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option15);

            PersuasionOptionArgs option16 = new PersuasionOptionArgs(DefaultSkills.Crafting, DefaultTraits.Generosity, TraitEffect.Positive, TotalStrength(Hero.MainHero, Hero.OneToOneConversationHero, 0), false, new TextObject("{=Dialogs_MPI_ConvertLord_First_Question_Reply_4}This is a complex issue that requires more consideration and discussion.", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option16);

            list.Add(persuasionTask4);

            return list;
        }
        private void OnConversationCharacacterStartAttemptToConvert()
        {
            if (Thought.Thought(Hero.MainHero) == "Legalist")
            {
                LegalistAttempt = 2;
                MohistAttempt = 1;
                Confucianistattempt = 1;
            }
            else if (Thought.Thought(Hero.MainHero) == "Mohist")
            {
                LegalistAttempt = 1;
                MohistAttempt = 2;
                Confucianistattempt = 1;
            }
            else if (Thought.Thought(Hero.MainHero) == "Confucianist")
            {
                LegalistAttempt = 1;
                MohistAttempt = 1;
                Confucianistattempt = 2;
            }
            else
            {
                LegalistAttempt = 1;
                MohistAttempt = 1;
                Confucianistattempt = 1;
            }
            RelationAdjust = 0;
            this._allReservations = this.GetPersuasionTasksForConversion();
            this._maximumScoreCap = (float)this._allReservations.Count<PersuasionTask>() * 1f + RelationAdjust;
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
            RemoveConversionPersuasionAttempt(Hero.OneToOneConversationHero);
            this._allReservations = null;
            SuccessXPReward();
            ConversationManager.EndPersuasion();
        }
        private void SuccessXPReward()
        {
            TraitLevelingHelper.OnPersuasionDefection(Hero.OneToOneConversationHero);
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
            foreach (PersuasionAttempt persuasionAttempt in this._previousConversionPersuasionAttempts)
            {
                if (persuasionAttempt.PersuadedHero == Hero.OneToOneConversationHero)
                {
                    PersuasionOptionResult result = persuasionAttempt.Result;
                    if (result != PersuasionOptionResult.Success)
                    {
                        if (result == PersuasionOptionResult.CriticalSuccess)
                        {
                            int num = ((persuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (MathF.Abs((int)persuasionAttempt.Args.ArgumentStrength) * 50) : 50);
                            SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, persuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, 2 * num);
                        }
                    }
                    else
                    {
                        int num = ((persuasionAttempt.Args.ArgumentStrength < PersuasionArgumentStrength.Normal) ? (MathF.Abs((int)persuasionAttempt.Args.ArgumentStrength) * 50) : 50);
                        SkillLevelingManager.OnPersuasionSucceeded(Hero.MainHero, persuasionAttempt.Args.SkillUsed, PersuasionDifficulty.Medium, num);
                    }
                }
            }
            IStatisticsCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<IStatisticsCampaignBehavior>();
            if (behavior != null)
            {
                behavior.OnDefectionPersuasionSucess();
            }
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
        private void OnDailyTick()
        {
            if (_previousConversionPersuasionAttempts != null && _previousConversionPersuasionAttempts.Count > 0)
            {
                for (int i = _previousConversionPersuasionAttempts.Count - 1; i >= 0; i--)
                {
                    PersuasionAttempt attempt = _previousConversionPersuasionAttempts[i];
                    if (attempt.GameTime.ElapsedDaysUntilNow >= 3 && !MobileParty.MainParty.PrisonRoster.Contains(attempt.PersuadedHero.CharacterObject))
                    {
                        _previousConversionPersuasionAttempts.RemoveAt(i);
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


                foreach (PersuasionAttempt attempt in _previousConversionPersuasionAttempts)
                {
                    if (attempt.GameTime.ElapsedDaysUntilNow > 3f && attempt.PersuadedHero == forHero && (attempt.Result == PersuasionOptionResult.Failure || attempt.Result == PersuasionOptionResult.Success) || attempt.GameTime.ElapsedWeeksUntilNow > 2f && attempt.PersuadedHero == forHero && attempt.Result == PersuasionOptionResult.CriticalFailure)
                    {
                        this.RemoveConversionPersuasionAttempt(forHero);
                        return false;
                    }
                }
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
            if (LegalistAttempt >= 1)
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
            if (MohistAttempt >= 1)
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
            if (Confucianistattempt >= 1)
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
            if (currentPersuasionTask.Options.Count > -1)
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
                currentPersuasionTask.FinalFailLine = new TextObject("{=Persuaion_FinalFailLine}You had failed, Now leave me along.");
                MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", currentPersuasionTask.FinalFailLine, false);
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
                return true;
            }
            return false;
        }


        private bool PersuasionConversationDialogLine()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            TextObject reply;
            if (currentPersuasionTask == this._allReservations.Last<PersuasionTask>())
            {
                if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    return false;
                }
            }
            if (!ConversationManager.GetPersuasionProgressSatisfied())
            {
                if (currentPersuasionTask.ReservationType == 0)
                {
                    reply = new TextObject("{=Dialogs_MPI_ConvertLord_TaskLine1}What do you think should be prioritized when governing a country?", null);
                    MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", reply, false);
                }
                if (currentPersuasionTask.ReservationType == 1)
                {
                    reply = new TextObject("{=Dialogs_MPI_ConvertLord_TaskLine2}What do you think is the most important quality in leading a team?", null);
                    MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", reply, false);
                }
                if (currentPersuasionTask.ReservationType == 2)
                {
                    reply = new TextObject("{=Dialogs_MPI_ConvertLord_TaskLine3}How do you think one should make the best choice when facing difficult decisions?", null);
                    MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", reply, false);
                }
                if (currentPersuasionTask.ReservationType == 3)
                {
                    reply = new TextObject("{=Dialogs_MPI_ConvertLord_TaskLine4}What do you think is the most effective way to handle conflicts?", null);
                    MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", reply, false);
                }
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

        private void PersuasionConversationPlayerLineClique(int noOption,int type)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > noOption)
            {
                currentPersuasionTask.Options[noOption].BlockTheOption(true);
            }
            if (type == 1)
            {
                LegalistAttempt--;
            }
            else if (type == 2)
            {
                MohistAttempt--;
            }
            else if (type == 3)
            { 
                Confucianistattempt--;
            }
            else if (type == 0)
            {
                if (rnd.Next(1, 101) > 85)
                {
                relationchange.GiveStaticRelation(Hero.MainHero, Hero.OneToOneConversationHero, 5);
                }
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
            this.GetCurrentPersuasionTask().ImmediateFailLine = new TextObject("{=Persuation_ImmediateFailLine}What the hell are you talking about!? That's non sense!");
            if ((item == PersuasionOptionResult.Failure || item == PersuasionOptionResult.CriticalFailure) && this.GetCurrentPersuasionTask().ImmediateFailLine != null)
            {
                if (item != PersuasionOptionResult.CriticalFailure)
                {
                    MBTextManager.SetTextVariable("PERSUASION_REACTION", "No...I don't think so...", false);
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
                        relationchange.GiveStaticRelation(Hero.MainHero, Hero.OneToOneConversationHero, -5);
                        return true;

                    }
                }
            }
            MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
            return true;
        }
    }
}

