using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using ManagedParameters = TaleWorlds.Core.ManagedParameters;

namespace BOF.Campaign
{
    public class BOFCampaign : GameType
    {
        public enum GameLoadingType
        {
            Tutorial,
            NewCampaign,
            SavedCampaign,
            Editor,
        }

        [Flags]
        public enum PartyRestFlags : uint
        {
            None = 0,
            SafeMode = 1,
        }

        // TODO: Let's get rid of as many of these variables as possible
        public const float ConfigTimeMultiplier = 0.25f;
        private readonly LogEntryHistory _logEntryHistory = new LogEntryHistory();
        public readonly CampaignOptions Options;
        private PartyBase _cameraFollowParty;
        private ICampaignBehaviorManager _campaignBehaviorManager;
        private EntitySystem<CampaignEntityComponent> _campaignEntitySystem;
        private CampaignPeriodicEventManager _campaignPeriodicEventManager;
        private List<Town> _castles;
        private MBReadOnlyList<CharacterObject> _characters;
        private MBReadOnlyList<Concept> _concepts;
        private ConversationManager _conversationManager;
        private int _curSessionFrame;
        private MBCampaignEvent _dailyTickEvent;
        [CachedData] 
        private float _dt;
        private EncyclopediaManager _encyclopediaManager;
        private GameLoadingType _gameLoadingType;
        private GameModels _gameModels;
        private List<Hideout> _hideouts;
        private MBCampaignEvent _hourlyTickEvent;
        private Monster _humanChildMonster;
        private Monster _humanMonsterMap;
        private Monster _humanMonsterSettlement;
        private Monster _humanMonsterSettlementFast;
        private Monster _humanMonsterSettlementSlow;
        private InventoryManager _inventoryManager;
        private bool _isMainPartyWaiting;
        private MBReadOnlyList<ItemModifierGroup> _itemModifierGroups;
        private MBReadOnlyList<ItemModifier> _itemModifiers;
        [CachedData] 
        private int _lastNonZeroDtFrame;
        private int _lastPartyIndex;
        private IMapScene _mapSceneWrapper;
        private IMapTracksCampaignBehavior _mapTracksCampaignBehavior;
        private LocatorGrid<MobileParty> _mobilePartyLocator;
        private CampaignTickPartyDataCache _mobilePartyTickDataCache = new CampaignTickPartyDataCache();
        private int _numGameMenusCreated;
        private PartyScreenManager _partyScreenManager;
        public PartyUpgrader _partyUpgrader = new PartyUpgrader();
        private Dictionary<CharacterObject, FormationClass> _playerFormationPreferences;
        private List<string> _previouslyUsedModules;
        private LocatorGrid<Settlement> _settlementLocator;
        private int _stepNo;
        private CampaignTimeControlMode _timeControlMode;
        private List<Town> _towns;
        private List<Village> _villages;
        private MBReadOnlyList<WorkshopType> _workshops;
        public PartyBase autoEnterTown;
        public int CheatFindItemRangeBegin;
        public ConversationContext CurrentConversationContext;
        public GameLogs GameLogs = new GameLogs();
        public bool GameStarted;
        public int InfluenceValueTermsOfGold = 10;
        public bool IsInitializedSinglePlayerReferences;
        public KingdomManager KingdomManager;
        public CampaignTimeControlMode LastTimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
        public float MaxSettlementX;
        public float MaxSettlementY;
        public float MinSettlementX;
        public float MinSettlementY;
        public MBReadOnlyDictionary<CharacterObject, FormationClass> PlayerFormationPreferences;
        public ITournamentManager TournamentManager;
        public bool UseFreeCameraAtMapScreen;

        public BOFCampaign(CampaignGameMode gameMode)
        {
            GameMode = gameMode;
            Options = new CampaignOptions();
            MapTimeTracker = new MapTimeTracker(CampaignData.CampaignStartTime);
            CampaignStartTime = MapTimeTracker.Now;
            CampaignObjectManager = new CampaignObjectManager();
            CurrentConversationContext = ConversationContext.Default;
            QuestManager = new QuestManager();
            IssueManager = new IssueManager();
            FactionManager = new FactionManager();
            CharacterRelationManager = new CharacterRelationManager();
            Romance = new Romance();
            PlayerCaptivity = new PlayerCaptivity();
            BarterManager = new BarterManager();
            AdjustedRandom = new AdjustedRandom();
            GameMenuCallbackManager = new GameMenuCallbackManager();
        }

        public static float MapDiagonal { get; private set; }
        public static Vec2 MapMinimumPosition { get; private set; }
        public static Vec2 MapMaximumPosition { get; private set; }
        public static float MapMaximumHeight { get; private set; }
        public static float AverageDistanceBetweenTwoTowns { get; private set; }
        public IReadOnlyList<string> PreviouslyUsedModules => _previouslyUsedModules;

        public CampaignEventDispatcher CampaignEventDispatcher { get; private set; }
        public string UniqueGameId { get; private set; }
        public SaveHandler SaveHandler { get; private set; }
        public override bool SupportsSaving => GameMode == CampaignGameMode.Campaign;
        public CampaignObjectManager CampaignObjectManager { get; private set; }
        public override bool IsDevelopment => GameMode == CampaignGameMode.Tutorial;
        public bool IsCraftingEnabled { get; set; } = true;
        public bool IsBannerEditorEnabled { get; set; } = true;
        public bool IsFaceGenEnabled { get; set; } = true;
        public ICampaignBehaviorManager CampaignBehaviorManager => _campaignBehaviorManager;
        public QuestManager QuestManager { get; private set; }
        public IssueManager IssueManager { get; private set; }
        public FactionManager FactionManager { get; private set; }
        public CharacterRelationManager CharacterRelationManager { get; private set; }
        public Romance Romance { get; private set; }
        public PlayerCaptivity PlayerCaptivity { get; private set; }
        public Clan PlayerDefaultFaction { get; set; }
        public ICampaignMissionManager CampaignMissionManager { get; set; }
        public ICampaignMapConversation CampaignMapConversationManager { get; set; }
        public IMapSceneCreator MapSceneCreator { get; set; }
        public AdjustedRandom AdjustedRandom { get; private set; }
        public override bool IsInventoryAccessibleAtMission => GameMode == CampaignGameMode.Tutorial;
        public GameMenuCallbackManager GameMenuCallbackManager { get; private set; }
        public VisualCreator VisualCreator { get; set; }
        public Monster HumanMonsterSettlement => _humanMonsterSettlement ?? (_humanMonsterSettlement = CurrentGame.ObjectManager.GetObject<Monster>("human_settlement"));
        public Monster HumanChildMonster => _humanChildMonster ?? (_humanChildMonster = CurrentGame.ObjectManager.GetObject<Monster>("human_child"));
        public Monster HumanMonsterSettlementSlow => _humanMonsterSettlementSlow ?? (_humanMonsterSettlementSlow = CurrentGame.ObjectManager.GetObject<Monster>("human_settlement_slow"));
        public Monster HumanMonsterSettlementFast => _humanMonsterSettlementFast ?? (_humanMonsterSettlementFast = CurrentGame.ObjectManager.GetObject<Monster>("human_settlement_fast"));
        public Monster HumanMonsterMap => _humanMonsterMap ??(_humanMonsterMap = CurrentGame.ObjectManager.GetObject<Monster>("human_map"));
        public MapStateData MapStateData { get; private set; }
        public DefaultPerks DefaultPerks { get; private set; }
        public DefaultTraits DefaultTraits { get; private set; }
        public DefaultPolicies DefaultPolicies { get; private set; }
        public DefaultBuildingTypes DefaultBuildingTypes { get; private set; }
        public DefaultIssueEffects DefaultIssueEffects { get; private set; }
        public DefaultSiegeStrategies DefaultSiegeStrategies { get; private set; }
        internal MBReadOnlyList<PerkObject> AllPerks { get; private set; }
        public PlayerUpdateTracker PlayerUpdateTracker { get; private set; }
        public DefaultSkillEffects DefaultSkillEffects { get; private set; }
        public DefaultVillageTypes DefaultVillageTypes { get; private set; }
        internal MBReadOnlyList<TraitObject> AllTraits { get; private set; }
        public DefaultFeats DefaultFeats { get; private set; }
        internal MBReadOnlyList<PolicyObject> AllPolicies { get; private set; }
        internal MBReadOnlyList<BuildingType> AllBuildingTypes { get; private set; }
        internal MBReadOnlyList<IssueEffect> AllIssueEffects { get; private set; }
        internal MBReadOnlyList<SiegeStrategy> AllSiegeStrategies { get; private set; }
        internal MBReadOnlyList<VillageType> AllVillageTypes { get; private set; }
        internal MBReadOnlyList<SkillEffect> AllSkillEffects { get; private set; }
        internal MBReadOnlyList<FeatObject> AllFeats { get; private set; }
        internal MBReadOnlyList<SkillObject> AllSkills { get; private set; }
        internal MBReadOnlyList<SiegeEngineType> AllSiegeEngineTypes { get; private set; }
        internal MBReadOnlyList<ItemCategory> AllItemCategories { get; private set; }
        internal MBReadOnlyList<CharacterAttribute> AllCharacterAttributes { get; private set; }
        internal MBReadOnlyList<ItemObject> AllItems { get; private set; }
        internal MapTimeTracker MapTimeTracker { get; private set; }
        public float RestTime { get; internal set; }
        public bool TimeControlModeLock { get; private set; }

        public CampaignTimeControlMode TimeControlMode
        {
            get => _timeControlMode;
            set
            {
                if (TimeControlModeLock || value == _timeControlMode)
                    return;
                _timeControlMode = value;
            }
        }

        public bool IsMapTooltipLongForm { get; set; }
        public float SpeedUpMultiplier { get; set; } = 4f;
        public float CampaignDt => _dt;
        public bool TrueSight { get; set; }
        public static BOFCampaign Current { get; private set; }
        public CampaignTime CampaignStartTime { get; private set; }
        public CampaignGameMode GameMode { get; private set; }
        public GameMenuManager GameMenuManager { get; private set; }
        public GameModels Models => _gameModels;
        public BOFGameManager SandBoxManager { get; private set; }
        public GameLoadingType CampaignGameLoadingType => _gameLoadingType;
        public SiegeEventManager SiegeEventManager { get; internal set; }
        public MapEventManager MapEventManager { get; internal set; }
        public CampaignEvents CampaignEvents { get; private set; }

        public MenuContext CurrentMenuContext
        {
            get
            {
                GameStateManager gameStateManager = CurrentGame.GameStateManager;
                
                if (gameStateManager.ActiveState is TutorialState activeState1)
                    return activeState1.MenuContext;
                
                if (gameStateManager.ActiveState is MapState activeState2)
                    return activeState2.MenuContext;
                
                return gameStateManager.ActiveState.Predecessor != null &&
                       gameStateManager.ActiveState.Predecessor is MapState predecessor
                    ? predecessor.MenuContext
                    : null;
            }
        }

        internal List<MBCampaignEvent> PeriodicCampaignEvents { get; private set; }

        public bool IsMainPartyWaiting
        {
            get => _isMainPartyWaiting;
            private set => _isMainPartyWaiting = value;
        }

        private int _curMapFrame { get; set; }

        public LocatorGrid<Settlement> SettlementLocator
        {
            get
            {
                if (_settlementLocator == null)
                    _settlementLocator = new LocatorGrid<Settlement>();
                return _settlementLocator;
            }
        }

        public LocatorGrid<MobileParty> MobilePartyLocator
        {
            get
            {
                if (_mobilePartyLocator == null)
                    _mobilePartyLocator = new LocatorGrid<MobileParty>();
                return _mobilePartyLocator;
            }
        }

        public IMapScene MapSceneWrapper => _mapSceneWrapper;
        public PlayerEncounter PlayerEncounter { get; set; }
        [CachedData] 
        internal LocationEncounter LocationEncounter { get; set; }
        internal NameGenerator NameGenerator { get; private set; }
        public BarterManager BarterManager { get; private set; }
        public bool IsMainHeroDisguised { get; set; }
        public bool DesertionEnabled { get; set; }
        public int InitialPlayerTotalSkills { get; set; }
        public Vec2 DefaultStartingPosition => new Vec2(685.3f, 410.9f);
        public static float CurrentTime => (float)CampaignTime.Now.ToHours;
        public IList<CampaignEntityComponent> CampaignEntityComponents => _campaignEntitySystem.Components;
        public MBReadOnlyList<Hero> AliveHeroes => CampaignObjectManager.AliveHeroes;
        public MBReadOnlyList<Hero> DeadOrDisabledHeroes => CampaignObjectManager.DeadOrDisabledHeroes;
        public MBReadOnlyList<MobileParty> MobileParties => CampaignObjectManager.MobileParties;
        public MBReadOnlyList<Settlement> Settlements => CampaignObjectManager.Settlements;
        public IEnumerable<IFaction> Factions => CampaignObjectManager.Factions;
        public MBReadOnlyList<Kingdom> Kingdoms => CampaignObjectManager.Kingdoms;
        public MBReadOnlyList<Clan> Clans => CampaignObjectManager.Clans;
        public MBReadOnlyList<CharacterObject> Characters => _characters;
        public MBReadOnlyList<WorkshopType> Workshops => _workshops;
        public MBReadOnlyList<ItemModifier> ItemModifiers => _itemModifiers;
        public MBReadOnlyList<ItemModifierGroup> ItemModifierGroups => _itemModifierGroups;
        public MBReadOnlyList<Concept> Concepts => _concepts;
        public MBReadOnlyList<CharacterObject> TemplateCharacters { get; private set; }
        public MBReadOnlyList<CharacterObject> ChildTemplateCharacters { get; private set; }
        public MobileParty MainParty { get; private set; }

        public PartyBase CameraFollowParty
        {
            get => _cameraFollowParty;
            set => _cameraFollowParty = value;
        }
        
        public CampaignInformationManager CampaignInformationManager { get; set; }
        public VisualTrackerManager VisualTrackerManager { get; set; }
        public LogEntryHistory LogEntryHistory => _logEntryHistory;
        public EncyclopediaManager EncyclopediaManager => _encyclopediaManager;
        public InventoryManager InventoryManager => _inventoryManager;
        public PartyScreenManager PartyScreenManager => _partyScreenManager;
        public ConversationManager ConversationManager => _conversationManager;
        public PartyUpgrader PartyUpgrader => _partyUpgrader;
        public IReadOnlyList<Track> DetectedTracks => _mapTracksCampaignBehavior?.DetectedTracks;
        public bool IsDay => !IsNight;
        public bool IsNight => CampaignTime.Now.IsNightTime;
        public HeroTraitDeveloper PlayerTraitDeveloper { get; private set; }
        public override bool IsPartyWindowAccessibleAtMission => GameMode == CampaignGameMode.Campaign;
        internal IReadOnlyList<Town> AllTowns => _towns;
        internal IReadOnlyList<Town> AllCastles => _castles;
        internal IReadOnlyList<Village> AllVillages => _villages;
        internal IReadOnlyList<Hideout> AllHideouts => _hideouts;

        public int CreateGameMenuIndex()
        {
            int gameMenusCreated = _numGameMenusCreated;
            ++_numGameMenusCreated;
            return gameMenusCreated;
        }

        public event Action WeeklyTicked;

        public void InitializeMainParty()
        {
            InitializeSinglePlayerReferences();
            MainParty.InitializeMobileParty(CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"), DefaultStartingPosition, 0.0f);
            MainParty.ActualClan = Clan.PlayerClan;
            MainParty.PartyComponent = new LordPartyComponent(Hero.MainHero);
            MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 1);
        }

        [LoadInitializationCallback]
        private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
        {
            _campaignEntitySystem = new EntitySystem<CampaignEntityComponent>();
            _partyUpgrader = new PartyUpgrader();

            if (BarterManager == null)
            {
                BarterManager = new BarterManager();
            }
                
            SpeedUpMultiplier = 4f;
            _mobilePartyTickDataCache = new CampaignTickPartyDataCache();

            if (_playerFormationPreferences == null)
            {
                _playerFormationPreferences = new Dictionary<CharacterObject, FormationClass>();
            }
                
            PlayerFormationPreferences = _playerFormationPreferences.GetReadOnlyDictionary();
            
            if (CampaignObjectManager != null)
                return;
            
            CampaignObjectManager = new CampaignObjectManager();
            CampaignObjectManager.SetForceCopyListsForSaveCompability();
        }

        private void InitializeForSavedGame()
        {
            foreach (CampaignEntityComponent component in _campaignEntitySystem.GetComponents())
                component.OnLoadSavedGame();
            
            foreach (Settlement settlement in Settlement.All)
                settlement.Party.OnFinishLoadState();
            
            foreach (MobileParty mobileParty in MobileParties.ToList())
                mobileParty.Party.OnFinishLoadState();
            
            foreach (Settlement settlement in Settlement.All)
                settlement.OnFinishLoadState();
            
            if (Game.Current.GameStateManager.ActiveState is MapState activeState)
                activeState.OnLoad();
            
            GameMenuCallbackManager.OnGameLoad();
            IssueManager.InitializeForSavedGame();
            MinSettlementX = 1000f;
            MinSettlementY = 1000f;
            
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.Position2D.x < (double)MinSettlementX)
                    MinSettlementX = settlement.Position2D.x;
                if (settlement.Position2D.y < (double)MinSettlementY)
                    MinSettlementY = settlement.Position2D.y;
                if (settlement.Position2D.x > (double)MaxSettlementX)
                    MaxSettlementX = settlement.Position2D.x;
                if (settlement.Position2D.y > (double)MaxSettlementY)
                    MaxSettlementY = settlement.Position2D.y;
            }
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            ObjectManager.PreAfterLoad();
            CampaignObjectManager.PreAfterLoad();
            ObjectManager.AfterLoad();
            CampaignObjectManager.AfterLoad();
            CharacterRelationManager.AfterLoad();
            CampaignEventDispatcher.Instance.OnGameEarlyLoaded(starter);
            CampaignEventDispatcher.Instance.OnGameLoaded(starter);
            InitializeForSavedGame();
        }

        private void OnDataLoadFinished(CampaignGameStarter starter)
        {
            _towns = Settlement.All.Where(x => x.IsTown).Select(x => x.Town).ToList();
            _castles = Settlement.All.Where(x => x.IsCastle).Select(x => x.Town).ToList();
            _villages = Settlement.All.Where(x => x.Village != null).Select(x => x.Village).ToList();
            _hideouts = Settlement.All.Where(x => x.IsHideout).Select(x => x.Hideout).ToList();
            
            if (_campaignPeriodicEventManager == null)
                _campaignPeriodicEventManager = new CampaignPeriodicEventManager();
            
            _campaignPeriodicEventManager.InitializeTickers();
            CreateCampaignEvents();
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            CampaignEventDispatcher.Instance.OnSessionStart(starter);
            CampaignEventDispatcher.Instance.OnAfterSessionStart(starter);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
            ConversationManager.Build();
            
            foreach (Settlement settlement in Settlements)
                settlement.OnSessionStart();
            
            IsCraftingEnabled = true;
            IsBannerEditorEnabled = true;
            IsFaceGenEnabled = true;
            MapEventManager.OnAfterLoad();
            KingdomManager.RegisterEvents();
            KingdomManager.OnNewGameCreated();
            CampaignInformationManager.RegisterEvents();
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement.IsVillage)
            {
                settlement.Village.DailyTick();
            }
            else
            {
                if (settlement.Town == null)
                    return;
                
                settlement.Town.DailyTick();
            }
        }

        private void GameInitTick()
        {
            foreach (Settlement settlement in Settlement.All)
                settlement.Party.UpdateVisibilityAndInspected();
            
            foreach (MobileParty mobileParty in MobileParties)
                mobileParty.Party.UpdateVisibilityAndInspected();
        }

        public void HourlyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
        {
            CampaignEventDispatcher.Instance.HourlyTick();
            
            if (!(Game.Current.GameStateManager.ActiveState is MapState activeState))
                return;
            
            activeState.OnHourlyTick();
        }

        public void DailyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
        {
            CampaignEventDispatcher.Instance.DailyTick();
            CampaignEventDispatcher.Instance.AfterDailyTick();
            
            if ((int)CampaignStartTime.ElapsedDaysUntilNow % 7 != 0)
                return;
            
            CampaignEventDispatcher.Instance.WeeklyTick();
            OnWeeklyTick();
        }

        private void OnWeeklyTick()
        {
            LogEntryHistory.DeleteOutdatedLogs();
            
            if (WeeklyTicked == null)
                return;
            
            WeeklyTicked();
        }

        public CampaignTimeControlMode GetSimplifiedTimeControlMode()
        {
            switch (TimeControlMode)
            {
                case CampaignTimeControlMode.Stop:
                    return CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.UnstoppablePlay:
                    return CampaignTimeControlMode.UnstoppablePlay;
                case CampaignTimeControlMode.UnstoppableFastForward:
                case CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime:
                    return CampaignTimeControlMode.UnstoppableFastForward;
                case CampaignTimeControlMode.StoppablePlay:
                    return !IsMainPartyWaiting
                        ? CampaignTimeControlMode.StoppablePlay
                        : CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.StoppableFastForward:
                    return !IsMainPartyWaiting
                        ? CampaignTimeControlMode.StoppableFastForward
                        : CampaignTimeControlMode.Stop;
                default:
                    return CampaignTimeControlMode.Stop;
            }
        }

        private void CheckMainPartyNeedsUpdate() => MobileParty.MainParty.CheckPartyNeedsUpdate();

        private void TickMapTime(float realDt)
        {
            float num1 = 0.0f;
            float speedUpMultiplier = SpeedUpMultiplier;
            float num2 = 0.25f * realDt;
            IsMainPartyWaiting = MobileParty.MainParty.ComputeIsWaiting();
            
            switch (TimeControlMode)
            {
                case CampaignTimeControlMode.Stop:
                case CampaignTimeControlMode.FastForwardStop:
                    _dt = num1;
                    MapTimeTracker.Tick(4320f * num1);
                    break;
                case CampaignTimeControlMode.UnstoppablePlay:
                    num1 = num2;
                    goto case CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.UnstoppableFastForward:
                case CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime:
                    num1 = num2 * speedUpMultiplier;
                    goto case CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.StoppablePlay:
                    if (!IsMainPartyWaiting)
                    {
                        num1 = num2;
                        goto case CampaignTimeControlMode.Stop;
                    }
                    else
                        goto case CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.StoppableFastForward:
                    if (!IsMainPartyWaiting)
                    {
                        num1 = num2 * speedUpMultiplier;
                        goto case CampaignTimeControlMode.Stop;
                    }
                    else
                        goto case CampaignTimeControlMode.Stop;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnGameOver()
        {
            if (!CampaignOptions.IsIronmanMode)
                return;
            
            SaveHandler.QuickSaveCurrentGame();
        }

        internal void RealTick(float realDt)
        {
            CheckMainPartyNeedsUpdate();
            TickMapTime(realDt);
            
            foreach (CampaignEntityComponent component in _campaignEntitySystem.GetComponents())
                component.OnTick(realDt, _dt);
            
            if (!GameStarted)
            {
                GameStarted = true;
                int num = 0;
                
                foreach (SkillObject skill in Skills.All)
                    num += Hero.MainHero.GetSkillValue(skill);
                
                InitialPlayerTotalSkills = num;
                SiegeEventManager.Tick(_dt);
            }

            _mobilePartyTickDataCache.ValidateMobilePartyTickDataCache(MobileParties.Count);
            int index1 = 0;
            foreach (MobileParty mobileParty in MobileParties)
            {
                _mobilePartyTickDataCache.CacheData[index1].mobileParty = mobileParty;
                _mobilePartyTickDataCache.CacheData[index1].isInArmy = mobileParty.Army != null;
                ++index1;
            }

            for (int index2 = 0; index2 < index1; ++index2)
            {
                MobileParty mobileParty = _mobilePartyTickDataCache.CacheData[index2].mobileParty;
                mobileParty.TickForMobileParty(ref _mobilePartyTickDataCache.CacheData[index2].localVariables,_dt, realDt);
                
                if (_mobilePartyTickDataCache.CacheData[index2].isInArmy)
                {
                    _mobilePartyTickDataCache.CacheData[index2].localVariables.nextPathFaceRecord = Current.MapSceneWrapper.GetFaceIndex(_mobilePartyTickDataCache.CacheData[index2].localVariables.nextPosition);
                    mobileParty.TickForMobileParty2(ref _mobilePartyTickDataCache.CacheData[index2].localVariables, realDt);
                }
            }

            int movedPartyCount = 0;
            for (int index3 = 0; index3 < index1; ++index3)
            {
                MobileParty.TickLocalVariables localVariables = _mobilePartyTickDataCache.CacheData[index3].localVariables;
                MobileParty mobileParty = _mobilePartyTickDataCache.CacheData[index3].mobileParty;
                
                if (localVariables.nextMoveDistance > 0.0 && mobileParty.BesiegedSettlement == null &&
                    (!localVariables.hasMapEvent || localVariables.isArmyMember) && !localVariables.isArmyMember)
                {
                    _mobilePartyTickDataCache.PositionArray[movedPartyCount * 2] = localVariables.nextPosition.x;
                    _mobilePartyTickDataCache.PositionArray[movedPartyCount * 2 + 1] = localVariables.nextPosition.y;
                    _mobilePartyTickDataCache.MovedPartiesIndices[movedPartyCount] = index3;
                    
                    ++movedPartyCount;
                }
            }

            Current.MapSceneWrapper.GetFaceIndexForMultiplePositions(movedPartyCount, _mobilePartyTickDataCache.PositionArray, _mobilePartyTickDataCache.ResultArray);
            
            for (int index4 = 0; index4 < movedPartyCount; ++index4)
                _mobilePartyTickDataCache.CacheData[_mobilePartyTickDataCache.MovedPartiesIndices[index4]].localVariables.nextPathFaceRecord = _mobilePartyTickDataCache.ResultArray[index4];
            
            for (int index5 = 0; index5 < index1; ++index5)
            {
                MobileParty mobileParty = _mobilePartyTickDataCache.CacheData[index5].mobileParty;
                
                if (!_mobilePartyTickDataCache.CacheData[index5].isInArmy)
                {
                    mobileParty.TickForMobileParty2(ref _mobilePartyTickDataCache.CacheData[index5].localVariables, realDt);
                }
            }

            foreach (Settlement settlement in Settlement.All)
                settlement.Party.Tick(realDt, _dt);
            
            foreach (MobileParty mobileParty in MobileParties)
                mobileParty.Party.Tick(realDt, _dt);
            
            SiegeEventManager.Tick(_dt);
        }

        public void SetTimeSpeed(int speed)
        {
            switch (speed)
            {
                case 0:
                    if (TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward || TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
                    {
                        TimeControlMode = CampaignTimeControlMode.FastForwardStop;
                        break;
                    }

                    if (TimeControlMode == CampaignTimeControlMode.FastForwardStop || TimeControlMode == CampaignTimeControlMode.Stop)
                        break;
                    
                    TimeControlMode = CampaignTimeControlMode.Stop;
                    break;
                case 1:
                    if ((TimeControlMode == CampaignTimeControlMode.Stop || TimeControlMode == CampaignTimeControlMode.FastForwardStop) && MainParty.IsHolding ||
                        IsMainPartyWaiting || MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
                    {
                        TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
                        break;
                    }

                    TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                    break;
                case 2:
                    if ((TimeControlMode == CampaignTimeControlMode.Stop || TimeControlMode == CampaignTimeControlMode.FastForwardStop) && MainParty.IsHolding ||
                        IsMainPartyWaiting || MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
                    {
                        TimeControlMode = CampaignTimeControlMode.UnstoppableFastForward;
                        break;
                    }

                    TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
                    break;
            }
        }

        internal void Tick()
        {
            ++_curMapFrame;
            ++_curSessionFrame;
            
            if (_dt > 0.0 || _curSessionFrame < 3)
            {
                CampaignEventDispatcher.Instance.Tick(_dt);
                _campaignPeriodicEventManager.TickPartialHourlyAi();
                _campaignPeriodicEventManager.OnTick(_dt);
                PartiesThink(_dt);
                MapEventManager.Tick();
                _lastNonZeroDtFrame = _curMapFrame;
                _campaignPeriodicEventManager.MobilePartyHourlyTick();
                CampaignInformationManager.Tick();
            }

            if (_dt > 0.0)
                _campaignPeriodicEventManager.TickPeriodicEvents();
            
            Current.PlayerCaptivity.Update(_dt);
            
            if (_dt > 0.0 || MobileParty.MainParty.MapEvent == null &&
                _curMapFrame == _lastNonZeroDtFrame + 1)
                EncounterManager.Tick(_dt);
            
            if (!(Game.Current.GameStateManager.ActiveState is MapState activeState) || activeState.AtMenu)
                return;
            
            string genericStateMenu = Models.EncounterGameMenuModel.GetGenericStateMenu();
            if (string.IsNullOrEmpty(genericStateMenu))
                return;
            
            GameMenu.ActivateGameMenu(genericStateMenu);
        }

        private void CreateCampaignEvents()
        {
            long numTicks = (CampaignTime.Now - CampaignData.CampaignStartTime).NumTicks;
            CampaignTime initialWait1 = CampaignTime.Days(1f);
           
            if (numTicks % 864000000L != 0L)
                initialWait1 = CampaignTime.Days(numTicks % 864000000L / 8.64E+08f);
            _dailyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Days(1f), initialWait1);
            _dailyTickEvent.AddHandler(DailyTick);
            CampaignTime initialWait2 = CampaignTime.Hours(0.5f);
            
            if (numTicks % 36000000L != 0L)
                initialWait2 = CampaignTime.Hours(numTicks % 36000000L / 3.6E+07f);
           
            _hourlyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(1f), initialWait2);
            _hourlyTickEvent.AddHandler(HourlyTick);
        }

        private void PartiesThink(float dt)
        {
            foreach (MobileParty mobileParty in MobileParties)
                mobileParty.TickAi(dt);
        }

        public TComponent GetEntityComponent<TComponent>() where TComponent : CampaignEntityComponent => _campaignEntitySystem.GetComponent<TComponent>();
        public TComponent AddEntityComponent<TComponent>() where TComponent : CampaignEntityComponent, new() => _campaignEntitySystem.AddComponent<TComponent>();

        public T GetCampaignBehavior<T>() => _campaignBehaviorManager.GetBehavior<T>();
        public IEnumerable<T> GetCampaignBehaviors<T>() => _campaignBehaviorManager.GetBehaviors<T>();
        public void AddCampaignBehaviorManager(ICampaignBehaviorManager manager) =>  _campaignBehaviorManager = manager;
        public void RemoveTracks(Predicate<Track> predicate) => _mapTracksCampaignBehavior?.RemoveTracks(predicate);

        public void AddMapArrow(
            TextObject pointerName,
            Vec2 trackPosition,
            Vec2 trackDirection,
            float life,
            int numberOfMembers)
        {
            _mapTracksCampaignBehavior?.AddMapArrow(pointerName, trackPosition, trackDirection, life, numberOfMembers);
        }

        public int GeneratePartyId(PartyBase party)
        {
            int lastPartyIndex = _lastPartyIndex;
            ++_lastPartyIndex;
            return lastPartyIndex;
        }

        public void AddTrack(MobileParty target, Vec2 trackPosition, Vec2 trackDirection)
        {
            if (_mapTracksCampaignBehavior.IsTrackDropped(target))
                return;
            
            _mapTracksCampaignBehavior.AddTrack(target, trackPosition, trackDirection);
        }

        private void LoadMapScene()
        {
            _mapSceneWrapper = MapSceneCreator.CreateMapScene();
            _mapSceneWrapper.SetSceneLevels(new List<string>
            {
                "level_1",
                "level_2",
                "level_3",
                "siege",
                "raid",
                "burned"
            });
            _mapSceneWrapper.Load();
            Vec2 minimumPosition;
            Vec2 maximumPosition;
            float maximumHeight;
            _mapSceneWrapper.GetMapBorders(out minimumPosition, out maximumPosition, out maximumHeight);
            MapMinimumPosition = minimumPosition;
            MapMaximumPosition = maximumPosition;
            MapMaximumHeight = maximumHeight;
            MapDiagonal = MapMinimumPosition.Distance(MapMaximumPosition);
        }

        private void InitializeCachedLists()
        {
            MBObjectManager objectManager = Game.Current.ObjectManager;
            _characters = objectManager.GetObjectTypeList<CharacterObject>();
            _workshops = objectManager.GetObjectTypeList<WorkshopType>();
            _itemModifiers = objectManager.GetObjectTypeList<ItemModifier>();
            _itemModifierGroups = objectManager.GetObjectTypeList<ItemModifierGroup>();
            _concepts = objectManager.GetObjectTypeList<Concept>();
            
            TemplateCharacters = _characters
                .Where(x => x.IsTemplate && !x.IsObsolete)
                .ToList().GetReadOnlyList();
            
            ChildTemplateCharacters = _characters
                .Where(x => x.IsChildTemplate && !x.IsObsolete)
                .ToList().GetReadOnlyList();
            
            _mapTracksCampaignBehavior = GetCampaignBehavior<IMapTracksCampaignBehavior>();
        }

        public IEnumerable<MobileParty> GetNearbyMobileParties(
            Vec2 position,
            float radius,
            Func<MobileParty, bool> condition)
        {
            return MobilePartyLocator.FindPartiesAroundPosition(position, radius, condition);
        }

        public override void OnDestroy()
        {
            GameTexts.ClearInstance();
            _mapSceneWrapper?.Destroy();
            ConversationManager.Clear();
            CampaignData.OnGameEnd();
            MBTextManager.ClearAll();
            CampaignSiegeTestStatic.Destruct();
            MBSaveLoad.OnGameDestroy();
            Current = null;
        }

        public void InitializeSinglePlayerReferences()
        {
            IsInitializedSinglePlayerReferences = true;
            InitializeGamePlayReferences();
        }

        private void CreateLists()
        {
            AllPerks = MBObjectManager.Instance.GetObjectTypeList<PerkObject>();
            AllTraits = MBObjectManager.Instance.GetObjectTypeList<TraitObject>();
            AllPolicies = MBObjectManager.Instance.GetObjectTypeList<PolicyObject>();
            AllBuildingTypes = MBObjectManager.Instance.GetObjectTypeList<BuildingType>();
            AllIssueEffects = MBObjectManager.Instance.GetObjectTypeList<IssueEffect>();
            AllSiegeStrategies = MBObjectManager.Instance.GetObjectTypeList<SiegeStrategy>();
            AllVillageTypes = MBObjectManager.Instance.GetObjectTypeList<VillageType>();
            AllSkillEffects = MBObjectManager.Instance.GetObjectTypeList<SkillEffect>();
            AllFeats = MBObjectManager.Instance.GetObjectTypeList<FeatObject>();
            AllSkills = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
            AllSiegeEngineTypes = MBObjectManager.Instance.GetObjectTypeList<SiegeEngineType>();
            AllItemCategories = MBObjectManager.Instance.GetObjectTypeList<ItemCategory>();
            AllCharacterAttributes = MBObjectManager.Instance.GetObjectTypeList<CharacterAttribute>();
            AllItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
        }

        private void CalculateAverageDistanceBetweenTowns()
        {
            if (GameMode == CampaignGameMode.Tutorial)
                return;
            
            float sum = 0.0f;
            int townCount = 0;
            
            foreach (Town allTown1 in AllTowns)
            {
                float num3 = 2.5E+07f;
                foreach (Town allTown2 in AllTowns)
                {
                    if (allTown1 != allTown2)
                    {
                        float num4 = allTown1.Settlement.Position2D.DistanceSquared(allTown2.Settlement.Position2D);
                        if (num4 < (double)num3)
                            num3 = num4;
                    }
                }

                sum += (float)Math.Sqrt(num3);
                ++townCount;
            }

            AverageDistanceBetweenTwoTowns = sum / townCount;
        }

        public void InitializeGamePlayReferences()
        {
            CurrentGame.PlayerTroop =
                (BasicCharacterObject)CurrentGame.ObjectManager.GetObject<CharacterObject>("main_hero");
            if (Hero.MainHero.Mother != null)
                Hero.MainHero.Mother.HasMet = true;
            if (Hero.MainHero.Father != null)
                Hero.MainHero.Father.HasMet = true;
            PlayerDefaultFaction = CampaignObjectManager.Find<Clan>("player_faction");
            Hero.MainHero.Detected = true;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, true);
        }

        private void InitializeScenes()
        {
            // TODO: Point these to correct module paths
            // GameSceneDataManager.Instance.LoadSPBattleScenes(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/sp_battle_scenes.xml");
            // GameSceneDataManager.Instance.LoadConversationScenes(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/conversation_scenes.xml");
            // GameSceneDataManager.Instance.LoadMeetingScenes(ModuleHelper.GetModuleFullPath("Sandbox") + "ModuleData/meeting_scenes.xml");
        }

        public void SetLoadingParameters(GameLoadingType gameLoadingType, int randomSeed)
        {
            Current = this;
            _gameLoadingType = gameLoadingType;
            if (gameLoadingType == GameLoadingType.SavedCampaign)
                return;
            CurrentGame.SetRandomSeed(randomSeed);
        }

        public void SetLoadingParameters(GameLoadingType gameLoadingType)
        {
            int randomSeed = (int)DateTime.Now.Ticks & ushort.MaxValue;
            SetLoadingParameters(gameLoadingType, randomSeed);
        }

        public void AddCampaignEventReceiver(CampaignEventReceiver receiver) => CampaignEventDispatcher.AddCampaignEventReceiver(receiver);

        protected override void OnInitialize()
        {
            CampaignEvents = new CampaignEvents();
            PeriodicCampaignEvents = new List<MBCampaignEvent>();
            CampaignEventDispatcher = new CampaignEventDispatcher(
                (IEnumerable<CampaignEventReceiver>)new CampaignEventReceiver[3]
                {
                    CampaignEvents,
                    IssueManager,
                    QuestManager
                });
            SandBoxManager = Game.Current.AddGameHandler<SandBoxManager>();
            SaveHandler = new SaveHandler();
            VisualCreator = new VisualCreator();
            GameMenuManager = new GameMenuManager();
            
            if (_gameLoadingType != GameLoadingType.Editor)
                CreateManagers();
            
            CampaignGameStarter campaignGameStarter = new CampaignGameStarter(GameMenuManager, ConversationManager, CurrentGame.GameTextManager);
            SandBoxManager.Initialize(campaignGameStarter);
            GameManager.InitializeGameStarter(CurrentGame, campaignGameStarter);
            CurrentGame.SetRandomGenerators();
            
            if (_gameLoadingType == GameLoadingType.NewCampaign || _gameLoadingType == GameLoadingType.SavedCampaign)
                InitializeScenes();
            
            GameManager.OnGameStart(CurrentGame, campaignGameStarter);
            CurrentGame.SetBasicModels(campaignGameStarter.Models);
            _gameModels = CurrentGame.AddGameModelsManager<GameModels>(campaignGameStarter.Models);
            CurrentGame.CreateGameManager();
            
            if (_gameLoadingType == GameLoadingType.SavedCampaign)
            {
                CurrentGame.InitializeOnCampaignStart();
                InitializeDefaultCampaignObjects();
            }

            GameManager.BeginGameStart(CurrentGame);
            
            if (_gameLoadingType != GameLoadingType.SavedCampaign)
                OnNewCampaignStart();
            
            CreateLists();
            InitializeBasicObjectXmls();
            
            if (_gameLoadingType != GameLoadingType.SavedCampaign)
                GameManager.OnNewCampaignStart(CurrentGame, campaignGameStarter);
            
            if (_gameLoadingType == GameLoadingType.SavedCampaign)
                CampaignObjectManager.InitializeForOldSaves();
            
            SandBoxManager.OnCampaignStart(campaignGameStarter, GameManager, _gameLoadingType == GameLoadingType.SavedCampaign);
           
            if (_gameLoadingType != GameLoadingType.SavedCampaign)
            {
                AddCampaignBehaviorManager(new CampaignBehaviorManager(campaignGameStarter.CampaignBehaviors));
                GameManager.OnAfterCampaignStart(CurrentGame);
            }
            else
            {
                SandBoxManager.OnGameLoaded(campaignGameStarter);
                GameManager.OnGameLoaded(CurrentGame, campaignGameStarter);
                _campaignBehaviorManager.InitializeCampaignBehaviors(campaignGameStarter.CampaignBehaviors);
                _campaignBehaviorManager.RegisterEvents();
                _campaignBehaviorManager.OnGameLoaded();
            }

            Current.GetCampaignBehavior<ICraftingCampaignBehavior>()?.InitializeCraftingElements();
            campaignGameStarter.UnregisterNonReadyObjects();

            if (_gameLoadingType == GameLoadingType.SavedCampaign)
            {
                InitializeCampaignObjectsOnAfterLoad();
            }
            else if (_gameLoadingType == GameLoadingType.NewCampaign || _gameLoadingType == GameLoadingType.Tutorial)
            {
                CampaignObjectManager.InitializeOnNewGame();
            }
            
            InitializeCachedLists();
            NameGenerator.Initialize();
            CurrentGame.OnGameStart();
            GameManager.OnGameInitializationFinished(CurrentGame);
        }

        private void CalculateCachedStatsOnLoad() => ItemRoster.CalculateCachedStatsOnLoad();

        private void InitializeBasicObjectXmls()
        {
            ObjectManager.LoadXML("SPCultures");
            ObjectManager.LoadXML("Concepts");
        }

        private void InitializeDefaultCampaignObjects()
        {
            var campaign = this;
            campaign.DefaultIssueEffects = new DefaultIssueEffects();
            campaign.DefaultTraits = new DefaultTraits();
            campaign.DefaultPolicies = new DefaultPolicies();
            campaign.DefaultPerks = new DefaultPerks();
            campaign.DefaultBuildingTypes = new DefaultBuildingTypes();
            campaign.DefaultVillageTypes = new DefaultVillageTypes();
            campaign.DefaultSiegeStrategies = new DefaultSiegeStrategies();
            campaign.DefaultSkillEffects = new DefaultSkillEffects();
            campaign.DefaultFeats = new DefaultFeats();
            campaign.PlayerUpdateTracker = new PlayerUpdateTracker();
        }

        private void InitializeManagers()
        {
            var campaign = this;
            campaign.KingdomManager = new KingdomManager();
            campaign.CampaignInformationManager = new CampaignInformationManager();
            campaign.VisualTrackerManager = new VisualTrackerManager();
            campaign.TournamentManager = new TournamentManager();
        }

        private void InitializeCampaignObjectsOnAfterLoad()
        {
            CampaignObjectManager.InitializeOnLoad();
            FactionManager.AfterLoad();
            AllPerks = new MBReadOnlyList<PerkObject>(AllPerks.Where(x => !x.IsTrash).ToList());
            LogEntryHistory.OnAfterLoad();
            
            foreach (Kingdom kingdom in Kingdoms)
            {
                foreach (Army army in kingdom.Armies)
                {
                    army.OnAfterLoad();
                }
            }
        }

        private void OnNewCampaignStart()
        {
            Game.Current.PlayerTroop = null;
            MapStateData = new MapStateData();
            CurrentGame.InitializeOnCampaignStart();
            InitializeDefaultCampaignObjects();
            MainParty = MBObjectManager.Instance.CreateObject<MobileParty>("player_party");
            MainParty.SetAsMainParty();
            InitializeManagers();
        }

        protected override void BeforeRegisterTypes(MBObjectManager objectManager) => objectManager.RegisterNonSerializedType<FeatObject>("Feat", "Feats", 0U);

        protected override void OnRegisterTypes(MBObjectManager objectManager)
        {
            objectManager.RegisterType<MobileParty>("MobileParty", "MobileParties", 14U, isTemporary: true);
            objectManager.RegisterType<CharacterObject>("NPCCharacter", "NPCCharacters", 16U);
            
            if (GameMode == CampaignGameMode.Tutorial)
                objectManager.RegisterType<BasicCharacterObject>("NPCCharacter", "MPCharacters", 43U);
            
            objectManager.RegisterType<CultureObject>("Culture", "SPCultures", 17U);
            objectManager.RegisterType<Clan>("Faction", "Factions", 18U, isTemporary: true);
            objectManager.RegisterType<PerkObject>("Perk", "Perks", 19U);
            objectManager.RegisterType<Kingdom>("Kingdom", "Kingdoms", 20U, isTemporary: true);
            objectManager.RegisterType<TraitObject>("Trait", "Traits", 21U);
            objectManager.RegisterType<VillageType>("VillageType", "VillageTypes", 22U);
            objectManager.RegisterType<BuildingType>("BuildingType", "BuildingTypes", 23U);
            objectManager.RegisterType<PartyTemplateObject>("PartyTemplate", "partyTemplates", 24U);
            objectManager.RegisterType<Settlement>("Settlement", "Settlements", 25U);
            objectManager.RegisterType<WorkshopType>("WorkshopType", "WorkshopTypes", 26U);
            objectManager.RegisterType<Village>("Village", "Components", 27U);
            objectManager.RegisterType<Hideout>("Hideout", "Components", 30U);
            objectManager.RegisterType<Town>("Town", "Components", 31U);
            objectManager.RegisterType<Hero>("Hero", "Heroes", 32U, isTemporary: true);
            objectManager.RegisterType<MenuContext>("MenuContext", "MenuContexts", 35U);
            objectManager.RegisterType<PolicyObject>("Policy", "Policies", 36U);
            objectManager.RegisterType<Concept>("Concept", "Concepts", 37U);
            objectManager.RegisterType<IssueEffect>("IssueEffect", "IssueEffects", 39U);
            objectManager.RegisterType<SiegeStrategy>("SiegeStrategy", "SiegeStrategies", 40U);
            objectManager.RegisterNonSerializedType<SkillEffect>("SkillEffect", "SkillEffects", 53U);
            objectManager.RegisterNonSerializedType<LocationComplexTemplate>("LocationComplexTemplate","LocationComplexTemplates", 42U);
        }

        private void CreateManagers()
        {
            _encyclopediaManager = new EncyclopediaManager();
            _inventoryManager = new InventoryManager();
            _partyScreenManager = new PartyScreenManager();
            _conversationManager = new ConversationManager();
            NameGenerator = new NameGenerator();
        }

        private void OnNewGameCreated(CampaignGameStarter gameStarter)
        {
            OnNewGameCreatedInternal();
            SandBoxManager.OnNewGameCreated(gameStarter);
            GameManager?.OnNewGameCreated(CurrentGame, gameStarter);
            CampaignEventDispatcher.Instance.OnNewGameCreated(gameStarter);
            OnAfterNewGameCreatedInternal();
        }

        private void OnNewGameCreatedInternal()
        {
            CheatFindItemRangeBegin = 0;
            UniqueGameId = MiscHelper.GenerateCampaignId(12);
            PlayerTraitDeveloper = new HeroTraitDeveloper(Hero.MainHero);
            TimeControlMode = CampaignTimeControlMode.Stop;
            _campaignEntitySystem = new EntitySystem<CampaignEntityComponent>();
            SiegeEventManager = new SiegeEventManager();
            MapEventManager = new MapEventManager(CurrentGame);
            autoEnterTown = null;
            MinSettlementX = 1000f;
            MinSettlementY = 1000f;
            
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.Position2D.x < (double)MinSettlementX)
                    MinSettlementX = settlement.Position2D.x;
                if (settlement.Position2D.y < (double)MinSettlementY)
                    MinSettlementY = settlement.Position2D.y;
                if (settlement.Position2D.x > (double)MaxSettlementX)
                    MaxSettlementX = settlement.Position2D.x;
                if (settlement.Position2D.y > (double)MaxSettlementY)
                    MaxSettlementY = settlement.Position2D.y;
            }

            CampaignBehaviorManager.RegisterEvents();
            CameraFollowParty = MainParty.Party;
        }

        private void OnAfterNewGameCreatedInternal()
        {
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, Hero.MainHero.Gold, true);
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, true);
            Hero.MainHero.Clan.Influence = 0.0f;
            Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
            GameInitTick();
            _playerFormationPreferences = new Dictionary<CharacterObject, FormationClass>();
            PlayerFormationPreferences =
                _playerFormationPreferences.GetReadOnlyDictionary();
            Current.DesertionEnabled = true;
        }

        protected override void DoLoadingForGameType(
            GameTypeLoadingStates gameTypeLoadingState,
            out GameTypeLoadingStates nextState)
        {
            nextState = GameTypeLoadingStates.None;
            switch (gameTypeLoadingState)
            {
                case GameTypeLoadingStates.InitializeFirstStep:
                    CurrentGame.Initialize();
                    nextState = GameTypeLoadingStates.WaitSecondStep;
                    break;
                case GameTypeLoadingStates.WaitSecondStep:
                    nextState = GameTypeLoadingStates.LoadVisualsThirdState;
                    break;
                case GameTypeLoadingStates.LoadVisualsThirdState:
                    if (GameMode == CampaignGameMode.Campaign)
                        LoadMapScene();
                    nextState = GameTypeLoadingStates.PostInitializeFourthState;
                    break;
                case GameTypeLoadingStates.PostInitializeFourthState:
                    CampaignGameStarter gameStarter = SandBoxManager.GameStarter;
                    if (_gameLoadingType == GameLoadingType.SavedCampaign)
                    {
                        OnDataLoadFinished(gameStarter);
                        CalculateAverageDistanceBetweenTowns();
                        DetermineModules();
                        MapEventManager.OnGameInitialized();
                        
                        foreach (Settlement settlement in Settlement.All)
                            settlement.Party.OnGameInitialized();
                        
                        foreach (MobileParty mobileParty in MobileParties.ToList())
                            mobileParty.Party.OnGameInitialized();
                        
                        CalculateCachedStatsOnLoad();
                        OnGameLoaded(gameStarter);
                        OnSessionStart(gameStarter);
                        
                        foreach (Hero allAliveHero in Hero.AllAliveHeroes)
                            allAliveHero.CheckInvalidEquipmentsAndReplaceIfNeeded();
                        
                        foreach (Hero deadOrDisabledHero in Hero.DeadOrDisabledHeroes)
                            deadOrDisabledHero.CheckInvalidEquipmentsAndReplaceIfNeeded();
                    }
                    else if (_gameLoadingType == GameLoadingType.NewCampaign)
                    {
                        OnDataLoadFinished(gameStarter);
                        CalculateAverageDistanceBetweenTowns();
                        MBSaveLoad.OnNewGame();
                        InitializeMainParty();
                        DetermineModules();
                        
                        foreach (Settlement settlement in Settlement.All)
                            settlement.Party.OnGameInitialized();
                        
                        foreach (MobileParty mobileParty in MobileParties.ToList())
                            mobileParty.Party.OnGameInitialized();
                        
                        foreach (Settlement settlement in Settlement.All)
                            settlement.OnGameCreated();
                        
                        foreach (Clan clan in Clan.All)
                            clan.OnGameCreated();
                        
                        MBObjectManager.Instance.RemoveTemporaryTypes();
                        OnNewGameCreated(gameStarter);
                        OnSessionStart(gameStarter);
                        Debug.Print("Finished starting a new game.");
                    }

                    GameManager.OnAfterGameInitializationFinished(CurrentGame, gameStarter);
                    break;
            }
        }

        private void DetermineModules()
        {
            if (_previouslyUsedModules == null)
                _previouslyUsedModules = new List<string>();
            
            foreach (string moduleName in SandBoxManager.Instance.ModuleManager.ModuleNames)
            {
                if (!_previouslyUsedModules.Contains(moduleName))
                    _previouslyUsedModules.Add(moduleName);
            }
        }

        public override void OnMissionIsStarting(string missionName, MissionInitializerRecord rec)
        {
            if (!rec.PlayingInCampaignMode)
                return;
            
            CampaignEventDispatcher.Instance.BeforeMissionOpened();
        }

        // TODO: Point this to correct module 
        public override void InitializeParameters() => ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_campaign_parameters"));
        public void SetTimeControlModeLock(bool isLocked) => TimeControlModeLock = isLocked;
        
        public void OnPlayerCharacterChanged()
        {
            MainParty = Hero.MainHero.PartyBelongedTo;
            if (Hero.MainHero.CurrentSettlement != null && !Hero.MainHero.IsPrisoner)
            {
                if (MainParty == null)
                    LeaveSettlementAction.ApplyForCharacterOnly(Hero.MainHero);
                else
                    LeaveSettlementAction.ApplyForParty(MainParty);
            }

            if (Hero.MainHero.IsFugitive)
                Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
            
            PlayerTraitDeveloper = new HeroTraitDeveloper(Hero.MainHero);
            
            if (MainParty == null)
            {
                MainParty = MobileParty.CreateParty("player_party_" + Hero.MainHero.StringId);
                MainParty.ActualClan = Clan.PlayerClan;
                
                if (Hero.MainHero.IsPrisoner)
                {
                    MainParty.InitializeMobileParty(
                        CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"),
                        Hero.MainHero.GetPosition().AsVec2, 0.0f, troopNumberLimit: 0);
                    MainParty.IsActive = false;
                }
                else
                {
                    Vec3 position1 = Hero.MainHero.GetPosition();
                    Vec2 vec2;
                    
                    if (!(position1.AsVec2 != Vec2.Zero))
                    {
                        vec2 = SettlementHelper.FindRandomSettlement(s =>
                            s.OwnerClan != null && !s.OwnerClan.IsAtWarWith(Clan.PlayerClan)).GetPosition2D;
                    }
                    else
                    {
                        position1 = Hero.MainHero.GetPosition();
                        vec2 = position1.AsVec2;
                    }

                    Vec2 position2 = vec2;
                    
                    MainParty.InitializeMobileParty(CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"), position2, 0.0f, troopNumberLimit: 0);
                    MainParty.IsActive = true;
                    MainParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1, true);
                }
            }
            else
            {
                Current.MainParty.IsVisible = true;
            }
                

            Current.MainParty.SetAsMainParty();
            PartyBase.MainParty.ItemRoster.UpdateVersion();
            PartyBase.MainParty.MemberRoster.UpdateVersion();
            
            if (MobileParty.MainParty.IsActive)
            {
                PartyBase.MainParty.SetAsCameraFollowParty();
                PartyBase.MainParty.Visuals.SetMapIconAsDirty();
                PartyBase.MainParty.Visuals.Tick(0.0f, 0.15f, PartyBase.MainParty);
                Current.MainParty.IsVisible = true;
            }

            if (Hero.MainHero.Mother != null)
                Hero.MainHero.Mother.HasMet = true;
            
            if (Hero.MainHero.Father != null)
                Hero.MainHero.Father.HasMet = true;
            
            MainParty.PaymentLimit = Current.Models.PartyWageModel.MaxWage;
        }

        public void SetPlayerFormationPreference(CharacterObject character, FormationClass formation)
        {
            if (!_playerFormationPreferences.ContainsKey(character))
                _playerFormationPreferences.Add(character, formation);
            else
                _playerFormationPreferences[character] = formation;
        }

        public override void OnStateChanged(GameState oldState)
        {
        }

        private struct PartyTickCachePerParty
        {
            internal MobileParty mobileParty;
            internal MobileParty.TickLocalVariables localVariables;
            internal bool isInArmy;
        }

        private class CampaignTickPartyDataCache
        {
            public CampaignTickPartyDataCache() => CurrentCapacity = 0;

            public PartyTickCachePerParty[] CacheData { get; private set; }

            public PathFaceRecord[] ResultArray { get; private set; }

            public float[] PositionArray { get; private set; }

            public int[] MovedPartiesIndices { get; private set; }

            public int CurrentCapacity { get; private set; }

            public void ValidateMobilePartyTickDataCache(int requestedCapacity)
            {
                if (CurrentCapacity >= requestedCapacity)
                    return;
                
                int length = (int)(requestedCapacity * 1.10000002384186);
                CacheData = new PartyTickCachePerParty[length];
                ResultArray = new PathFaceRecord[length];
                PositionArray = new float[length * 2];
                MovedPartiesIndices = new int[length];
                CurrentCapacity = length;
            }
        }
    }
}