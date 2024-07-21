using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static MorePrisonerInteractions.SubModule;

namespace MorePrisonerInteractions.Behavior
{
    public class HuntPrisonerBehavior : CampaignBehaviorBase
    {
        public static Hero TargetPrisoner;
        public override void RegisterEvents()
        {
            //throw new NotImplementedException();
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddMenus);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
            dataStore.SyncData("MorePrisonerInteractions_HuntPrisoner", ref TargetPrisoner);
            if (dataStore.IsLoading)
            {
                int x = 0;
                dataStore.SyncData("MorePrisonerInteractions_HuntPrisonerStatus", ref x);
            }
            else if (dataStore.IsSaving)
            {
                int x = (int)huntMissionStatus;
                dataStore.SyncData("MorePrisonerInteractions_HuntPrisonerStatus", ref x);
            }

        }

        public void AddMenus(CampaignGameStarter gameStarter)
        {
            gameStarter.AddGameMenu("gamemenu_mpi_huntinggame", "{=GameMenu_MPI_HuntingGame_Title}Hunting Game", new OnInitDelegate(GameMenuInit), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            gameStarter.AddGameMenuOption("gamemenu_mpi_huntinggame", "gamemenu_mpi_huntinggame_start", "{=GameMenu_MPI_HuntingGame_Start}Start the hunting game!", null, new GameMenuOption.OnConsequenceDelegate(StartHuntingMission));
            gameStarter.AddGameMenuOption("gamemenu_mpi_huntinggame", "gamemenu_mpi_huntinggame_cancel", "{=GameMenu_MPI_HuntingGame_Cancel}Cancel.", null, new GameMenuOption.OnConsequenceDelegate(CancelHunting));
        }

        void GameMenuInit(MenuCallbackArgs args)
        {
            if (TargetPrisoner == null)
                GameMenu.ExitToLast();
        }

        public void AddDialogs(CampaignGameStarter gameStarter)
        {
            gameStarter.AddPlayerLine("dialog_mpi_huntprisoner_start", "dialog_mpi_main_options", "dialog_mpi_huntprisoner_startreply", "{=Dialog_MPI_HuntPrisoner_Start}My people and I are bored. Let's play the hunting game.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(IsPrisonerHealthy), null);
            gameStarter.AddDialogLine("dialog_mpi_huntprisoner_startreply", "dialog_mpi_huntprisoner_startreply", "dialog_mpi_huntprisoner_0", "{=Dialog_MPI_HuntPrisoner_StartReply}W-what is a hunting game?[ib:weary2][if:convo_nervous]", null, null, 100, null);
            gameStarter.AddPlayerLine("dialog_mpi_huntprisoner_0", "dialog_mpi_huntprisoner_0", "dialog_mpi_huntprisoner_0_reply", "{=Dialog_MPI_HuntPrisoner_0}You get a headstart to run, and we hunt you down afterwards!", null, null, 100, null, null);
            gameStarter.AddDialogLine("dialog_mpi_huntprisoner_0_reply", "dialog_mpi_huntprisoner_0_reply", "dialog_mpi_huntprisoner_1", "{=Dialog_MPI_HuntPrisoner_0Reply}No, please! Have mercy! [ib:nervous][if:convo_shocked]", null, null, 100, null);
            gameStarter.AddPlayerLine("dialog_mpi_huntprisoner_1", "dialog_mpi_huntprisoner_1", "close_window", "{=Dialog_MPI_HuntPrisoner_1}The only mercy you are getting is the headstart! Now run!", null, () => StartHunting(), 100, null, null);
            gameStarter.AddPlayerLine("dialog_mpi_huntprisoner_1_cancel", "dialog_mpi_huntprisoner_1", "dialog_mpi_start", "{=Dialog_MPI_HuntPrisoner_1_cancel}Hah! Look at your face! I was only joking...", null, null, 99, null, null);
        }

        bool IsPrisonerHealthy(out TextObject reason)
        {
            if (SubModule.huntMissionStatus != HuntMissionStatus.Disabled)
            {
                reason = new TextObject("{=Dialog_MPI_HuntPrisoner_DisabledReason}You have already started a hunting game targeting {HERO_NAME}! Finish it first!");
                reason.SetTextVariable("HERO_NAME", TargetPrisoner.Name);
                return false;
            }


            bool isPrisonerHealthy = !Hero.OneToOneConversationHero.IsWounded;
            if (!isPrisonerHealthy)
                reason = new TextObject("{=Dialog_MPI_HuntPrisoner_PrisonerTooWounded}Prisoner is too injured to be involved.");
            else
                reason = null;
            return isPrisonerHealthy;
        }

        void StartHunting()
        {
            Campaign.Current.ConversationManager.ConversationEndOneShot += () =>
            {
                //if (PartyScreenManager.PartyScreenLogic != null)
                //    PartyScreenManager.CloseScreen(true);
                TargetPrisoner = Hero.OneToOneConversationHero;
                GameMenu.ActivateGameMenu("gamemenu_mpi_huntinggame");

                SubModule.huntMissionStatus = SubModule.HuntMissionStatus.Pending;
            };

        }

        void CancelHunting(MenuCallbackArgs args)
        {
            huntMissionStatus = HuntMissionStatus.Ended;
            GameMenu.ExitToLast();
        }

        void StartHuntingMission(MenuCallbackArgs args)
        {
            if (!MobileParty.MainParty.PrisonRoster.Contains(TargetPrisoner.CharacterObject))
            {

                huntMissionStatus = HuntMissionStatus.Ended;
                GameMenu.ExitToLast();

                TextObject title = new TextObject("{=GameMenu_MPI_HuntPrisonerFailed_Title}You can no longer start the hunting game!");
                TextObject desc = new TextObject("{=GameMenu_MPI_HuntPrisonerFailed_Desc}{HERO_NAME} is no longer a prisoner of your party! You will have to cancel it...");
                desc.SetTextVariable("HERO_NAME", TargetPrisoner.Name);
                TextObject ok = new TextObject("{=GameMenu_MPI_HuntPrisonerFailed_Confirm}I see...");
                InformationManager.ShowInquiry(new InquiryData(title.ToString(), desc.ToString(), true, false, ok.ToString(), "", null, null));
                return;
            }

            Clan clan = Clan.BanditFactions.First((Clan clanLooters) => clanLooters.StringId == "looters");
            clan.Banner.SetBannerVisual(Banner.CreateRandomBanner().BannerVisual);
            Settlement settlement2 = SettlementHelper.FindNearestSettlement((Settlement settlement) => true, null);
            MobileParty mobileParty = BanditPartyComponent.CreateLooterParty("MPI_" + MBRandom.RandomInt(int.MaxValue).ToString(), clan, settlement2, false);
            PartyTemplateObject defaultPartyTemplate = clan.DefaultPartyTemplate;
            mobileParty.InitializeMobilePartyAroundPosition(defaultPartyTemplate, MobileParty.MainParty.Position2D, 0.5f, 0.1f, -1);
            mobileParty.SetCustomName(TargetPrisoner.Name);
            mobileParty.MemberRoster.Clear();
            MobileParty.MainParty.PrisonRoster.RemoveTroop(TargetPrisoner.CharacterObject);
            AddHeroToPartyAction.Apply(TargetPrisoner, mobileParty, false);
            mobileParty.RecentEventsMorale = -100f;
            mobileParty.IsActive = true;
            mobileParty.ActualClan = clan;
            mobileParty.Party.SetCustomOwner(clan.Leader);
            //mobileParty.Party.Visuals.SetMapIconAsDirty();
            mobileParty.Party.SetVisualAsDirty();
            mobileParty.InitializePartyTrade(0);
            mobileParty.SetCustomHomeSettlement(settlement2);
            PlayerEncounter.RestartPlayerEncounter(mobileParty.Party, MobileParty.MainParty.Party, true);
            StartBattleAction.ApplyStartBattle(MobileParty.MainParty, mobileParty);
            PlayerEncounter.Update();
            SubModule.huntMissionStatus = SubModule.HuntMissionStatus.Started;
            string battleSceneForMapPatch = PlayerEncounter.GetBattleSceneForMapPatch(Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position2D));
            MissionInitializerRecord rec = new MissionInitializerRecord(battleSceneForMapPatch)
            {
                TerrainType = (int)Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace),
                DamageToPlayerMultiplier = Campaign.Current.Models.DifficultyModel.GetDamageToPlayerMultiplier(),
                DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier(),
                NeedsRandomTerrain = false,
                PlayingInCampaignMode = true,
                RandomTerrainSeed = MBRandom.RandomInt(10000),
                AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.GetLogicalPosition())
            };
            float timeOfDay = Campaign.CurrentTime % 24f;
            if (Campaign.Current != null)
            {
                rec.TimeOfDay = timeOfDay;
            }


            IMission mission = CampaignMission.OpenBattleMission(rec);
        }
    }
}
