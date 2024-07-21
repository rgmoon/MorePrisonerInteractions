using MoreHeroInteractions.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace MorePrisonerInteractions.Behavior
{
    public class StripEquipmentBehavior : CampaignBehaviorBase
    {
        Dictionary<Hero, Equipment> _prisonerBattleEquipment = new Dictionary<Hero, Equipment>();
        Dictionary<Hero, Equipment> _prisonerCivilianEquipment = new Dictionary<Hero, Equipment>();


        public override void RegisterEvents()
        {
            //throw new NotImplementedException();
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnPrisonerReleased);
            CampaignEvents.CharacterBecameFugitive.AddNonSerializedListener(this, OnHeroFugitive);
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
            {
                dataStore.SyncData("MorePrisonerInteractions_PrisonerBattleEquipment", ref _prisonerBattleEquipment);
                dataStore.SyncData("MorePrisonerInteractions_PrisonerCivilianEquipment", ref _prisonerCivilianEquipment);
            }
            else if (dataStore.IsLoading)
            {
                _prisonerBattleEquipment = new Dictionary<Hero, Equipment>();
                _prisonerCivilianEquipment = new Dictionary<Hero, Equipment>();
                dataStore.SyncData("MorePrisonerInteractions_PrisonerBattleEquipment", ref _prisonerBattleEquipment);
                dataStore.SyncData("MorePrisonerInteractions_PrisonerCivilianEquipment", ref _prisonerCivilianEquipment);
            }
        }

        public void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_stripequipment_start", "dialog_mpi_main_options", "dialog_mpi_stripequipment_startreply", "{=Dialog_MPI_StripEquipment_Start}Your equipment looks nice! I will be taking them away.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(CanStripHero), null);
            gameStarter.AddDialogLine("dialog_mpi_stripequipment_startreply", "dialog_mpi_stripequipment_startreply", "dialog_mpi_stripequipment_0", "{=Dialog_MPI_StripEquipment_StartReply}W-what?![ib:nervous][if:convo_shocked]", null, null, 100, null);
            gameStarter.AddPlayerLine("dialog_mpi_stripequipment_0", "dialog_mpi_stripequipment_0", "dialog_mpi_stripequipment_0_reply", "{=Dialog_MPI_StripEquipment_0}Yes, Now strip!", null, () => { StripTheHero(Hero.OneToOneConversationHero); Agent agent = Campaign.Current.ConversationManager.OneToOneConversationAgent as Agent; agent.UpdateSpawnEquipmentAndRefreshVisuals(Hero.OneToOneConversationHero.CivilianEquipment); }, 100, null, null);
            gameStarter.AddPlayerLine("dialog_mpi_stripequipment_0_cancel;", "dialog_mpi_stripequipment_0", "dialog_mpi_start", "{=Dialog_MPI_StripEquipment_0_Cancel}Nah, I was only joking!", null, null, 100, null, null);
            gameStarter.AddDialogLine("dialog_mpi_stripequipment_0_reply", "dialog_mpi_stripequipment_0_reply", "dialog_mpi_start", "{=Dialog_MPI_StripEquipment_0_Reply}You monster!", null, null, 100, null);
        }


        bool CanStripHero(out TextObject reason)
        {
           
            bool canStrip = (!_prisonerBattleEquipment.ContainsKey(Hero.OneToOneConversationHero) || !_prisonerCivilianEquipment.ContainsKey(Hero.OneToOneConversationHero));
            if (canStrip)
            {
                reason = new TextObject();
            }
            else
            {
                reason = new TextObject("{=Dialogs_MPI_StripEquipment_DisabledReason}You have already stripped this person's equipment!");
            }
            return canStrip;
        }

        void StripTheHero(Hero hero)
        {
            if (!_prisonerBattleEquipment.ContainsKey(hero))
                _prisonerBattleEquipment.Add(hero, hero.BattleEquipment.Clone());
            if (!_prisonerCivilianEquipment.ContainsKey(hero))
                _prisonerCivilianEquipment.Add(hero, hero.CivilianEquipment.Clone());

            for (int i = 0; i < 12; i++)
            {
                ItemRoster itemRoster = PartyBase.MainParty.ItemRoster;
                EquipmentElement equipmentElement = hero.BattleEquipment[i];
                if (equipmentElement.Item != null)
                {
                    itemRoster.AddToCounts(equipmentElement.Item, 1);
                    hero.BattleEquipment[i] = default;
                    TextObject text = new TextObject("{=Announcement_MPI_StripEquipment}{EQUIPMENT_NAME} has been taken!");
                    text.SetTextVariable("EQUIPMENT_NAME", equipmentElement.Item.Name.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                }

                equipmentElement = hero.CivilianEquipment[i];
                if (equipmentElement.Item != null)
                {
                    itemRoster.AddToCounts(equipmentElement.Item, 1);
                    hero.CivilianEquipment[i] = default;
                    TextObject text = new TextObject("{=Announcement_MPI_StripEquipment}{EQUIPMENT_NAME} has been taken!");
                    text.SetTextVariable("EQUIPMENT_NAME", equipmentElement.Item.Name.ToString());
                    InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                }
            }



            if (MCMSettings.Instance.stripPrisonerLordMinRelationLoss > MCMSettings.Instance.stripPrisonerLordMaxRelationLoss)
            {
                int temp = MCMSettings.Instance.stripPrisonerLordMinRelationLoss;
                MCMSettings.Instance.stripPrisonerLordMinRelationLoss = MCMSettings.Instance.stripPrisonerLordMaxRelationLoss;
                MCMSettings.Instance.stripPrisonerLordMaxRelationLoss = temp;
            }
            ChangeRelationAction.ApplyPlayerRelation(hero, -new Random().Next(MCMSettings.Instance.stripPrisonerLordMinRelationLoss, MCMSettings.Instance.stripPrisonerLordMaxRelationLoss), false, true);
        }

        void OnMobilePartyCreated(MobileParty party)
        {;

            Hero leader = party.LeaderHero;
            if (leader != null)
            {
                ReturnEquipment(leader);
            }
             
        }

        void OnPrisonerReleased(Hero hero, PartyBase party, IFaction faction, EndCaptivityDetail detail)
        {
                ReturnEquipment(hero);
        }

        void OnHeroFugitive(Hero hero)
        {
            ReturnEquipment(hero);
        }

        // fail-safe mechanism
        void DailyTickHero(Hero hero)
        {
            Dictionary<Hero, Equipment> copyOfPrisonerBattleEquipment = new Dictionary<Hero, Equipment>(_prisonerBattleEquipment);
            Dictionary<Hero, Equipment> copyOfPrisonerCivilianEquipment = new Dictionary<Hero, Equipment>(_prisonerCivilianEquipment);


            foreach (var heroEquipment in copyOfPrisonerBattleEquipment)
            {
                if (!heroEquipment.Key.IsPrisoner || heroEquipment.Key.PartyBelongedToAsPrisoner != PartyBase.MainParty)
                {
                    ReturnEquipment(heroEquipment.Key);
                }
            }

            foreach (var heroEquipment in copyOfPrisonerCivilianEquipment)
            {
                if (!heroEquipment.Key.IsPrisoner || heroEquipment.Key.PartyBelongedToAsPrisoner != PartyBase.MainParty)
                {
                    ReturnEquipment(heroEquipment.Key);
                }
            }
        }

       
        void ReturnEquipment(Hero hero)
        {
            if (_prisonerBattleEquipment == null || _prisonerCivilianEquipment == null)
                return;
            if ((_prisonerBattleEquipment.ContainsKey(hero) || _prisonerCivilianEquipment.ContainsKey(hero)))
            {
                bool containsBattleEquipment = _prisonerBattleEquipment.ContainsKey(hero);
                bool containsCivilianEquipment = _prisonerCivilianEquipment.ContainsKey(hero);
                for (int i = 0; i < 12; i++)
                {

                    //EquipmentElement equipmentElement = hero.BattleEquipment[i];

                    //if (equipmentElement.Item != null)
                    {
                        if (containsBattleEquipment)
                        {
                            hero.BattleEquipment[i] = _prisonerBattleEquipment[hero][i];
                            //InformationManager.DisplayMessage(new InformationMessage("Hiya1"));
                        }
                    }


                    //equipmentElement = hero.CivilianEquipment[i];
                    //if (equipmentElement.Item != null)
                    {
                        if (containsCivilianEquipment)
                        {
                            hero.CivilianEquipment[i] = _prisonerCivilianEquipment[hero][i];
                            //InformationManager.DisplayMessage(new InformationMessage("Hiya2"));
                        }
                    }
                }
                if (containsBattleEquipment)
                    _prisonerBattleEquipment.Remove(hero);
                if (containsCivilianEquipment)
                    _prisonerCivilianEquipment.Remove(hero);
            }
        }
    }
}
