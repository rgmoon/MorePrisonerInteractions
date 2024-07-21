using MoreHeroInteractions.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MorePrisonerInteractions.Behavior
{
    public class DemandRansomBehavior : CampaignBehaviorBase
    {
        Dictionary<Hero, CampaignTime> _ransomTimeline = new Dictionary<Hero, CampaignTime>();
        public override void RegisterEvents()
        {
            //throw new NotImplementedException();
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("MorePrisonerInteractions_RansomTimeline", ref _ransomTimeline);
        }

        public void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_demandransom_start", "dialog_mpi_main_options", "dialog_mpi_demandransom_startreply", "{=Dialog_MPI_DemandRansom_Start}You better start writing a letter to your clan members for them to deliver some denars to me. Otherwise, it's off with your head!", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(CanDemandRansom), null);
            gameStarter.AddDialogLine("dialog_mpi_demandransom_startreply", "dialog_mpi_demandransom_startreply", "dialog_mpi_demandransom_0", "{=Dialog_MPI_DemandRansom_StartReply}W-what?! Please, have mercy![ib:nervous][if:convo_shocked]", null, null, 100, null);
            gameStarter.AddPlayerLine("dialog_mpi_demandransom_0", "dialog_mpi_demandransom_0", "dialog_mpi_demandransom_0_reply", "{=Dialog_MPI_DemandRansom_0}You will have your mercy once I receive my denars!", null, null, 100, null, null);
            gameStarter.AddPlayerLine("dialog_mpi_demandransom_0", "dialog_mpi_demandransom_0", "dialog_mpi_start", "{=Dialog_MPI_DemandRansom_0_Cancel}Calm down, I was only joking...", null, null, 100, null, null);
            gameStarter.AddDialogLine("dialog_mpi_demandransom_0_reply", "dialog_mpi_demandransom_0_reply", "dialog_mpi_start", "{=Dialog_MPI_DemandRansom_0_Reply}Okay, okay! I will write a letter for my clan members to send you some denars...[if:convo_dismayed]", null, ()=> AttemptToDemandRansom(Hero.OneToOneConversationHero), 100, null);
        }

        public void OnHourlyTick()
        {
            Dictionary<Hero, CampaignTime> ransomTimeline = new Dictionary<Hero, CampaignTime>(_ransomTimeline);
            foreach (var timeline in ransomTimeline)
            {
                if (timeline.Value.ElapsedMillisecondsUntilNow >= 0)
                    DeadlineReachedForRansom(timeline.Key);
            }
        }

        bool CanDemandRansom(out TextObject reason)
        {
            // Check whether prisoner has other clan members.
            {
                if (Hero.OneToOneConversationHero.Clan == null)
                {
                    reason = new TextObject("{=Dialogs_MPI_DemandRansom_DisabledReason_NoClan}This prisoner is not part of any clan!");
                    return false;
                }

                if (Hero.OneToOneConversationHero.Clan.Lords.Count == 1)
                {
                    reason = new TextObject("{=Dialogs_MPI_DemandRansom_DisabledReason_NoOtherClanMembers}This prisoner has no other clan members he can write a letter to!");
                    return false;
                }
            }

            // If player has already demanded a ransom for this prisoner
            {
                if (_ransomTimeline.ContainsKey(Hero.OneToOneConversationHero))
                {
                    reason = new TextObject("{=Dialogs_MPI_DemandRansom_DisabledReason_AlreadyDemandedRansom}You have already demanded a ransom for this prisoner. The prisoner's clan members has not gotten back to you!");
                    return false;
                }
            }

            reason = new TextObject();
            return true;
        }


        void AttemptToDemandRansom(Hero hero)
        {
            
            if (!_ransomTimeline.ContainsKey(hero))
            {
                if (MCMSettings.Instance.demandRansomMinDaysToArrive > MCMSettings.Instance.demandRansomMaxDaysToArrive)
                {
                    int temp = MCMSettings.Instance.demandRansomMinDaysToArrive;
                    MCMSettings.Instance.demandRansomMinDaysToArrive = MCMSettings.Instance.demandRansomMaxDaysToArrive;
                    MCMSettings.Instance.demandRansomMaxDaysToArrive = temp;
                }
                Random random = new Random((int)DateTime.Now.Ticks);
                float daysToWait = random.Next(MCMSettings.Instance.demandRansomMinDaysToArrive, MCMSettings.Instance.demandRansomMaxDaysToArrive + 1);
                _ransomTimeline.Add(hero, CampaignTime.DaysFromNow(daysToWait));
            }
        }
        
        void DeadlineReachedForRansom(Hero hero)
        {
            if (_ransomTimeline.ContainsKey(hero)) 
            { 
                _ransomTimeline.Remove(hero);

                bool chooseToPay = new Random().Next(101) <= MCMSettings.Instance.demandRansomChanceOfRansomArriving;

                if (hero.IsDead)
                {
                    if (chooseToPay)
                    {
                        // Ransom was given, but hero is dead!!! Clan members angy
                        if (MCMSettings.Instance.demandRansomPrisonerDeadRansomGivenMinRelationLoss > MCMSettings.Instance.demandRansomPrisonerDeadRansomGivenMaxRelationLoss)
                        {
                            int temp = MCMSettings.Instance.demandRansomPrisonerDeadRansomGivenMinRelationLoss;
                            MCMSettings.Instance.demandRansomPrisonerDeadRansomGivenMinRelationLoss = MCMSettings.Instance.demandRansomPrisonerDeadRansomGivenMaxRelationLoss;
                            MCMSettings.Instance.demandRansomPrisonerDeadRansomGivenMaxRelationLoss = temp;
                        }


                        TextObject desc = new TextObject("{=GameMenu_MPI_RansomArrived_SuccessButPrisonerDead}You have received ransom from {HERO_NAME}'s clan. However {HERO_NAME} is dead and you did not fulfill your end of the ransom agreement. The members of {HERO_NAME}'s clan will not be happy.");
                        desc.SetTextVariable("HERO_NAME", hero.Name);
                        ShowInquiryMenu(desc, hero, chooseToPay);
                        
                    }
                    else
                    {
                        TextObject desc = new TextObject("{=GameMenu_MPI_RansomArrived_FailedAndPrisonerDead}{HERO_NAME}'s clan has refused to pay the ransom. However {HERO_NAME} is dead and you also did not fulfill your end of the ransom agreement. You can consider {HERO_NAME}'s death as payment for the refusal of ransom.");
                        desc.SetTextVariable("HERO_NAME", hero.Name);
                        ShowInquiryMenu(desc, hero, chooseToPay);
                    }
                 
                }
                else
                {
                    if (chooseToPay)
                    {
                        TextObject desc = new TextObject("{=GameMenu_MPI_RansomArrived_SuccessAndPrisonerAlive}You have received ransom from {HERO_NAME}'s clan and also fulfilled your end of the ransom agreement by keeping {HERO_NAME} alive.");
                        desc.SetTextVariable("HERO_NAME", hero.Name);
                        ShowInquiryMenu(desc, hero, chooseToPay);
                    }
                    else
                    {
                        TextObject desc = new TextObject("{=GameMenu_MPI_RansomArrived_FailedButPrisonerAlive}{HERO_NAME}'s clan has refused to pay the ransom even though you fulfilled your end of the ransom agreement! There was no benefit in keeping {HERO_NAME} alive. Maybe it's time to do something against {HERO_NAME}!");
                        desc.SetTextVariable("HERO_NAME", hero.Name);
                        ShowInquiryMenu(desc, hero, chooseToPay);
                    }
                }
                
            }
        }

        void ShowInquiryMenu(TextObject desc, Hero targetHero, bool chooseToPay)
        {
            TextObject title = chooseToPay ? new TextObject("{=GameMenu_MPI_RansomArrived_SuccessTitle}Ransom from {HERO_NAME}'s clan has arrived!") : new TextObject("{=GameMenu_MPI_RansomArrived_FailedTitle}Ransom from {HERO_NAME}'s clan did not arrive!");
            title.SetTextVariable("HERO_NAME", targetHero.Name);


            InformationManager.ShowInquiry(new InquiryData(title.ToString(), desc.ToString(), true, false, new TextObject("{=GameMenu_MPI_RansomArrived_ConfirmButton}Okay").ToString(), "", () => { if (chooseToPay) GiveRansomToPlayer(targetHero); }, null));
        }

        void GiveRansomToPlayer(Hero hero)
        {
            int ransomAmount = Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(hero.CharacterObject, Hero.MainHero);
            Hero giverHero = hero.Clan == null ? hero.Clan.Leader : null;
            GiveGoldAction.ApplyBetweenCharacters(giverHero, Hero.MainHero, ransomAmount, true);
            TextObject announcement = new TextObject("{=Announcement_MPI_RansomGiven}You have received {AMOUNT} denars as ransom payment to keep {HERO_NAME} alive.");
            announcement.SetTextVariable("AMOUNT", ransomAmount);
            announcement.SetTextVariable("HERO_NAME", hero.Name);
            InformationManager.DisplayMessage(new InformationMessage(announcement.ToString()));
        }
    }
}
