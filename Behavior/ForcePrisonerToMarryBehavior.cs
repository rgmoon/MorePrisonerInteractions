using System;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Engine;
using TaleWorlds.SaveSystem;
using System.IO;
using MoreHeroInteractions.Settings;
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;
using static TaleWorlds.CampaignSystem.CampaignTime;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using System.Runtime.Remoting.Messaging;
namespace MorePrisonerInteractions.Behavior
{
    public class ForcePrisonerToMarryBehavior : CampaignBehaviorBase
        {
        
        Dictionary<Hero, bool> PaidBefore = new Dictionary<Hero, bool>();
        Dictionary<Hero, BridePriceInfo> BriceData = new Dictionary<Hero, BridePriceInfo>();
        private CampaignTime LastConsiderTime = CampaignTime.Never;
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
        }

        private void OnHeroPrisonerReleased(Hero hero, PartyBase @base, IFaction faction, EndCaptivityDetail detail)
        {
            if (BriceData.ContainsKey(hero))
            {
                BriceData.Remove(hero);
                PaidBefore.Remove(hero);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
                dataStore.SyncData("PaidBefore", ref PaidBefore);
                dataStore.SyncData("BriceData", ref BriceData);
        }
        public bool HasPaidBridePrice(Hero hero)
        {
            if (!PaidBefore.ContainsKey(hero))
            {
                PaidBefore[hero] = false; // 初始化為 false
            }
            return PaidBefore[hero];
        }
        void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_unmarried_0", "dialog_mpi_main_options", "dialog_mpi_forcemarriage_unmarried_1", "{=Dialog_MPI_ForceMarriage_Unmarried_Start}I want you to marry one of my clan members! In exchange, I will set you free from your current chains.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(CanForceMarry));
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_1", "dialog_mpi_forcemarriage_unmarried_1", "dialog_mpi_forcemarriage_unmarried_1_1", "{=!}{Dialog_MPI_ForceMarriage_LordAskforDenierOrNot}", () => !HasPaidBridePrice(Hero.OneToOneConversationHero)&&RelationShip(), () => BridePrice());
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_2", "dialog_mpi_forcemarriage_unmarried_1", "dialog_mpi_forcemarriage_unmarried_2", "I remember you...Now Who should I marry ?", ()=>HasPaidBridePrice(Hero.OneToOneConversationHero), null);

            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_1", "dialog_mpi_forcemarriage_bad_relation", "You want me join your pathetic clan? I would rather die.", null, null);

            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_execute", "dialog_mpi_forcemarriage_bad_relation", "close_window", "As you wish. Guards! bring {?INTERLOCUTOR.GENDER}she{?}he{\\?} to the guillotine! ", null, ()=> ExecuteLords(Hero.OneToOneConversationHero), 100);
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_letgo", "dialog_mpi_forcemarriage_bad_relation", "close_window", "Don't worry. You will get it very soon. I promise.", null, null, 100);


            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_CanGiveGold", "dialog_mpi_forcemarriage_unmarried_1_1", "dialog_mpi_forcemarriage_unmarried_2", "{=Dialog_MPI_ForceMarriage_GiveDenier}Fine.Here's Your Denier", null, ()=>GiveDenier(), 100, new ConversationSentence.OnClickableConditionDelegate(BrokeOrNot));
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_ConsiderAgain", "dialog_mpi_forcemarriage_unmarried_1_1", "dialog_mpi_forcemarriage_ConsiderAgain", "{=Dialog_MPI_ForceMarriage_ConsiderAgain}You sure about that? Don't you see how strong am I now? I advise you consider again!", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(ConsiderCondition));

            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_ConsiderAgain_2", "dialog_mpi_forcemarriage_ConsiderAgain", "close_window", "{=!}{Dialog_MPI_ForceMarriage_ConsiderAgainSpeak}Hmm...I will Consider That... Come back to me later", () => IsCanConsiderAgain(), ()=> ConsiderFunction());
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_ConsiderAgain_2", "dialog_mpi_forcemarriage_ConsiderAgain", "dialog_mpi_main_options", "{=!}{Dialog_MPI_ForceMarriage_ConsiderAgainSpeak}I already give you answer. NO!", null, null);
            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_CanNotGiveGold", "dialog_mpi_forcemarriage_unmarried_1_1", "close_window", "{=Dialog_MPI_ForceMarriage_CantGive}Nevermind.", null, null, 100);

            gameStarter.AddPlayerLine("dialog_mpi_forcemarriage_unmarried_2", "dialog_mpi_forcemarriage_unmarried_2", "dialog_mpi_forcemarriage_unmarried_3", "{=Dialog_MPI_ForceMarriage_Unmarried_AskWhoReply}Let's see...", null, () => OpenInquiryMenuForMarryableMembers());

            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_4", "...", null, null);
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_4", "dialog_mpi_forcemarriage_unmarried_5", "{=Dialog_MPI_ForceMarriage_married_Finished}I promise I'll treat {?INTERLOCUTOR.GENDER}him{?}her{\\?} as well as I can.", ()=>MarriedOrNot(Hero.OneToOneConversationHero), null);
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_4", "dialog_mpi_forcemarriage_unmarried_5", "{=Dialog_MPI_ForceMarriage_Unmarried_Finished}Oh well. I think I'll waiting for you until you found a good person for me.", null, null);
            gameStarter.AddDialogLine("dialog_mpi_forcemarriage_unmarried_3", "dialog_mpi_forcemarriage_unmarried_5", "dialog_mpi_main_options", "{=Dialog_MPI_ForceMarriage_Unmarried_Finished}So. What now?", null, null);


        }

        void OpenInquiryMenuForMarryableMembers()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            foreach (Hero hero in Clan.PlayerClan.Heroes)
            {
                if (CanMarry(hero, Hero.OneToOneConversationHero) && hero.IsAlive && hero.Clan == Clan.PlayerClan)
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

            if (first != null && Hero.OneToOneConversationHero != null)
            {
                if (CanMarry(first, Hero.OneToOneConversationHero))
                {
                    MarriageAction.Apply(first, Hero.OneToOneConversationHero);
                    Hero.OneToOneConversationHero.Spouse = first;
                    first.Spouse = Hero.OneToOneConversationHero;
                    Hero.OneToOneConversationHero.SetNewOccupation(first.Occupation);

                    if (Hero.OneToOneConversationHero.Occupation == Occupation.Wanderer)
                    {
                        CharacterObject elementWithPredicate = TaleWorlds.Core.Extensions.GetRandomElementWithPredicate<CharacterObject>(
                            Hero.OneToOneConversationHero.Culture.NotableAndWandererTemplates,
                            x => x.Occupation == Occupation.Wanderer && ((BasicCharacterObject)x).IsFemale == ((BasicCharacterObject)Hero.OneToOneConversationHero.CharacterObject).IsFemale && x.CivilianEquipments != null);

                        if (elementWithPredicate == null)
                        {
                            elementWithPredicate = TaleWorlds.Core.Extensions.GetRandomElementWithPredicate<CharacterObject>(Hero.OneToOneConversationHero.Culture.NotableAndWandererTemplates, (Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.CivilianEquipments != null));
                            if (elementWithPredicate == null)
                            {
                                throw new Exception($"[MorePrisonerInteractions]: {Hero.OneToOneConversationHero.Name}'s culture ({Hero.OneToOneConversationHero.CharacterObject.Culture.Name}) has no wanderer or lords templates. Please contact the author of this culture instead of MorePrisonerInteractions.");
                            }
                        }

                        Hero.OneToOneConversationHero.CharacterObject.StringId = "MPI" + Hero.OneToOneConversationHero.CharacterObject.StringId;
                        typeof(CharacterObject).GetField("_originCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(Hero.OneToOneConversationHero.CharacterObject, elementWithPredicate);
                    }

                    Hero.OneToOneConversationHero.CharacterObject.StringId = "MPI" + Hero.OneToOneConversationHero.CharacterObject.StringId;
                    Hero.OneToOneConversationHero.StringId = Hero.OneToOneConversationHero.CharacterObject.StringId;
                    EndCaptivityAction.ApplyByReleasedByChoice(Hero.OneToOneConversationHero, Hero.MainHero);
                    AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty);
                    Clan clan = Hero.OneToOneConversationHero.Clan;
                    clan.Heroes.Remove(Hero.OneToOneConversationHero);
                    ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(clan);
                    AddCompanionAction.Apply(Clan.PlayerClan, Hero.OneToOneConversationHero);
                    TextObject announcement = new TextObject("{=Announcement_MPI_CompanionsMarried}{HERO_NAME} and {HERO_NAME2} are now married!");
                    announcement.SetTextVariable("HERO_NAME", Hero.OneToOneConversationHero.Name);
                    announcement.SetTextVariable("HERO_NAME2", first.Name);
                    MBInformationManager.AddQuickInformation(announcement, 0, null, "event:/ui/notification/relation");
                    if (clan.Leader == Hero.OneToOneConversationHero)
                    {
                        if (clan.Heroes.Count >= 0)
                        {
                            for (int i = clan.Heroes.Count - 1; i >= 0; --i)
                            {
                                if (clan.Heroes.ElementAt(i).IsAlive && clan.Heroes.ElementAt(i).IsChild)
                                {
                                    clan.SetLeader(clan.Heroes.ElementAt(i));
                                    break;
                                }
                            }
                            if(clan.Leader == Hero.OneToOneConversationHero)
                            {
                                CharacterObject character = CharacterObject.All.FirstOrDefault(c => c.Occupation == Occupation.Lord);
                                Hero temphero = HeroCreator.CreateSpecialHero(character, null, clan, null, -1);
                                TextObject FistName = new TextObject("Clan Leader");
                                TextObject LastName = new TextObject("Dead");
                                temphero.SetName(LastName, FistName);
                                clan.SetLeader(temphero);
                                DestroyClanAction.Apply(clan);
                            }
                        }
                        
                    }
                    Campaign.Current.ConversationManager.ContinueConversation();
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Cannot marry {first.Name} and {Hero.OneToOneConversationHero.Name}"));
                }
            }
            else
            {
                throw new Exception("One or both heroes are null.");
            }
        }
        bool MarriedOrNot(Hero x)
        {
            if (x.Spouse != null)
            {
                return true;
            }
            return false;
        }


        void HandleFailedInquiryMenuForMarriage(List<InquiryElement> inqury)
        {
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
            bool Disgusting = true;
            if (x.Spouse != null /*|| !Disgusting*/)
            {
                return false;
            }
            return Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(x, hero) || ((x.IsWanderer || x.IsHumanPlayerCharacter || hero.IsWanderer) && (x.IsFemale != hero.IsFemale));
        }
        bool ConsiderCondition(out TextObject reason)
        {
            CampaignTime currenttime = CampaignTime.Now;
            float DaySinceLastEXE = MathF.Abs(currenttime.ElapsedDaysUntilNow - LastConsiderTime.ElapsedDaysUntilNow);
            if (Hero.MainHero.GetSkillValue(DefaultSkills.Charm) > 150 && DaySinceLastEXE > 7)
            {
                reason = new TextObject();
                return true;
            }
            else if (Hero.MainHero.GetSkillValue(DefaultSkills.Charm) < 150)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_NoMarryableClanMembers}Your charm skill at least need 150 to makes {?INTERLOCUTOR.GENDER}she{?}he{\\?} consider again!");
                return false;
            }
            else
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_NoMarryableClanMembers}You need to wait more time to makes {?INTERLOCUTOR.GENDER}she{?}he{\\?} consider again");
                return false;
            }
        }
        bool IsCanConsiderAgain()
        {
            CampaignTime currenttime = CampaignTime.Now;
            float DaySinceLastEXE = MathF.Abs(currenttime.ElapsedDaysUntilNow - LastConsiderTime.ElapsedDaysUntilNow);
            if (DaySinceLastEXE > 7)
            {
                LastConsiderTime = currenttime;
                return true;
            }
            return false;

        }
        void ConsiderFunction()
        {
            if (BriceData.ContainsKey(Hero.OneToOneConversationHero))
            {
                BriceData.Remove(Hero.OneToOneConversationHero);
            }
        }
        void BridePrice()
        {
            if (!HasPaidBridePrice(Hero.OneToOneConversationHero))
            {
                Clan player = Hero.MainHero.Clan;
                Clan hero = Hero.OneToOneConversationHero.Clan;
                Random rndnmb = new Random();
                if (hero.TotalStrength-player.TotalStrength >= 650)
                {
                    if (Hero.MainHero.Gold / 2 > 1000000)
                    {
                        int PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...Unless you give me {GOLD_ICON}{DENIER_PRICE}");
                        if (!BriceData.ContainsKey(Hero.OneToOneConversationHero))
                        {
                            PlayerBridePrice = rndnmb.Next(1000000, Hero.MainHero.Gold / 2) + rndnmb.Next(1000000, 2000000);
                            BriceData[Hero.OneToOneConversationHero] = new BridePriceInfo(PlayerBridePrice, true);
                        }
                        else
                        {
                            PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        }
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else if (BriceData.ContainsKey(Hero.OneToOneConversationHero) && Hero.MainHero.Gold > BriceData[Hero.OneToOneConversationHero].PlayerBridePrice)
                    {
                        int PlayerBridePrice;
                        PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierBackWithLessMoney}You are back...Seems like you are broke now...Hope you still have {GOLD_ICON}{DENIER_PRICE}");
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else
                    {
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...You are so broke and weak...");
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                }
                else if (hero.TotalStrength - player.TotalStrength >= 350)
                {
                    if (Hero.MainHero.Gold / 2 > 500000)
                    {
                        int PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...Unless you give me {GOLD_ICON}{DENIER_PRICE}");
                        if (!BriceData.ContainsKey(Hero.OneToOneConversationHero))
                        {
                            PlayerBridePrice = rndnmb.Next(500000, Hero.MainHero.Gold / 2) + rndnmb.Next(500000, 1000000);
                            BriceData[Hero.OneToOneConversationHero] = new BridePriceInfo(PlayerBridePrice, true);
                        }
                        else
                        {
                            PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        }
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else if (BriceData.ContainsKey(Hero.OneToOneConversationHero) && Hero.MainHero.Gold > BriceData[Hero.OneToOneConversationHero].PlayerBridePrice)
                    {
                        int PlayerBridePrice;
                        PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierBackWithLessMoney}You are back...Seems like you are broke now...Hope you still have {GOLD_ICON}{DENIER_PRICE}");
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else
                    {
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...You are so broke and weak...");
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                }
                else if (hero.TotalStrength - player.TotalStrength >= 100)
                {
                    if (Hero.MainHero.Gold / 2 > 100000)
                    {
                        int PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...Unless you give me {GOLD_ICON}{DENIER_PRICE}");
                        if (!BriceData.ContainsKey(Hero.OneToOneConversationHero))
                        {
                            PlayerBridePrice = rndnmb.Next(100000, Hero.MainHero.Gold / 2) + rndnmb.Next(100000, 200000);
                            BriceData[Hero.OneToOneConversationHero] = new BridePriceInfo(PlayerBridePrice, true);
                        }
                        else
                        {
                            PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        }
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else if (BriceData.ContainsKey(Hero.OneToOneConversationHero) && Hero.MainHero.Gold > BriceData[Hero.OneToOneConversationHero].PlayerBridePrice)
                    {
                        int PlayerBridePrice;
                        PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierBackWithLessMoney}You are back...Seems like you are broke now...Hope you still have {GOLD_ICON}{DENIER_PRICE}");
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else
                    {
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...You are so broke and weak...");
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                }
                else if (hero.TotalStrength - player.TotalStrength > 0)
                {
                    if (Hero.MainHero.Gold / 2 > 10000)
                    {
                        int PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...Unless you give me {GOLD_ICON}{DENIER_PRICE}");
                        if (!BriceData.ContainsKey(Hero.OneToOneConversationHero))
                        {
                            PlayerBridePrice = rndnmb.Next(10000, Hero.MainHero.Gold / 2) + rndnmb.Next(10000, 20000);
                            BriceData[Hero.OneToOneConversationHero] = new BridePriceInfo(PlayerBridePrice, true);
                        }
                        else
                        {
                            PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        }
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else if (BriceData.ContainsKey(Hero.OneToOneConversationHero) && Hero.MainHero.Gold > BriceData[Hero.OneToOneConversationHero].PlayerBridePrice)
                    {
                        int PlayerBridePrice;
                        PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierBackWithLessMoney}You are back...Seems like you are broke now...Hope you still have {GOLD_ICON}{DENIER_PRICE}");
                        Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                    else
                    {
                        TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}I don't think so...You are so broke and weak...");
                        MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);
                    }
                }
                else if (hero.TotalStrength <= player.TotalStrength)
                {
                    int PlayerBridePrice;
                    TextObject Reply = new TextObject("{=Dialog_MPI_ForceMarriage_Unmarried_AskForDenierCaculating}Yes! Yes! Yes I do. Just give me {GOLD_ICON}{DENIER_PRICE} to makes me bought some new clothes. \n I promise I will join you!");
                    if (!BriceData.ContainsKey(Hero.OneToOneConversationHero))
                    {
                        PlayerBridePrice = 100;
                        BriceData[Hero.OneToOneConversationHero] = new BridePriceInfo(PlayerBridePrice, true);
                    }
                    else
                    {
                        PlayerBridePrice = BriceData[Hero.OneToOneConversationHero].PlayerBridePrice;
                    }
                    Reply.SetTextVariable("DENIER_PRICE", PlayerBridePrice);
                    MBTextManager.SetTextVariable("Dialog_MPI_ForceMarriage_LordAskforDenierOrNot", Reply, false);

                }
            }
        }
        bool RelationShip()
        {
            Hero hero = Hero.OneToOneConversationHero;
            if (hero.GetRelationWithPlayer() > -70) 
            {
                return true;
            }
            return false;
        }
        void GiveDenier()
        {
            Hero.MainHero.ChangeHeroGold(-BriceData[Hero.OneToOneConversationHero].PlayerBridePrice);
           foreach (Hero hero in Hero.OneToOneConversationHero.Clan.Heroes)
           {
               if (hero != Hero.OneToOneConversationHero)
               {
                   hero.ChangeHeroGold(BriceData[Hero.OneToOneConversationHero].PlayerBridePrice / (Hero.OneToOneConversationHero.Clan.Heroes.Count - 1));
               }
           }
           InformationManager.DisplayMessage(new InformationMessage($"You paid {BriceData[Hero.OneToOneConversationHero].PlayerBridePrice} denar."));
           SoundEvent yourSoundEvent = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString("event:/ui/notification/coins_negative"), Mission.Current.Scene);
           yourSoundEvent.Play();
           PaidBefore[Hero.OneToOneConversationHero] = true;
        }
        bool BrokeOrNot(out TextObject reason)
        {
            Clan hero = Hero.OneToOneConversationHero.Clan;
            Clan player = Hero.MainHero.Clan;
            if (BriceData.ContainsKey(Hero.OneToOneConversationHero) && Hero.MainHero.Gold < BriceData[Hero.OneToOneConversationHero].PlayerBridePrice)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_PlayerIsBroke}You are Broke...");
                return false;
            }
            else if (!BriceData.ContainsKey(Hero.OneToOneConversationHero) && hero.TotalStrength - player.TotalStrength >= 650 && Hero.MainHero.Gold / 2 < 1000000)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_PlayerIsBroke}You are Broke...");
                return false;
            }
            else if (!BriceData.ContainsKey(Hero.OneToOneConversationHero) && hero.TotalStrength - player.TotalStrength >= 350 && Hero.MainHero.Gold / 2 < 500000)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_PlayerIsBroke}You are Broke...");
                return false;
            }
            else if (!BriceData.ContainsKey(Hero.OneToOneConversationHero) && hero.TotalStrength - player.TotalStrength >= 100 && Hero.MainHero.Gold / 2 < 100000)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_PlayerIsBroke}You are Broke...");
                return false;
            }
            else if (!BriceData.ContainsKey(Hero.OneToOneConversationHero) && hero.TotalStrength - player.TotalStrength > 0 && Hero.MainHero.Gold / 2 < 10000)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_PlayerIsBroke}You are Broke...");
                return false;
            }
            else if (!BriceData.ContainsKey(Hero.OneToOneConversationHero) && hero.TotalStrength <= player.TotalStrength && Hero.MainHero.Gold < 100)
            {
                reason = new TextObject("{=Dialog_ForceMarry_DisabledReason_PlayerIsBroke}You are Broke...");
                return false;
            }
            else
            {
                reason = new TextObject();
                return true;
            }
        
        }

        void ExecuteLords(Hero hero)
        {
            KillCharacterAction.ApplyByExecution(hero,Hero.MainHero,true,true);
            BriceData.Remove(hero);
        }
    }
}
