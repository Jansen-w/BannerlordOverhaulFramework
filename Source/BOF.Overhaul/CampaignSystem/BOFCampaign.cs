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
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using Debug = System.Diagnostics.Debug;
using ManagedParameters = TaleWorlds.Core.ManagedParameters;

namespace BOF.CampaignSystem.CampaignSystem
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

        public const float ConfigTimeMultiplier = 0.25f;

        /*[SaveableField(64)]*/
        private readonly LogEntryHistory _logEntryHistory = new LogEntryHistory();

        /*[SaveableField(2)]*/
        public readonly CampaignOptions Options;

        /*[SaveableField(61)]*/
        private PartyBase _cameraFollowParty;

        /*[SaveableField(7)]*/
        private ICampaignBehaviorManager _campaignBehaviorManager;

        /*[SaveableField(51)]*/
        private EntitySystem<CampaignEntityComponent> _campaignEntitySystem;

        /*[SaveableField(210)]*/
        private CampaignPeriodicEventManager _campaignPeriodicEventManager;
        private List<Town> _castles;
        private MBReadOnlyList<CharacterObject> _characters;

        private MBReadOnlyList<Concept> _concepts;

        private ConversationManager _conversationManager;
        private int _curSessionFrame;
        private MBCampaignEvent _dailyTickEvent;
        [CachedData] private float _dt;
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

        /*[SaveableField(53)]*/
        private bool _isMainPartyWaiting;
        private MBReadOnlyList<ItemModifierGroup> _itemModifierGroups;
        private MBReadOnlyList<ItemModifier> _itemModifiers;
        [CachedData] private int _lastNonZeroDtFrame;

        /*[SaveableField(79)]*/
        private int _lastPartyIndex;

        private IMapScene _mapSceneWrapper;
        private IMapTracksCampaignBehavior _mapTracksCampaignBehavior;
        private LocatorGrid<MobileParty> _mobilePartyLocator;

        private CampaignTickPartyDataCache _mobilePartyTickDataCache =
            new CampaignTickPartyDataCache();

        /*[SaveableField(29)]*/
        private int _numGameMenusCreated;
        private PartyScreenManager _partyScreenManager;
        public PartyUpgrader _partyUpgrader = new PartyUpgrader();

        /*[SaveableField(77)]*/
        private Dictionary<CharacterObject, FormationClass> _playerFormationPreferences;

        /*[SaveableField(78)]*/
        private List<string> _previouslyUsedModules;

        private LocatorGrid<Settlement> _settlementLocator;
        private int _stepNo;
        private CampaignTimeControlMode _timeControlMode;
        private List<Town> _towns;
        private List<Village> _villages;
        private MBReadOnlyList<WorkshopType> _workshops;

        /*[SaveableField(44)]*/
        public PartyBase autoEnterTown;

        /*[SaveableField(23)]*/
        public int CheatFindItemRangeBegin;
        public ConversationContext CurrentConversationContext;

        /*[SaveableField(49)]*/
        public GameLogs GameLogs = new GameLogs();

        /*[SaveableField(34)]*/
        public bool GameStarted;
        public int InfluenceValueTermsOfGold = 10;

        /*[SaveableField(27)]*/
        public bool IsInitializedSinglePlayerReferences;

        /*[SaveableField(65)]*/
        public KingdomManager KingdomManager;

        /*[SaveableField(31)]*/
        public CampaignTimeControlMode LastTimeControlMode = CampaignTimeControlMode.UnstoppablePlay;

        /*[SaveableField(30)]*/
        public int MainHeroIllDays = -1;
        public float MaxSettlementX;

        public float MaxSettlementY;
        public float MinSettlementX;
        public float MinSettlementY;

        public MBReadOnlyDictionary<CharacterObject, FormationClass> PlayerFormationPreferences;

        /*[SaveableField(13)]*/
        public ITournamentManager TournamentManager;

        public bool UseFreeCameraAtMapScreen;

        public BOFCampaign(CampaignGameMode gameMode)
        {
            this.GameMode = gameMode;
            this.Options = new CampaignOptions();
            this.MapTimeTracker = new MapTimeTracker(CampaignData.CampaignStartTime);
            this.CampaignStartTime = this.MapTimeTracker.Now;
            this.CampaignObjectManager = new CampaignObjectManager();
            this.CurrentConversationContext = ConversationContext.Default;
            this.QuestManager = new QuestManager();
            this.IssueManager = new IssueManager();
            this.FactionManager = new FactionManager();
            this.CharacterRelationManager = new CharacterRelationManager();
            this.Romance = new Romance();
            this.PlayerCaptivity = new PlayerCaptivity();
            this.BarterManager = new BarterManager();
            this.AdjustedRandom = new AdjustedRandom();
            this.GameMenuCallbackManager = new GameMenuCallbackManager();
        }

        public static float MapDiagonal { get; private set; }

        public static Vec2 MapMinimumPosition { get; private set; }

        public static Vec2 MapMaximumPosition { get; private set; }

        public static float MapMaximumHeight { get; private set; }

        public static float AverageDistanceBetweenTwoTowns { get; private set; }

        public IReadOnlyList<string> PreviouslyUsedModules => (IReadOnlyList<string>)this._previouslyUsedModules;

        public CampaignEventDispatcher CampaignEventDispatcher { get; private set; }

        /*[SaveableProperty(80)]*/
        public string UniqueGameId { get; private set; }

        public SaveHandler SaveHandler { get; private set; }

        public override bool SupportsSaving => this.GameMode == CampaignGameMode.Campaign;

        /*[SaveableProperty(211)]*/
        public CampaignObjectManager CampaignObjectManager { get; private set; }

        public override bool IsDevelopment => this.GameMode == CampaignGameMode.Tutorial;

        /*[SaveableProperty(3)]*/
        public bool IsCraftingEnabled { get; set; } = true;

        /*[SaveableProperty(4)]*/
        public bool IsBannerEditorEnabled { get; set; } = true;

        /*[SaveableProperty(5)]*/
        public bool IsFaceGenEnabled { get; set; } = true;

        public ICampaignBehaviorManager CampaignBehaviorManager => this._campaignBehaviorManager;

        /*[SaveableProperty(8)]*/
        public QuestManager QuestManager { get; private set; }

        /*[SaveableProperty(9)]*/
        public IssueManager IssueManager { get; private set; }

        /*[SaveableProperty(11)]*/
        public FactionManager FactionManager { get; private set; }

        /*[SaveableProperty(12)]*/
        public CharacterRelationManager CharacterRelationManager { get; private set; }

        /*[SaveableProperty(14)]*/
        public Romance Romance { get; private set; }

        /*[SaveableProperty(16)]*/
        public PlayerCaptivity PlayerCaptivity { get; private set; }

        /*[SaveableProperty(17)]*/
        public Clan PlayerDefaultFaction { get; set; }

        public ICampaignMissionManager CampaignMissionManager { get; set; }

        public ICampaignMapConversation CampaignMapConversationManager { get; set; }

        public IMapSceneCreator MapSceneCreator { get; set; }

        /*[SaveableProperty(21)]*/
        public AdjustedRandom AdjustedRandom { get; private set; }

        public override bool IsInventoryAccessibleAtMission => this.GameMode == CampaignGameMode.Tutorial;

        /*[SaveableProperty(22)]*/
        public GameMenuCallbackManager GameMenuCallbackManager { get; private set; }

        public VisualCreator VisualCreator { get; set; }

        public Monster HumanMonsterSettlement => this._humanMonsterSettlement ?? (this._humanMonsterSettlement =
            this.CurrentGame.ObjectManager.GetObject<Monster>("human_settlement"));

        public Monster HumanChildMonster => this._humanChildMonster ??
                                            (this._humanChildMonster =
                                                this.CurrentGame.ObjectManager.GetObject<Monster>("human_child"));

        public Monster HumanMonsterSettlementSlow => this._humanMonsterSettlementSlow ??
                                                     (this._humanMonsterSettlementSlow =
                                                         this.CurrentGame.ObjectManager.GetObject<Monster>(
                                                             "human_settlement_slow"));

        public Monster HumanMonsterSettlementFast => this._humanMonsterSettlementFast ??
                                                     (this._humanMonsterSettlementFast =
                                                         this.CurrentGame.ObjectManager.GetObject<Monster>(
                                                             "human_settlement_fast"));

        public Monster HumanMonsterMap => this._humanMonsterMap ??
                                          (this._humanMonsterMap =
                                              this.CurrentGame.ObjectManager.GetObject<Monster>("human_map"));

        /*[SaveableProperty(28)]*/
        public MapStateData MapStateData { get; private set; }

        public DefaultPerks DefaultPerks { get; private set; }

        public DefaultTraits DefaultTraits { get; private set; }

        public DefaultPolicies DefaultPolicies { get; private set; }

        public DefaultBuildingTypes DefaultBuildingTypes { get; private set; }

        public DefaultIssueEffects DefaultIssueEffects { get; private set; }

        public DefaultSiegeStrategies DefaultSiegeStrategies { get; private set; }

        public MBReadOnlyList<PerkObject> AllPerks { get; private set; }

        public PlayerUpdateTracker PlayerUpdateTracker { get; private set; }

        public DefaultSkillEffects DefaultSkillEffects { get; private set; }

        public DefaultVillageTypes DefaultVillageTypes { get; private set; }

        public MBReadOnlyList<TraitObject> AllTraits { get; private set; }

        public DefaultCulturalFeats DefaultFeats { get; private set; }

        public MBReadOnlyList<PolicyObject> AllPolicies { get; private set; }

        public MBReadOnlyList<BuildingType> AllBuildingTypes { get; private set; }

        public MBReadOnlyList<IssueEffect> AllIssueEffects { get; private set; }

        public MBReadOnlyList<SiegeStrategy> AllSiegeStrategies { get; private set; }

        public MBReadOnlyList<VillageType> AllVillageTypes { get; private set; }

        public MBReadOnlyList<SkillEffect> AllSkillEffects { get; private set; }

        public MBReadOnlyList<FeatObject> AllFeats { get; private set; }

        public MBReadOnlyList<SkillObject> AllSkills { get; private set; }

        public MBReadOnlyList<SiegeEngineType> AllSiegeEngineTypes { get; private set; }

        public MBReadOnlyList<ItemCategory> AllItemCategories { get; private set; }

        public MBReadOnlyList<CharacterAttribute> AllCharacterAttributes { get; private set; }

        public MBReadOnlyList<ItemObject> AllItems { get; private set; }

        /*[SaveableProperty(100)]*/
        public MapTimeTracker MapTimeTracker { get; private set; }

        public float RestTime { get; set; }

        public bool TimeControlModeLock { get; private set; }

        public CampaignTimeControlMode TimeControlMode
        {
            get => this._timeControlMode;
            set
            {
                if (this.TimeControlModeLock || value == this._timeControlMode)
                    return;
                this._timeControlMode = value;
            }
        }

        public bool IsMapTooltipLongForm { get; set; }

        public float SpeedUpMultiplier { get; set; } = 4f;

        public float CampaignDt => this._dt;

        public bool TrueSight { get; set; }

        public static BOFCampaign Current { get; private set; }

        /*[SaveableProperty(36)]*/
        public CampaignTime CampaignStartTime { get; private set; }

        /*[SaveableProperty(37)]*/
        public CampaignGameMode GameMode { get; private set; }

        public GameMenuManager GameMenuManager { get; private set; }

        public GameModels Models => this._gameModels;

        public SandBoxManager SandBoxManager { get; private set; }

        public GameLoadingType CampaignGameLoadingType => this._gameLoadingType;

        /*[SaveableProperty(40)]*/
        public SiegeEventManager SiegeEventManager { get; set; }

        /*[SaveableProperty(41)]*/
        public MapEventManager MapEventManager { get; set; }

        public CampaignEvents CampaignEvents { get; private set; }

        public MenuContext CurrentMenuContext
        {
            get
            {
                GameStateManager gameStateManager = this.CurrentGame.GameStateManager;
                if (gameStateManager.ActiveState is TutorialState activeState1)
                    return activeState1.MenuContext;
                if (gameStateManager.ActiveState is MapState activeState2)
                    return activeState2.MenuContext;
                return gameStateManager.ActiveState.Predecessor != null &&
                       gameStateManager.ActiveState.Predecessor is MapState predecessor
                    ? predecessor.MenuContext
                    : (MenuContext)null;
            }
        }

        public List<MBCampaignEvent> PeriodicCampaignEvents { get; private set; }

        public bool IsMainPartyWaiting
        {
            get => this._isMainPartyWaiting;
            private set => this._isMainPartyWaiting = value;
        }

        /*[SaveableProperty(45)]*/
        private int _curMapFrame { get; set; }

        public LocatorGrid<Settlement> SettlementLocator
        {
            get
            {
                if (this._settlementLocator == null)
                    this._settlementLocator = new LocatorGrid<Settlement>();
                return this._settlementLocator;
            }
        }

        public LocatorGrid<MobileParty> MobilePartyLocator
        {
            get
            {
                if (this._mobilePartyLocator == null)
                    this._mobilePartyLocator = new LocatorGrid<MobileParty>();
                return this._mobilePartyLocator;
            }
        }

        public IMapScene MapSceneWrapper => this._mapSceneWrapper;

        /*[SaveableProperty(54)]*/
        public PlayerEncounter PlayerEncounter { get; set; }

        [CachedData] public LocationEncounter LocationEncounter { get; set; }

        public NameGenerator NameGenerator { get; private set; }

        /*[SaveableProperty(58)]*/
        public BarterManager BarterManager { get; private set; }

        /*[SaveableProperty(69)]*/
        public bool IsMainHeroDisguised { get; set; }

        /*[SaveableProperty(70)]*/
        public bool DesertionEnabled { get; set; }

        /*[SaveableProperty(76)]*/
        public int InitialPlayerTotalSkills { get; set; }

        public Vec2 DefaultStartingPosition => new Vec2(685.3f, 410.9f);

        public static float CurrentTime => (float)CampaignTime.Now.ToHours;

        public IList<CampaignEntityComponent> CampaignEntityComponents => this._campaignEntitySystem.Components;

        public MBReadOnlyList<Hero> AliveHeroes => this.CampaignObjectManager.AliveHeroes;

        public MBReadOnlyList<Hero> DeadOrDisabledHeroes => this.CampaignObjectManager.DeadOrDisabledHeroes;

        public MBReadOnlyList<MobileParty> MobileParties => this.CampaignObjectManager.MobileParties;

        public MBReadOnlyList<Settlement> Settlements => this.CampaignObjectManager.Settlements;

        public IEnumerable<IFaction> Factions => (IEnumerable<IFaction>)this.CampaignObjectManager.Factions;

        public MBReadOnlyList<Kingdom> Kingdoms => this.CampaignObjectManager.Kingdoms;

        public MBReadOnlyList<Clan> Clans => this.CampaignObjectManager.Clans;

        public MBReadOnlyList<CharacterObject> Characters => this._characters;

        public MBReadOnlyList<WorkshopType> Workshops => this._workshops;

        public MBReadOnlyList<ItemModifier> ItemModifiers => this._itemModifiers;

        public MBReadOnlyList<ItemModifierGroup> ItemModifierGroups => this._itemModifierGroups;

        public MBReadOnlyList<Concept> Concepts => this._concepts;

        public MBReadOnlyList<CharacterObject> TemplateCharacters { get; private set; }

        public MBReadOnlyList<CharacterObject> ChildTemplateCharacters { get; private set; }

        /*[SaveableProperty(60)]*/
        public MobileParty MainParty { get; private set; }

        public PartyBase CameraFollowParty
        {
            get => this._cameraFollowParty;
            set => this._cameraFollowParty = value;
        }

        /*[SaveableProperty(62)]*/
        public CampaignInformationManager CampaignInformationManager { get; set; }

        /*[SaveableProperty(63)]*/
        public VisualTrackerManager VisualTrackerManager { get; set; }

        public LogEntryHistory LogEntryHistory => this._logEntryHistory;

        public EncyclopediaManager EncyclopediaManager => this._encyclopediaManager;

        public InventoryManager InventoryManager => this._inventoryManager;

        public PartyScreenManager PartyScreenManager => this._partyScreenManager;

        public ConversationManager ConversationManager => this._conversationManager;

        public PartyUpgrader PartyUpgrader => this._partyUpgrader;

        public IReadOnlyList<Track> DetectedTracks => this._mapTracksCampaignBehavior?.DetectedTracks;

        public bool IsDay => !this.IsNight;

        public bool IsNight => CampaignTime.Now.IsNightTime;

        /*[SaveableProperty(68)]*/
        public HeroTraitDeveloper PlayerTraitDeveloper { get; private set; }

        public override bool IsPartyWindowAccessibleAtMission => this.GameMode == CampaignGameMode.Campaign;

        public IReadOnlyList<Town> AllTowns => (IReadOnlyList<Town>)this._towns;

        public IReadOnlyList<Town> AllCastles => (IReadOnlyList<Town>)this._castles;

        public IReadOnlyList<Village> AllVillages => (IReadOnlyList<Village>)this._villages;

        public IReadOnlyList<Hideout> AllHideouts => (IReadOnlyList<Hideout>)this._hideouts;

        public int CreateGameMenuIndex()
        {
            int gameMenusCreated = this._numGameMenusCreated;
            ++this._numGameMenusCreated;
            return gameMenusCreated;
        }

        public event Action WeeklyTicked;

        public void InitializeMainParty()
        {
            this.InitializeSinglePlayerReferences();
            this.MainParty.InitializeMobilePartyAtPosition(
                this.CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"),
                this.DefaultStartingPosition);
            this.MainParty.ActualClan = Clan.PlayerClan;
            this.MainParty.PartyComponent = (PartyComponent)new LordPartyComponent(Hero.MainHero, Hero.MainHero);
            this.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 1);
        }

        // [LoadInitializationCallback]
        // private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
        // {
        //     this._campaignEntitySystem = new EntitySystem<CampaignEntityComponent>();
        //     this._partyUpgrader = new PartyUpgrader();
        //     if (this.BarterManager == null)
        //         this.BarterManager = new BarterManager();
        //     this.SpeedUpMultiplier = 4f;
        //     this._mobilePartyTickDataCache = new Campaign.CampaignTickPartyDataCache();
        //     if (this._playerFormationPreferences == null)
        //         this._playerFormationPreferences = new Dictionary<CharacterObject, FormationClass>();
        //     this.PlayerFormationPreferences =
        //         this._playerFormationPreferences.GetReadOnlyDictionary<CharacterObject, FormationClass>();
        //     if (this.CampaignObjectManager != null)
        //         return;
        //     this.CampaignObjectManager = new CampaignObjectManager();
        //     this.CampaignObjectManager.SetForceCopyListsForSaveCompability();
        // }

        private void InitializeForSavedGame()
        {
            foreach (CampaignEntityComponent component in this._campaignEntitySystem.GetComponents())
                component.OnLoadSavedGame();
            foreach (Settlement settlement in Settlement.All)
                settlement.Party.OnFinishLoadState();
            foreach (MobileParty mobileParty in this.MobileParties.ToList<MobileParty>())
                mobileParty.Party.OnFinishLoadState();
            foreach (Settlement settlement in Settlement.All)
                settlement.OnFinishLoadState();
            if (Game.Current.GameStateManager.ActiveState is MapState activeState)
                activeState.OnLoad();
            this.GameMenuCallbackManager.OnGameLoad();
            this.IssueManager.InitializeForSavedGame();
            this.MinSettlementX = 1000f;
            this.MinSettlementY = 1000f;
            foreach (Settlement settlement in Settlement.All)
            {
                if ((double)settlement.Position2D.x < (double)this.MinSettlementX)
                    this.MinSettlementX = settlement.Position2D.x;
                if ((double)settlement.Position2D.y < (double)this.MinSettlementY)
                    this.MinSettlementY = settlement.Position2D.y;
                if ((double)settlement.Position2D.x > (double)this.MaxSettlementX)
                    this.MaxSettlementX = settlement.Position2D.x;
                if ((double)settlement.Position2D.y > (double)this.MaxSettlementY)
                    this.MaxSettlementY = settlement.Position2D.y;
            }
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            this.ObjectManager.PreAfterLoad();
            this.CampaignObjectManager.PreAfterLoad();
            this.ObjectManager.AfterLoad();
            this.CampaignObjectManager.AfterLoad();
            this.CharacterRelationManager.AfterLoad();
            CampaignEventDispatcher.Instance.OnGameEarlyLoaded(starter);
            CampaignEventDispatcher.Instance.OnGameLoaded(starter);
            this.InitializeForSavedGame();
        }

        private void OnDataLoadFinished(CampaignGameStarter starter)
        {
            this._towns = Settlement.All.Where<Settlement>((Func<Settlement, bool>)(x => x.IsTown))
                .Select<Settlement, Town>((Func<Settlement, Town>)(x => x.Town)).ToList<Town>();
            this._castles = Settlement.All.Where<Settlement>((Func<Settlement, bool>)(x => x.IsCastle))
                .Select<Settlement, Town>((Func<Settlement, Town>)(x => x.Town)).ToList<Town>();
            this._villages = Settlement.All.Where<Settlement>((Func<Settlement, bool>)(x => x.Village != null))
                .Select<Settlement, Village>((Func<Settlement, Village>)(x => x.Village)).ToList<Village>();
            this._hideouts = Settlement.All.Where<Settlement>((Func<Settlement, bool>)(x => x.IsHideout))
                .Select<Settlement, Hideout>((Func<Settlement, Hideout>)(x => x.Hideout)).ToList<Hideout>();
            if (this._campaignPeriodicEventManager == null)
                this._campaignPeriodicEventManager = new CampaignPeriodicEventManager();
            this._campaignPeriodicEventManager.InitializeTickers();
            this.CreateCampaignEvents();
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            CampaignEventDispatcher.Instance.OnSessionStart(starter);
            CampaignEventDispatcher.Instance.OnAfterSessionStart(starter);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener((object)this,
                new Action<Settlement>(this.DailyTickSettlement));
            this.ConversationManager.Build();
            foreach (Settlement settlement in this.Settlements)
                settlement.OnSessionStart();
            this.IsCraftingEnabled = true;
            this.IsBannerEditorEnabled = true;
            this.IsFaceGenEnabled = true;
            this.MapEventManager.OnAfterLoad();
            this.KingdomManager.RegisterEvents();
            this.KingdomManager.OnNewGameCreated();
            this.CampaignInformationManager.RegisterEvents();
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
            foreach (MobileParty mobileParty in this.MobileParties)
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
            if ((int)this.CampaignStartTime.ElapsedDaysUntilNow % 7 != 0)
                return;
            CampaignEventDispatcher.Instance.WeeklyTick();
            this.OnWeeklyTick();
        }

        private void OnWeeklyTick()
        {
            this.LogEntryHistory.DeleteOutdatedLogs();
            if (this.WeeklyTicked == null)
                return;
            this.WeeklyTicked();
        }

        public CampaignTimeControlMode GetSimplifiedTimeControlMode()
        {
            switch (this.TimeControlMode)
            {
                case CampaignTimeControlMode.Stop:
                    return CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.UnstoppablePlay:
                    return CampaignTimeControlMode.UnstoppablePlay;
                case CampaignTimeControlMode.UnstoppableFastForward:
                case CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime:
                    return CampaignTimeControlMode.UnstoppableFastForward;
                case CampaignTimeControlMode.StoppablePlay:
                    return !this.IsMainPartyWaiting
                        ? CampaignTimeControlMode.StoppablePlay
                        : CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.StoppableFastForward:
                    return !this.IsMainPartyWaiting
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
            float speedUpMultiplier = this.SpeedUpMultiplier;
            float num2 = 0.25f * realDt;
            this.IsMainPartyWaiting = MobileParty.MainParty.ComputeIsWaiting();
            switch (this.TimeControlMode)
            {
                case CampaignTimeControlMode.Stop:
                case CampaignTimeControlMode.FastForwardStop:
                    this._dt = num1;
                    this.MapTimeTracker.Tick(4320f * num1);
                    break;
                case CampaignTimeControlMode.UnstoppablePlay:
                    num1 = num2;
                    goto case CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.UnstoppableFastForward:
                case CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime:
                    num1 = num2 * speedUpMultiplier;
                    goto case CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.StoppablePlay:
                    if (!this.IsMainPartyWaiting)
                    {
                        num1 = num2;
                        goto case CampaignTimeControlMode.Stop;
                    }
                    else
                        goto case CampaignTimeControlMode.Stop;
                case CampaignTimeControlMode.StoppableFastForward:
                    if (!this.IsMainPartyWaiting)
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
            this.SaveHandler.QuickSaveCurrentGame();
        }

        public void RealTick(float realDt)
        {
            this.CheckMainPartyNeedsUpdate();
            this.TickMapTime(realDt);
            foreach (CampaignEntityComponent component in this._campaignEntitySystem.GetComponents())
                component.OnTick(realDt, this._dt);
            if (!this.GameStarted)
            {
                this.GameStarted = true;
                int num = 0;
                foreach (SkillObject skill in Skills.All)
                    num += Hero.MainHero.GetSkillValue(skill);
                this.InitialPlayerTotalSkills = num;
                this.SiegeEventManager.Tick(this._dt);
            }

            this._mobilePartyTickDataCache.ValidateMobilePartyTickDataCache(this.MobileParties.Count);
            int index1 = 0;
            foreach (MobileParty mobileParty in this.MobileParties)
            {
                this._mobilePartyTickDataCache.CacheData[index1].mobileParty = mobileParty;
                this._mobilePartyTickDataCache.CacheData[index1].isInArmy = mobileParty.Army != null;
                ++index1;
            }

            for (int index2 = 0; index2 < index1; ++index2)
            {
                MobileParty mobileParty = this._mobilePartyTickDataCache.CacheData[index2].mobileParty;
                mobileParty.TickForMobileParty(ref this._mobilePartyTickDataCache.CacheData[index2].localVariables,
                    this._dt, realDt);
                if (this._mobilePartyTickDataCache.CacheData[index2].isInArmy)
                {
                    this._mobilePartyTickDataCache.CacheData[index2].localVariables.nextPathFaceRecord =
                        Current.MapSceneWrapper.GetFaceIndex(this._mobilePartyTickDataCache.CacheData[index2]
                            .localVariables.nextPosition);
                    mobileParty.TickForMobileParty2(ref this._mobilePartyTickDataCache.CacheData[index2].localVariables,
                        realDt);
                }
            }

            int movedPartyCount = 0;
            for (int index3 = 0; index3 < index1; ++index3)
            {
                MobileParty.TickLocalVariables localVariables =
                    this._mobilePartyTickDataCache.CacheData[index3].localVariables;
                MobileParty mobileParty = this._mobilePartyTickDataCache.CacheData[index3].mobileParty;
                if ((double)localVariables.nextMoveDistance > 0.0 && mobileParty.BesiegedSettlement == null &&
                    (!localVariables.hasMapEvent || localVariables.isArmyMember) && !localVariables.isArmyMember)
                {
                    this._mobilePartyTickDataCache.PositionArray[movedPartyCount * 2] = localVariables.nextPosition.x;
                    this._mobilePartyTickDataCache.PositionArray[movedPartyCount * 2 + 1] =
                        localVariables.nextPosition.y;
                    this._mobilePartyTickDataCache.MovedPartiesIndices[movedPartyCount] = index3;
                    ++movedPartyCount;
                }
            }

            Current.MapSceneWrapper.GetFaceIndexForMultiplePositions(movedPartyCount,
                this._mobilePartyTickDataCache.PositionArray, this._mobilePartyTickDataCache.ResultArray);
            for (int index4 = 0; index4 < movedPartyCount; ++index4)
                this._mobilePartyTickDataCache.CacheData[this._mobilePartyTickDataCache.MovedPartiesIndices[index4]]
                    .localVariables.nextPathFaceRecord = this._mobilePartyTickDataCache.ResultArray[index4];
            for (int index5 = 0; index5 < index1; ++index5)
            {
                MobileParty mobileParty = this._mobilePartyTickDataCache.CacheData[index5].mobileParty;
                if (!this._mobilePartyTickDataCache.CacheData[index5].isInArmy)
                    mobileParty.TickForMobileParty2(ref this._mobilePartyTickDataCache.CacheData[index5].localVariables,
                        realDt);
            }

            foreach (Settlement settlement in Settlement.All)
                settlement.Party.Tick(realDt, this._dt);
            foreach (MobileParty mobileParty in this.MobileParties)
                mobileParty.Party.Tick(realDt, this._dt);
            this.SiegeEventManager.Tick(this._dt);
        }

        public void SetTimeSpeed(int speed)
        {
            switch (speed)
            {
                case 0:
                    if (this.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward ||
                        this.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
                    {
                        this.TimeControlMode = CampaignTimeControlMode.FastForwardStop;
                        break;
                    }

                    if (this.TimeControlMode == CampaignTimeControlMode.FastForwardStop ||
                        this.TimeControlMode == CampaignTimeControlMode.Stop)
                        break;
                    this.TimeControlMode = CampaignTimeControlMode.Stop;
                    break;
                case 1:
                    if ((this.TimeControlMode == CampaignTimeControlMode.Stop ||
                         this.TimeControlMode == CampaignTimeControlMode.FastForwardStop) && this.MainParty.IsHolding ||
                        this.IsMainPartyWaiting || MobileParty.MainParty.Army != null &&
                        MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
                    {
                        this.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
                        break;
                    }

                    this.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
                    break;
                case 2:
                    if ((this.TimeControlMode == CampaignTimeControlMode.Stop ||
                         this.TimeControlMode == CampaignTimeControlMode.FastForwardStop) && this.MainParty.IsHolding ||
                        this.IsMainPartyWaiting || MobileParty.MainParty.Army != null &&
                        MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
                    {
                        this.TimeControlMode = CampaignTimeControlMode.UnstoppableFastForward;
                        break;
                    }

                    this.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
                    break;
            }
        }

        public void Tick()
        {
            ++this._curMapFrame;
            ++this._curSessionFrame;
            if ((double)this._dt > 0.0 || this._curSessionFrame < 3)
            {
                CampaignEventDispatcher.Instance.Tick(this._dt);
                this._campaignPeriodicEventManager.TickPartialHourlyAi();
                this._campaignPeriodicEventManager.OnTick(this._dt);
                this.PartiesThink(this._dt);
                this.MapEventManager.Tick();
                this._lastNonZeroDtFrame = this._curMapFrame;
                this._campaignPeriodicEventManager.MobilePartyHourlyTick();
                this.CampaignInformationManager.Tick();
            }

            if ((double)this._dt > 0.0)
                this._campaignPeriodicEventManager.TickPeriodicEvents();
            Current.PlayerCaptivity.Update(this._dt);
            if ((double)this._dt > 0.0 || MobileParty.MainParty.MapEvent == null &&
                this._curMapFrame == this._lastNonZeroDtFrame + 1)
                EncounterManager.Tick(this._dt);
            if (!(Game.Current.GameStateManager.ActiveState is MapState activeState) || activeState.AtMenu)
                return;
            string genericStateMenu = this.Models.EncounterGameMenuModel.GetGenericStateMenu();
            if (string.IsNullOrEmpty(genericStateMenu))
                return;
            GameMenu.ActivateGameMenu(genericStateMenu);
        }

        private void CreateCampaignEvents()
        {
            long numTicks = (CampaignTime.Now - CampaignData.CampaignStartTime).NumTicks;
            CampaignTime initialWait1 = CampaignTime.Days(1f);
            if (numTicks % 864000000L != 0L)
                initialWait1 = CampaignTime.Days((float)(numTicks % 864000000L) / 8.64E+08f);
            this._dailyTickEvent =
                CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Days(1f), initialWait1);
            this._dailyTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.DailyTick));
            CampaignTime initialWait2 = CampaignTime.Hours(0.5f);
            if (numTicks % 36000000L != 0L)
                initialWait2 = CampaignTime.Hours((float)(numTicks % 36000000L) / 3.6E+07f);
            this._hourlyTickEvent =
                CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(1f), initialWait2);
            this._hourlyTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.HourlyTick));
        }

        private void PartiesThink(float dt)
        {
            foreach (MobileParty mobileParty in this.MobileParties)
                mobileParty.TickAi(dt);
        }

        public TComponent GetEntityComponent<TComponent>() where TComponent : CampaignEntityComponent =>
            this._campaignEntitySystem.GetComponent<TComponent>();

        public TComponent AddEntityComponent<TComponent>() where TComponent : CampaignEntityComponent, new() =>
            this._campaignEntitySystem.AddComponent<TComponent>();

        public T GetCampaignBehavior<T>() => this._campaignBehaviorManager.GetBehavior<T>();

        public IEnumerable<T> GetCampaignBehaviors<T>() => this._campaignBehaviorManager.GetBehaviors<T>();

        public void AddCampaignBehaviorManager(ICampaignBehaviorManager manager) =>
            this._campaignBehaviorManager = manager;

        public void RemoveTracks(Predicate<Track> predicate) =>
            this._mapTracksCampaignBehavior?.RemoveTracks(predicate);

        public void AddMapArrow(
            TextObject pointerName,
            Vec2 trackPosition,
            Vec2 trackDirection,
            float life,
            int numberOfMembers)
        {
            this._mapTracksCampaignBehavior?.AddMapArrow(pointerName, trackPosition, trackDirection, life,
                numberOfMembers);
        }

        public int GeneratePartyId(PartyBase party)
        {
            int lastPartyIndex = this._lastPartyIndex;
            ++this._lastPartyIndex;
            return lastPartyIndex;
        }

        public void AddTrack(MobileParty target, Vec2 trackPosition, Vec2 trackDirection)
        {
            if (this._mapTracksCampaignBehavior.IsTrackDropped(target))
                return;
            this._mapTracksCampaignBehavior.AddTrack(target, trackPosition, trackDirection);
        }

        private void LoadMapScene()
        {
            this._mapSceneWrapper = this.MapSceneCreator.CreateMapScene();
            this._mapSceneWrapper.SetSceneLevels(new List<string>()
            {
                "level_1",
                "level_2",
                "level_3",
                "siege",
                "raid",
                "burned"
            });
            this._mapSceneWrapper.Load();
            Vec2 minimumPosition;
            Vec2 maximumPosition;
            float maximumHeight;
            this._mapSceneWrapper.GetMapBorders(out minimumPosition, out maximumPosition, out maximumHeight);
            MapMinimumPosition = minimumPosition;
            MapMaximumPosition = maximumPosition;
            MapMaximumHeight = maximumHeight;
            MapDiagonal = Campaign.MapMinimumPosition.Distance(Campaign.MapMaximumPosition);
        }

        private void InitializeCachedLists()
        {
            MBObjectManager objectManager = Game.Current.ObjectManager;
            this._characters = objectManager.GetObjectTypeList<CharacterObject>();
            this._workshops = objectManager.GetObjectTypeList<WorkshopType>();
            this._itemModifiers = objectManager.GetObjectTypeList<ItemModifier>();
            this._itemModifierGroups = objectManager.GetObjectTypeList<ItemModifierGroup>();
            this._concepts = objectManager.GetObjectTypeList<Concept>();
            this.TemplateCharacters = this._characters
                .Where<CharacterObject>((Func<CharacterObject, bool>)(x => x.IsTemplate && !x.IsObsolete))
                .ToList<CharacterObject>().GetReadOnlyList<CharacterObject>();
            this.ChildTemplateCharacters = this._characters
                .Where<CharacterObject>((Func<CharacterObject, bool>)(x => x.IsChildTemplate && !x.IsObsolete))
                .ToList<CharacterObject>().GetReadOnlyList<CharacterObject>();
            this._mapTracksCampaignBehavior = this.GetCampaignBehavior<IMapTracksCampaignBehavior>();
        }

        public IEnumerable<MobileParty> GetNearbyMobileParties(
            Vec2 position,
            float radius,
            Func<MobileParty, bool> condition)
        {
            return this.MobilePartyLocator.FindPartiesAroundPosition(position, radius, condition);
        }

        public override void OnDestroy()
        {
            GameTexts.ClearInstance();
            this._mapSceneWrapper?.Destroy();
            ConversationManager.Clear();
            CampaignData.OnGameEnd();
            MBTextManager.ClearAll();
            CampaignSiegeTestStatic.Destruct();
            MBSaveLoad.OnGameDestroy();
            Current = null;
        }

        public void InitializeSinglePlayerReferences()
        {
            this.IsInitializedSinglePlayerReferences = true;
            this.InitializeGamePlayReferences();
        }

        private void CreateLists()
        {
            this.AllPerks = MBObjectManager.Instance.GetObjectTypeList<PerkObject>();
            this.AllTraits = MBObjectManager.Instance.GetObjectTypeList<TraitObject>();
            this.AllPolicies = MBObjectManager.Instance.GetObjectTypeList<PolicyObject>();
            this.AllBuildingTypes = MBObjectManager.Instance.GetObjectTypeList<BuildingType>();
            this.AllIssueEffects = MBObjectManager.Instance.GetObjectTypeList<IssueEffect>();
            this.AllSiegeStrategies = MBObjectManager.Instance.GetObjectTypeList<SiegeStrategy>();
            this.AllVillageTypes = MBObjectManager.Instance.GetObjectTypeList<VillageType>();
            this.AllSkillEffects = MBObjectManager.Instance.GetObjectTypeList<SkillEffect>();
            this.AllFeats = MBObjectManager.Instance.GetObjectTypeList<FeatObject>();
            this.AllSkills = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
            this.AllSiegeEngineTypes = MBObjectManager.Instance.GetObjectTypeList<SiegeEngineType>();
            this.AllItemCategories = MBObjectManager.Instance.GetObjectTypeList<ItemCategory>();
            this.AllCharacterAttributes = MBObjectManager.Instance.GetObjectTypeList<CharacterAttribute>();
            this.AllItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
        }

        private void CalculateAverageDistanceBetweenTowns()
        {
            if (this.GameMode == CampaignGameMode.Tutorial)
                return;
            float num1 = 0.0f;
            int num2 = 0;
            foreach (Town allTown1 in (IEnumerable<Town>)this.AllTowns)
            {
                float num3 = 2.5E+07f;
                foreach (Town allTown2 in (IEnumerable<Town>)this.AllTowns)
                {
                    if (allTown1 != allTown2)
                    {
                        float num4 = allTown1.Settlement.Position2D.DistanceSquared(allTown2.Settlement.Position2D);
                        if ((double)num4 < (double)num3)
                            num3 = num4;
                    }
                }

                num1 += (float)Math.Sqrt((double)num3);
                ++num2;
            }

            AverageDistanceBetweenTwoTowns = num1 / (float)num2;
        }

        public void InitializeGamePlayReferences()
        {
            this.CurrentGame.PlayerTroop =
                (BasicCharacterObject)this.CurrentGame.ObjectManager.GetObject<CharacterObject>("main_hero");
            if (Hero.MainHero.Mother != null)
                Hero.MainHero.Mother.HasMet = true;
            if (Hero.MainHero.Father != null)
                Hero.MainHero.Father.HasMet = true;
            this.PlayerDefaultFaction = this.CampaignObjectManager.Find<Clan>("player_faction");
            Hero.MainHero.Detected = true;
            GiveGoldAction.ApplyBetweenCharacters((Hero)null, Hero.MainHero, 1000, true);
        }

        private void InitializeScenes()
        {
            GameSceneDataManager.Instance.LoadSPBattleScenes(ModuleHelper.GetModuleFullPath("Sandbox") +
                                                             "ModuleData/sp_battle_scenes.xml");
            GameSceneDataManager.Instance.LoadConversationScenes(ModuleHelper.GetModuleFullPath("Sandbox") +
                                                                 "ModuleData/conversation_scenes.xml");
            GameSceneDataManager.Instance.LoadMeetingScenes(ModuleHelper.GetModuleFullPath("Sandbox") +
                                                            "ModuleData/meeting_scenes.xml");
        }

        public void SetLoadingParameters(GameLoadingType gameLoadingType, int randomSeed)
        {
            Current = this;
            this._gameLoadingType = gameLoadingType;
            if (gameLoadingType == GameLoadingType.SavedCampaign)
                return;
            this.CurrentGame.SetRandomSeed(randomSeed);
        }

        public void SetLoadingParameters(GameLoadingType gameLoadingType)
        {
            int randomSeed = (int)DateTime.Now.Ticks & (int)ushort.MaxValue;
            this.SetLoadingParameters(gameLoadingType, randomSeed);
        }

        public void AddCampaignEventReceiver(CampaignEventReceiver receiver) =>
            this.CampaignEventDispatcher.AddCampaignEventReceiver(receiver);

        protected override void OnInitialize()
        {
            this.CampaignEvents = new CampaignEvents();
            this.PeriodicCampaignEvents = new List<MBCampaignEvent>();
            this.CampaignEventDispatcher = new CampaignEventDispatcher(
                (IEnumerable<CampaignEventReceiver>)new CampaignEventReceiver[3]
                {
                    (CampaignEventReceiver)this.CampaignEvents,
                    (CampaignEventReceiver)this.IssueManager,
                    (CampaignEventReceiver)this.QuestManager
                });
            this.SandBoxManager = Game.Current.AddGameHandler<SandBoxManager>();
            this.SaveHandler = new SaveHandler();
            this.VisualCreator = new VisualCreator();
            this.GameMenuManager = new GameMenuManager();
            if (this._gameLoadingType != GameLoadingType.Editor)
                this.CreateManagers();
            CampaignGameStarter campaignGameStarter = new CampaignGameStarter(this.GameMenuManager,
                this.ConversationManager, this.CurrentGame.GameTextManager);
            this.SandBoxManager.Initialize(campaignGameStarter);
            this.GameManager.InitializeGameStarter(this.CurrentGame, (IGameStarter)campaignGameStarter);
            this.CurrentGame.SetRandomGenerators();
            if (this._gameLoadingType == GameLoadingType.NewCampaign ||
                this._gameLoadingType == GameLoadingType.SavedCampaign)
                this.InitializeScenes();
            this.GameManager.OnGameStart(this.CurrentGame, (IGameStarter)campaignGameStarter);
            this.CurrentGame.SetBasicModels(campaignGameStarter.Models);
            this._gameModels = this.CurrentGame.AddGameModelsManager<GameModels>(campaignGameStarter.Models);
            this.CurrentGame.CreateGameManager();
            if (this._gameLoadingType == GameLoadingType.SavedCampaign)
            {
                this.CurrentGame.InitializeOnCampaignStart();
                this.InitializeDefaultCampaignObjects();
            }

            this.GameManager.BeginGameStart(this.CurrentGame);
            if (this._gameLoadingType != GameLoadingType.SavedCampaign)
                this.OnNewCampaignStart();
            this.CreateLists();
            this.InitializeBasicObjectXmls();
            if (this._gameLoadingType != GameLoadingType.SavedCampaign)
                this.GameManager.OnNewCampaignStart(this.CurrentGame, (object)campaignGameStarter);
            if (this._gameLoadingType == GameLoadingType.SavedCampaign)
                this.CampaignObjectManager.InitializeForOldSaves();
            this.SandBoxManager.OnCampaignStart(campaignGameStarter, this.GameManager,
                this._gameLoadingType == GameLoadingType.SavedCampaign);
            if (this._gameLoadingType != GameLoadingType.SavedCampaign)
            {
                this.AddCampaignBehaviorManager(
                    (ICampaignBehaviorManager)
                    new TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.CampaignBehaviorManager(
                        (IEnumerable<CampaignBehaviorBase>)campaignGameStarter.CampaignBehaviors));
                this.GameManager.OnAfterCampaignStart(this.CurrentGame);
            }
            else
            {
                this.SandBoxManager.OnGameLoaded((object)campaignGameStarter);
                this.GameManager.OnGameLoaded(this.CurrentGame, (object)campaignGameStarter);
                this._campaignBehaviorManager.InitializeCampaignBehaviors(
                    (IEnumerable<CampaignBehaviorBase>)campaignGameStarter.CampaignBehaviors);
                this._campaignBehaviorManager.RegisterEvents();
                this._campaignBehaviorManager.OnGameLoaded();
            }

            Current.GetCampaignBehavior<ICraftingCampaignBehavior>()?.InitializeCraftingElements();
            campaignGameStarter.UnregisterNonReadyObjects();
            if (this._gameLoadingType == GameLoadingType.SavedCampaign)
                this.InitializeCampaignObjectsOnAfterLoad();
            else if (this._gameLoadingType == GameLoadingType.NewCampaign ||
                     this._gameLoadingType == GameLoadingType.Tutorial)
                this.CampaignObjectManager.InitializeOnNewGame();
            this.InitializeCachedLists();
            this.NameGenerator.Initialize();
            this.CurrentGame.OnGameStart();
            this.GameManager.OnGameInitializationFinished(this.CurrentGame);
        }

        private void CalculateCachedStatsOnLoad() => ItemRoster.CalculateCachedStatsOnLoad();

        private void InitializeBasicObjectXmls()
        {
            this.ObjectManager.LoadXML("SPCultures");
            this.ObjectManager.LoadXML("Concepts");
        }

        private void InitializeDefaultCampaignObjects()
        {
            BOFCampaign campaign = this;
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
            Campaign campaign = this;
            campaign.KingdomManager = new KingdomManager();
            campaign.CampaignInformationManager = new CampaignInformationManager();
            campaign.VisualTrackerManager = new VisualTrackerManager();
            campaign.TournamentManager =
                (ITournamentManager)new TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames.TournamentManager();
        }

        private void InitializeCampaignObjectsOnAfterLoad()
        {
            this.CampaignObjectManager.InitializeOnLoad();
            this.FactionManager.AfterLoad();
            this.AllPerks = new MBReadOnlyList<PerkObject>(this.AllPerks
                .Where<PerkObject>((Func<PerkObject, bool>)(x => !x.IsTrash)).ToList<PerkObject>());
            this.LogEntryHistory.OnAfterLoad();
            foreach (Kingdom kingdom in this.Kingdoms)
            {
                foreach (Army army in kingdom.Armies)
                    army.OnAfterLoad();
            }
        }

        private void OnNewCampaignStart()
        {
            Game.Current.PlayerTroop = (BasicCharacterObject)null;
            this.MapStateData = new MapStateData();
            this.CurrentGame.InitializeOnCampaignStart();
            this.InitializeDefaultCampaignObjects();
            this.MainParty = MBObjectManager.Instance.CreateObject<MobileParty>("player_party");
            this.MainParty.SetAsMainParty();
            this.InitializeManagers();
        }

        protected override void BeforeRegisterTypes(MBObjectManager objectManager) =>
            objectManager.RegisterNonSerializedType<FeatObject>("Feat", "Feats", 0U);

        protected override void OnRegisterTypes(MBObjectManager objectManager)
        {
            objectManager.RegisterType<MobileParty>("MobileParty", "MobileParties", 14U, isTemporary: true);
            objectManager.RegisterType<CharacterObject>("NPCCharacter", "NPCCharacters", 16U);
            if (this.GameMode == CampaignGameMode.Tutorial)
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
            objectManager.RegisterNonSerializedType<LocationComplexTemplate>("LocationComplexTemplate",
                "LocationComplexTemplates", 42U);
        }

        private void CreateManagers()
        {
            this._encyclopediaManager = new EncyclopediaManager();
            this._inventoryManager = new InventoryManager();
            this._partyScreenManager = new PartyScreenManager();
            this._conversationManager = new ConversationManager();
            this.NameGenerator = new NameGenerator();
        }

        private void OnNewGameCreated(CampaignGameStarter gameStarter)
        {
            this.OnNewGameCreatedInternal();
            this.SandBoxManager.OnNewGameCreated((object)gameStarter);
            this.GameManager?.OnNewGameCreated(this.CurrentGame, (object)gameStarter);
            CampaignEventDispatcher.Instance.OnNewGameCreated(gameStarter);
            this.OnAfterNewGameCreatedInternal();
        }

        private void OnNewGameCreatedInternal()
        {
            this.CheatFindItemRangeBegin = 0;
            this.UniqueGameId = MiscHelper.GenerateCampaignId(12);
            this.PlayerTraitDeveloper = new HeroTraitDeveloper(Hero.MainHero);
            this.TimeControlMode = CampaignTimeControlMode.Stop;
            this._campaignEntitySystem = new EntitySystem<CampaignEntityComponent>();
            this.SiegeEventManager = new SiegeEventManager();
            this.MapEventManager = new MapEventManager(this.CurrentGame);
            this.autoEnterTown = (PartyBase)null;
            this.MinSettlementX = 1000f;
            this.MinSettlementY = 1000f;
            foreach (Settlement settlement in Settlement.All)
            {
                if ((double)settlement.Position2D.x < (double)this.MinSettlementX)
                    this.MinSettlementX = settlement.Position2D.x;
                if ((double)settlement.Position2D.y < (double)this.MinSettlementY)
                    this.MinSettlementY = settlement.Position2D.y;
                if ((double)settlement.Position2D.x > (double)this.MaxSettlementX)
                    this.MaxSettlementX = settlement.Position2D.x;
                if ((double)settlement.Position2D.y > (double)this.MaxSettlementY)
                    this.MaxSettlementY = settlement.Position2D.y;
            }

            this.CampaignBehaviorManager.RegisterEvents();
            this.CameraFollowParty = this.MainParty.Party;
        }

        private void OnAfterNewGameCreatedInternal()
        {
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, (Hero)null, Hero.MainHero.Gold, true);
            GiveGoldAction.ApplyBetweenCharacters((Hero)null, Hero.MainHero, 1000, true);
            Hero.MainHero.Clan.Influence = 0.0f;
            Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
            this.GameInitTick();
            this._playerFormationPreferences = new Dictionary<CharacterObject, FormationClass>();
            this.PlayerFormationPreferences =
                this._playerFormationPreferences.GetReadOnlyDictionary<CharacterObject, FormationClass>();
            BOFCampaign.Current.DesertionEnabled = true;
        }

        protected override void DoLoadingForGameType(
            GameTypeLoadingStates gameTypeLoadingState,
            out GameTypeLoadingStates nextState)
        {
            nextState = GameTypeLoadingStates.None;
            switch (gameTypeLoadingState)
            {
                case GameTypeLoadingStates.InitializeFirstStep:
                    this.CurrentGame.Initialize();
                    nextState = GameTypeLoadingStates.WaitSecondStep;
                    break;
                case GameTypeLoadingStates.WaitSecondStep:
                    nextState = GameTypeLoadingStates.LoadVisualsThirdState;
                    break;
                case GameTypeLoadingStates.LoadVisualsThirdState:
                    if (this.GameMode == CampaignGameMode.Campaign)
                        this.LoadMapScene();
                    nextState = GameTypeLoadingStates.PostInitializeFourthState;
                    break;
                case GameTypeLoadingStates.PostInitializeFourthState:
                    CampaignGameStarter gameStarter = this.SandBoxManager.GameStarter;
                    if (this._gameLoadingType == BOFCampaign.GameLoadingType.SavedCampaign)
                    {
                        this.OnDataLoadFinished(gameStarter);
                        this.CalculateAverageDistanceBetweenTowns();
                        this.DetermineModules();
                        this.MapEventManager.OnGameInitialized();
                        foreach (Settlement settlement in Settlement.All)
                            settlement.Party.OnGameInitialized();
                        foreach (MobileParty mobileParty in this.MobileParties.ToList<MobileParty>())
                            mobileParty.Party.OnGameInitialized();
                        this.CalculateCachedStatsOnLoad();
                        this.OnGameLoaded(gameStarter);
                        this.OnSessionStart(gameStarter);
                        foreach (Hero allAliveHero in Hero.AllAliveHeroes)
                            allAliveHero.CheckInvalidEquipmentsAndReplaceIfNeeded();
                        foreach (Hero deadOrDisabledHero in Hero.DeadOrDisabledHeroes)
                            deadOrDisabledHero.CheckInvalidEquipmentsAndReplaceIfNeeded();
                    }
                    else if (this._gameLoadingType == BOFCampaign.GameLoadingType.NewCampaign)
                    {
                        this.OnDataLoadFinished(gameStarter);
                        this.CalculateAverageDistanceBetweenTowns();
                        MBSaveLoad.OnNewGame();
                        this.InitializeMainParty();
                        this.DetermineModules();
                        foreach (Settlement settlement in Settlement.All)
                            settlement.Party.OnGameInitialized();
                        foreach (MobileParty mobileParty in this.MobileParties.ToList<MobileParty>())
                            mobileParty.Party.OnGameInitialized();
                        foreach (Settlement settlement in Settlement.All)
                            settlement.OnGameCreated();
                        foreach (Clan clan in Clan.All)
                            clan.OnGameCreated();
                        MBObjectManager.Instance.RemoveTemporaryTypes();
                        this.OnNewGameCreated(gameStarter);
                        this.OnSessionStart(gameStarter);
                        Debug.Print("Finished starting a new game.");
                    }

                    this.GameManager.OnAfterGameInitializationFinished(this.CurrentGame, (object)gameStarter);
                    break;
            }
        }

        private void DetermineModules()
        {
            if (this._previouslyUsedModules == null)
                this._previouslyUsedModules = new List<string>();
            foreach (string moduleName in SandBoxManager.Instance.ModuleManager.ModuleNames)
            {
                if (!this._previouslyUsedModules.Contains(moduleName))
                    this._previouslyUsedModules.Add(moduleName);
            }
        }

        public override void OnMissionIsStarting(string missionName, MissionInitializerRecord rec)
        {
            if (!rec.PlayingInCampaignMode)
                return;
            CampaignEventDispatcher.Instance.BeforeMissionOpened();
        }

        public override void InitializeParameters() =>
            ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_campaign_parameters"));

        public void SetTimeControlModeLock(bool isLocked) => this.TimeControlModeLock = isLocked;

        public void OnPlayerCharacterChanged()
        {
            this.MainParty = Hero.MainHero.PartyBelongedTo;
            if (Hero.MainHero.CurrentSettlement != null && !Hero.MainHero.IsPrisoner)
            {
                if (this.MainParty == null)
                    LeaveSettlementAction.ApplyForCharacterOnly(Hero.MainHero);
                else
                    LeaveSettlementAction.ApplyForParty(this.MainParty);
            }

            if (Hero.MainHero.IsFugitive)
                Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
            this.PlayerTraitDeveloper = new HeroTraitDeveloper(Hero.MainHero);
            if (this.MainParty == null)
            {
                this.MainParty = MobileParty.CreateParty("player_party_" + Hero.MainHero.StringId);
                this.MainParty.ActualClan = Clan.PlayerClan;
                if (Hero.MainHero.IsPrisoner)
                {
                    this.MainParty.InitializeMobileParty(
                        this.CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"),
                        Hero.MainHero.GetPosition().AsVec2, 0.0f, troopNumberLimit: 0);
                    this.MainParty.IsActive = false;
                }
                else
                {
                    Vec3 position1 = Hero.MainHero.GetPosition();
                    Vec2 vec2;
                    if (!(position1.AsVec2 != Vec2.Zero))
                    {
                        vec2 = SettlementHelper.FindRandomSettlement((Func<Settlement, bool>)(s =>
                            s.OwnerClan != null && !s.OwnerClan.IsAtWarWith((IFaction)Clan.PlayerClan))).GetPosition2D;
                    }
                    else
                    {
                        position1 = Hero.MainHero.GetPosition();
                        vec2 = position1.AsVec2;
                    }

                    Vec2 position2 = vec2;
                    this.MainParty.InitializeMobileParty(
                        this.CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"),
                        position2, 0.0f, troopNumberLimit: 0);
                    this.MainParty.IsActive = true;
                    this.MainParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1, true);
                }
            }
            else
                Current.MainParty.IsVisible = true;

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
            this.MainParty.PaymentLimit = Current.Models.PartyWageModel.MaxWage;
        }

        public void SetPlayerFormationPreference(CharacterObject character, FormationClass formation)
        {
            if (!this._playerFormationPreferences.ContainsKey(character))
                this._playerFormationPreferences.Add(character, formation);
            else
                this._playerFormationPreferences[character] = formation;
        }

        public override void OnStateChanged(GameState oldState)
        {
        }

        protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
        {
            base.AutoGeneratedInstanceCollectObjects(collectedObjects);
            collectedObjects.Add((object)this.Options);
            collectedObjects.Add((object)this.TournamentManager);
            collectedObjects.Add((object)this.autoEnterTown);
            collectedObjects.Add((object)this.GameLogs);
            collectedObjects.Add((object)this.KingdomManager);
            collectedObjects.Add((object)this._campaignEntitySystem);
            collectedObjects.Add((object)this._campaignPeriodicEventManager);
            collectedObjects.Add((object)this._previouslyUsedModules);
            collectedObjects.Add((object)this._playerFormationPreferences);
            collectedObjects.Add((object)this._campaignBehaviorManager);
            collectedObjects.Add((object)this._cameraFollowParty);
            collectedObjects.Add((object)this._logEntryHistory);
            collectedObjects.Add((object)this.CampaignObjectManager);
            collectedObjects.Add((object)this.QuestManager);
            collectedObjects.Add((object)this.IssueManager);
            collectedObjects.Add((object)this.FactionManager);
            collectedObjects.Add((object)this.CharacterRelationManager);
            collectedObjects.Add((object)this.Romance);
            collectedObjects.Add((object)this.PlayerCaptivity);
            collectedObjects.Add((object)this.PlayerDefaultFaction);
            collectedObjects.Add((object)this.AdjustedRandom);
            collectedObjects.Add((object)this.GameMenuCallbackManager);
            collectedObjects.Add((object)this.MapStateData);
            collectedObjects.Add((object)this.MapTimeTracker);
            CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime((object)this.CampaignStartTime,
                collectedObjects);
            collectedObjects.Add((object)this.SiegeEventManager);
            collectedObjects.Add((object)this.MapEventManager);
            collectedObjects.Add((object)this.PlayerEncounter);
            collectedObjects.Add((object)this.BarterManager);
            collectedObjects.Add((object)this.MainParty);
            collectedObjects.Add((object)this.CampaignInformationManager);
            collectedObjects.Add((object)this.VisualTrackerManager);
            collectedObjects.Add((object)this.PlayerTraitDeveloper);
        }

        private struct PartyTickCachePerParty
        {
            public MobileParty mobileParty;
            public MobileParty.TickLocalVariables localVariables;
            public bool isInArmy;
        }

        private class CampaignTickPartyDataCache
        {
            public CampaignTickPartyDataCache() => this.CurrentCapacity = 0;

            public PartyTickCachePerParty[] CacheData { get; private set; }

            public PathFaceRecord[] ResultArray { get; private set; }

            public float[] PositionArray { get; private set; }

            public int[] MovedPartiesIndices { get; private set; }

            public int CurrentCapacity { get; private set; }

            public void ValidateMobilePartyTickDataCache(int requestedCapacity)
            {
                if (this.CurrentCapacity >= requestedCapacity)
                    return;
                int length = (int)((double)requestedCapacity * 1.10000002384186);
                this.CacheData = new PartyTickCachePerParty[length];
                this.ResultArray = new PathFaceRecord[length];
                this.PositionArray = new float[length * 2];
                this.MovedPartiesIndices = new int[length];
                this.CurrentCapacity = length;
            }
        }
    }
}