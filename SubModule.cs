using MorePrisonerInteractions.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MorePrisonerInteractions
{
    public class SubModule : MBSubModuleBase
    {
        #region HuntPrisonerBehavior
        public enum HuntMissionStatus
        {
            Disabled,
            Pending,
            Started,
            InProgress,
            Ended,
        }
        public static HuntMissionStatus huntMissionStatus = HuntMissionStatus.Disabled;
        #endregion

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            CampaignGameStarter campaignGameStarter = gameStarterObject as CampaignGameStarter;
            if (campaignGameStarter != null)
            {
                campaignGameStarter.AddBehavior(new MorePrisonerInteractionsBaseDialogBehavior());
                campaignGameStarter.AddBehavior(new HuntPrisonerBehavior());
                campaignGameStarter.AddBehavior(new StripEquipmentBehavior());
                campaignGameStarter.AddBehavior(new DemandRansomBehavior());
                campaignGameStarter.AddBehavior(new PersuadePrisonerToBecomeCompanionBehavior());
                campaignGameStarter.AddBehavior(new ForcePrisonerToMarryBehavior());
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            UpdateHuntMission();
        }
        void UpdateHuntMission()
        {
            if (huntMissionStatus == HuntMissionStatus.Disabled)
                return;
            else if (huntMissionStatus == HuntMissionStatus.Started)
            {
                MissionState missionState = Game.Current.GameStateManager.ActiveState as MissionState;
                if (missionState != null && missionState.CurrentMission.IsLoadingFinished && missionState.CurrentMission.IsMissionEnding)
                {
                    huntMissionStatus = HuntMissionStatus.InProgress;
                    
                }
  
            }
            else if (huntMissionStatus == HuntMissionStatus.InProgress)
            {
              
                MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
                if (mapState != null && mapState.IsActive )
                {
                    
                   // PlayerEncounter.SetPlayerVictorious();
                    //PlayerEncounter.EnemySurrender = true;
                    //PlayerEncounter.LeaveEncounter = true;
                    //PlayerEncounter.Update();
                    
                    huntMissionStatus = HuntMissionStatus.Ended;
                    MobileParty party = HuntPrisonerBehavior.TargetPrisoner.PartyBelongedTo;
                    MobileParty.MainParty.AddPrisoner(HuntPrisonerBehavior.TargetPrisoner.CharacterObject, 1);
                    party.RemoveParty();
                    GameMenu.ExitToLast();
                }
            }
            else if (huntMissionStatus == HuntMissionStatus.Ended)
            {
                MapState mapState2 = Game.Current.GameStateManager.ActiveState as MapState;
                if (mapState2 != null && !mapState2.IsMenuState && PlayerEncounter.Current == null)
                {
                    huntMissionStatus = HuntMissionStatus.Disabled;

                }
            }
        }
    }
}
