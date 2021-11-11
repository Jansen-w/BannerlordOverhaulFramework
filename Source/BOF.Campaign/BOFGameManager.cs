using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem.Load;

namespace BOF.Campaign
{
    public class BOFGameManager : MBGameManager
    {
        private bool _loadingSavedGame;
        private LoadResult _loadedGameResult; // TODO: Replace with SQLite LoadResult
        private int _seed = 1234;

        public BOFGameManager()
        {
            _loadingSavedGame = false;
            _seed = (int)DateTime.Now.Ticks & ushort.MaxValue;
        }

        public BOFGameManager(int seed)
        {
            _loadingSavedGame = false;
            _seed = seed;
        }

        public BOFGameManager(LoadResult loadedGameResult)
        {
            _loadingSavedGame = true;
            _loadedGameResult = loadedGameResult;
        }

        public override void OnGameEnd(Game game)
        {
            MBDebug.SetErrorReportScene(null);
            base.OnGameEnd(game);
        }

        protected override void DoLoadingForGameManager(
            GameManagerLoadingSteps gameManagerLoadingStep,
            out GameManagerLoadingSteps nextStep)
        {
            nextStep = GameManagerLoadingSteps.None;
            switch (gameManagerLoadingStep)
            {
                case GameManagerLoadingSteps.PreInitializeZerothStep:
                    nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
                    break;
                case GameManagerLoadingSteps.FirstInitializeFirstStep:
                    LoadModuleData(_loadingSavedGame);
                    nextStep = GameManagerLoadingSteps.WaitSecondStep;
                    break;
                case GameManagerLoadingSteps.WaitSecondStep:
                    if (!_loadingSavedGame)
                        StartNewGame();
                    nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
                    break;
                case GameManagerLoadingSteps.SecondInitializeThirdState:
                    MBGlobals.InitializeReferences();
                    if (!_loadingSavedGame)
                    {
                        MBDebug.Print("Initializing new game begin...");
                        var campaign = new BOFCampaign(CampaignGameMode.Campaign);
                        Game.CreateGame(campaign, this);
                        campaign.SetLoadingParameters(BOFCampaign.GameLoadingType.NewCampaign, _seed);
                        MBDebug.Print("Initializing new game end...");
                    }
                    else
                    {
                        MBDebug.Print("Initializing saved game begin...");
                        ((BOFCampaign)Game.LoadSaveGame(_loadedGameResult, this).GameType).SetLoadingParameters(BOFCampaign.GameLoadingType.SavedCampaign, _seed);
                        _loadedGameResult = null;
                        Common.MemoryCleanup();
                        MBDebug.Print("Initializing saved game end...");
                    }

                    Game.Current.DoLoading();
                    nextStep = GameManagerLoadingSteps.PostInitializeFourthState;
                    break;
                case GameManagerLoadingSteps.PostInitializeFourthState:
                    bool flag = true;
                    foreach (MBSubModuleBase subModule in Module.CurrentModule.SubModules)
                        flag = flag && subModule.DoLoading(Game.Current);
                    nextStep = flag
                        ? GameManagerLoadingSteps.FinishLoadingFifthStep
                        : GameManagerLoadingSteps.PostInitializeFourthState;
                    break;
                case GameManagerLoadingSteps.FinishLoadingFifthStep:
                    nextStep = Game.Current.DoLoading()
                        ? GameManagerLoadingSteps.None
                        : GameManagerLoadingSteps.FinishLoadingFifthStep;
                    break;
            }
        }

        public override void OnLoadFinished()
        {
            if (!_loadingSavedGame)
            {
                MBDebug.Print("Switching to menu window...");
                if (!Game.Current.IsDevelopmentMode)
                {
                    VideoPlaybackState state = Game.Current.GameStateManager.CreateState<VideoPlaybackState>();
                    
                    // TODO: relocate to BOF module 
                    string moduleFullPath = ModuleHelper.GetModuleFullPath("SandBox");
                    string videoPath = moduleFullPath + "Videos/campaign_intro.ivf";
                    string audioPath = moduleFullPath + "Videos/campaign_intro.ogg";
                    string subtitleFileBasePath = moduleFullPath + "Videos/campaign_intro";
                    
                    state.SetStartingParameters(videoPath, audioPath, subtitleFileBasePath, 60f, true);
                    state.SetOnVideoFinisedDelegate(LaunchSandboxCharacterCreation);
                    Game.Current.GameStateManager.CleanAndPushState(state);
                }
                else
                {
                    LaunchSandboxCharacterCreation();
                }
            }
            else
            {
                Game.Current.GameStateManager.OnSavedGameLoadFinished();
                Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<MapState>());

                var activeState = Game.Current.GameStateManager.ActiveState;
                var isMapState = activeState is MapState;
                
                string name = activeState is MapState activeState2
                    ? activeState2.GameMenuId
                    : null;
                
                if (!string.IsNullOrEmpty(name))
                {
                    PlayerEncounter.Current?.OnLoad();
                    BOFCampaign.Current.GameMenuManager.SetNextMenu(name);
                }

                PartyBase.MainParty.Visuals?.SetMapIconAsDirty();
                BOFCampaign.Current.CampaignInformationManager.OnGameLoaded();
                
                foreach (Settlement settlement in Settlement.All)
                    settlement.Party.Visuals.RefreshLevelMask(settlement.Party);
                
                CampaignEventDispatcher.Instance.OnGameLoadFinished();
                
                if(isMapState)
                    ((MapState)activeState).OnLoadingFinished();
            }

            IsLoaded = true;
        }

        private void LaunchSandboxCharacterCreation() => Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<CharacterCreationState>(new SandboxCharacterCreationContent()));

        [CrashInformationCollector.CrashInformationProvider]
        private static CrashInformationCollector.CrashInformation UsedModuleInfoCrashCallback()
        {
            if (BOFCampaign.Current?.PreviouslyUsedModules == null)
                return null;
            
            string[] moduleNames = SandBoxManager.Instance.ModuleManager.ModuleNames;
            List<(string, string)> valueTupleList = new List<(string, string)>();
            foreach (string previouslyUsedModule in BOFCampaign.Current.PreviouslyUsedModules)
            {
                string module = previouslyUsedModule;
                bool flag =
                    moduleNames.FindIndex(x => x == module) !=
                    -1;
                valueTupleList.Add((module, flag ? "1" : "0"));
            }

            return new CrashInformationCollector.CrashInformation("Used Mods", valueTupleList);
        }
    }
}