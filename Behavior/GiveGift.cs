using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using static TaleWorlds.CampaignSystem.Actions.ChangeRelationAction;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MorePrisonerInteractions.Behavior
{
    public class GiveGift : CampaignBehaviorBase
    {
        long giftvalue = 0;
        public override void RegisterEvents()
        {
            CampaignEvents.OnBarterAcceptedEvent.AddNonSerializedListener(this, OnGivedGift);
            CampaignEvents.OnBarterCanceledEvent.AddNonSerializedListener(this, OnCancel);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
        }

        public override void SyncData(IDataStore dataStore)
        {

        }
        public void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_persuadeprisoner_giving_gift", "dialog_mpi_main_options", "player_give_gift", "{=Dialog_MPI_PersuadePrisoner_GiveGift}Listen, I have some gift for you.", null, null, 100, null, null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_reply_gift_by_lord", "player_give_gift", "lord_reply_recive_gift", "{=Dialog_MPI_PersuadePrisoner_Reciver_Reply_Before}I'd never expect this will happened since I was capture by your troops.", CanGiveAgain, null, 100, null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_reply_gift_by_lord", "player_give_gift", "lord_reply_recive_gift_failed", "{=!}{Cant_Give_Reason}", null, GiftReply, 100, null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_opening_barter", "lord_reply_recive_gift", "lord_recive_gift", "{=Dialog_MPI_PersuadePrisoner_GiveGiftAction}", null, GivingGift, 100, null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_after_recived", "lord_recive_gift", "lord_reply_after_recived", "{=!}{Give_Reaction}", AcceptGift, null, 100, null);
            gameStarter.AddDialogLine("dialog_mpi_persuadeprisoner_after_recived_failed", "lord_recive_gift", "lord_reply_after_recived_failed", "{=!}{Dialog_MPI_PersuadePrisoner_Reciver_Reply_After_Failed}", null, GiftReply, 100, null);

        }
        private bool AcceptGift()
        {
            return Campaign.Current.BarterManager.LastBarterIsAccepted;
        }
        void GiftReply()
        {
            Hero x = Hero.OneToOneConversationHero;
            Hero player = Hero.MainHero;

            bool replied = false;
            if (!CanGiveAgain() && x.GetRelationWithPlayer() > 0 && !replied)
            {
                MBTextManager.SetTextVariable("Cant_Give_Reason", "I think you've given me too many gifts recently, I don't deserve them.");
                replied = true;
            }
            else if (!CanGiveAgain() && x.GetRelationWithPlayer() < 0 && !replied)
            {
                MBTextManager.SetTextVariable("Cant_Give_Reason", "You come to insult me again? Fuck off...");
                replied = true;
            }
            MBTextManager.SetTextVariable("Dialog_MPI_PersuadePrisoner_Reciver_Reply_After_Failed", "I guess you haven't prepared my gift yet? Come find me when you're ready.");
        }
        void OnCancel(Hero player, Hero x, List<Barterable> barters)
        {
            
        }
        public bool CanGiveAgain()
        {
            return BarterManager.Instance.CanPlayerBarterWithHero(Hero.OneToOneConversationHero);
        }
        void GivingGift()
        {
            Hero mainHero = Hero.MainHero;
            Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
            BarterManager.Instance.StartBarterOffer(mainHero, oneToOneConversationHero, PartyBase.MainParty, null, null, null, 0, false, null);
        }
        int GetTotalBarterValue(Hero NPCHero, PartyBase NPCParty, PartyBase PlayerParty, IEnumerable<Barterable> offeredBarters)
        {
            float totalValue = BarterManager.Instance.GetOfferValue(NPCHero, NPCParty, PlayerParty, offeredBarters);
            return (int)totalValue;
        }
        void OnGivedGift(Hero player, Hero x, List<Barterable> barters)
        {
            if (x!=null)
            {
                if (x.PartyBelongedTo == null && x.PartyBelongedToAsPrisoner == PartyBase.MainParty)
                {
                    giftvalue = GetTotalBarterValue(x, x.PartyBelongedToAsPrisoner, PartyBase.MainParty, barters);
                    long TotalValue = 0;
                    DefaultSettlementValueModel settlementValueModel = new DefaultSettlementValueModel();
                    long settlementValue = 0;
                    foreach (Settlement settlement in Settlement.All)
                    {
                        if (settlement.IsCastle || settlement.IsFortification)
                        {
                            if (settlement.Owner == x)
                            {
                                settlementValue = (long)settlementValueModel.CalculateSettlementBaseValue(settlement);
                                TotalValue += (long)settlementValue;
                            }
                        }
                    }

                    TotalValue += x.Gold - giftvalue;


                    if (giftvalue >= 1 && giftvalue <= 10 && x.GetRelationWithPlayer() >= 0)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Thanks...I guess?");
                        GiveStaticRelation(player, x, 1);
                    }
                    else if (giftvalue >= 1 && giftvalue <= 10 && x.GetRelationWithPlayer() < 0)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "You think I'm joke or what? Don't insult me again.");
                        GiveStaticRelation(player, x, -5);
                    }
                    else if (x.GetRelationWithPlayer() < -70 && giftvalue > TotalValue)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Based on the value of the gifts you've given me,\n I feel I need to thank you,\nbut I will never forget what you have done, nor the hatred between us.");
                        GiveStaticRelation(player, x, 20);
                    }
                    else if (x.GetRelationWithPlayer() > -70 && x.GetRelationWithPlayer() < -20 && giftvalue > TotalValue / 2)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Alright, this time I'm going to thank you,\n but don't forget,\n we are still enemies, that's all.");
                        GiveStaticRelation(player, x, 15);
                    }
                    else if (x.GetRelationWithPlayer() < 0 && x.GetRelationWithPlayer() > -20 && giftvalue > TotalValue / 4)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Oh. Thanks! But don’t you think you are too generous to the enemy?");
                        GiveStaticRelation(player, x, 10);
                    }
                    else if (x.GetRelationWithPlayer() < 20 && x.GetRelationWithPlayer() >= 0 && giftvalue > TotalValue / 8)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Oh. Thanks! I think we might become friends after this war.");
                        GiveStaticRelation(player, x, 10);
                    }
                    else if (x.GetRelationWithPlayer() < 70 && x.GetRelationWithPlayer() >= 20 && giftvalue > TotalValue / 10)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Buddy, You know I don't wanna hurt you...\nBut it is difficult to disobey the king’s order.\nLet's go to the bar after this war,OK?");
                        GiveStaticRelation(player, x, 10);
                    }
                    else if (x.GetRelationWithPlayer() > 70)
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Bro, you don't have to do this... \nI'll always be your friend. I feel so ashamed.”");
                        GiveStaticRelation(player, x, 2);
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("Give_Reaction", "Although it's not as much as I imagined, I'll still accept it. Thank you.");
                        GiveStaticRelation(player, x, 2);
                    }
                }
            }

        }
        public void GiveStaticRelation(Hero player,Hero x,int GainedRelation)
        {
            Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(player, x, out player, out x);
            int num = CharacterRelationManager.GetHeroRelation(player, x) + GainedRelation;
            num = MBMath.ClampInt(num, -100, 100);
            player.SetPersonalRelation(x, num);
            CampaignEventDispatcher.Instance.OnHeroRelationChanged(player, x, GainedRelation, true, ChangeRelationDetail.Default, player, x);
        }
    }
}
