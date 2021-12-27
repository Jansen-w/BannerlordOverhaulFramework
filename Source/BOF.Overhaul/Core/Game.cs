using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem.Core
{
    // [SaveableRootClass(5000)]
    public class Game : IGameStateManagerOwner
    {
        private Game.State _currentState;
        private EntitySystem<GameHandler> _gameEntitySystem;
        private Monster _humanMonster;
        private Monster _horseMonster;
        private BasicGameModels _basicModels;
        private Dictionary<Type, GameModelsManager> _gameModelManagers;
        private static Game _current;

        private IBannerVisualCreator _bannerVisualCreator;

        //[SaveableField(10)]
        private int _randomSeed;

        //[SaveableField(11)]
        private int _nextUniqueTroopSeed = 1;
        private IReadOnlyDictionary<string, Equipment> _defaultEquipments;
        public Action<float> AfterTick;

        public Game.State CurrentState
        {
            get => this._currentState;
            private set => this._currentState = value;
        }

        public IMonsterMissionDataCreator MonsterMissionDataCreator { get; set; }

        public Monster HumanMonster =>
            this._humanMonster ?? (this._humanMonster = this.ObjectManager.GetObject<Monster>("human"));

        public Monster HorseMonster =>
            this._horseMonster ?? (this._horseMonster = this.ObjectManager.GetObject<Monster>("horse"));

        //[SaveableProperty(3)]
        public GameType GameType { get; private set; }

        //[SaveableProperty(4)]
        public DefaultSiegeEngineTypes DefaultSiegeEngineTypes { get; private set; }

        //[SaveableProperty(6)]
        public ObsoleteObjectManager ObsoleteObjectManager { get; private set; }

        public MBObjectManager ObjectManager { get; private set; }

        //[SaveableProperty(7)]
        public BasicCharacterObject LastFaceEditedCharacter { get; set; }

        //[SaveableProperty(8)]
        public Core.BasicCharacterObject PlayerTroop { get; set; }

        public MBFastRandom RandomGenerator { get; private set; }

        public BasicGameModels BasicModels => this._basicModels;

        public T AddGameModelsManager<T>(IEnumerable<GameModel> inputComponents) where T : GameModelsManager
        {
            T instance = (T)Activator.CreateInstance(typeof(T), (object)inputComponents);
            this._gameModelManagers.Add(typeof(T), (GameModelsManager)instance);
            return instance;
        }

        public GameManagerBase GameManager { get; private set; }

        public GameTextManager GameTextManager { get; private set; }

        public GameStateManager GameStateManager { get; private set; }

        public MBFastRandom DeterministicRandomGenerator { get; private set; }

        public bool CheatMode => this.GameManager.CheatMode;

        public bool IsDevelopmentMode => this.GameManager.IsDevelopmentMode;

        public bool IsEditModeOn => this.GameManager.IsEditModeOn;

        public bool DeterministicMode => this.GameManager.DeterministicMode;

        public float ApplicationTime => this.GameManager.ApplicationTime;

        public static Game Current
        {
            get => Game._current;
            set => Game._current = value;
        }

        public IBannerVisualCreator BannerVisualCreator
        {
            get => this._bannerVisualCreator;
            set => this._bannerVisualCreator = value;
        }

        public IBannerVisual CreateBannerVisual(Banner banner) => this.BannerVisualCreator == null
            ? (IBannerVisual)null
            : this.BannerVisualCreator.CreateBannerVisual(banner);

        public int NextUniqueTroopSeed => this._nextUniqueTroopSeed++;

        public DefaultCharacterAttributes DefaultCharacterAttributes { get; private set; }

        public DefaultSkills DefaultSkills { get; private set; }

        public DefaultItemCategories DefaultItemCategories { get; private set; }

        public DefaultItems DefaultItems { get; private set; }

        public EventManager EventManager { get; private set; }

        public Equipment GetDefaultEquipmentWithName(string equipmentName)
        {
            if (this._defaultEquipments.ContainsKey(equipmentName))
                return this._defaultEquipments[equipmentName].Clone();
            Debug.FailedAssert("Equipment with name \"" + equipmentName + "\" could not be found.",
                "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Game.cs", nameof(GetDefaultEquipmentWithName),
                150);
            return (Equipment)null;
        }

        public void SetDefaultEquipments(
            IReadOnlyDictionary<string, Equipment> defaultEquipments)
        {
            if (this._defaultEquipments != null)
                return;
            this._defaultEquipments = defaultEquipments;
        }

        private Game(GameType gameType, GameManagerBase gameManager, MBObjectManager objectManager)
        {
            this.GameType = gameType;
            Game.Current = this;
            this.GameType.CurrentGame = this;
            this.GameManager = gameManager;
            this.GameManager.Game = this;
            this.EventManager = new EventManager();
            this.ObjectManager = objectManager;
            this.InitializeParameters();
        }

        public static Game CreateGame(GameType gameType, GameManagerBase gameManager)
        {
            MBObjectManager objectManager = MBObjectManager.Init();
            Game.RegisterTypes(gameType, objectManager);
            return new Game(gameType, gameManager, objectManager);
        }

        // public static Game LoadSaveGame(LoadResult loadResult, GameManagerBase gameManager)
        // {
        //   MBSaveLoad.OnStartGame(loadResult);
        //   MBObjectManager objectManager = MBObjectManager.Init();
        //   Game root = (Game) loadResult.Root;
        //   Game.RegisterTypes(root.GameType, objectManager);
        //   ObsoleteObjectManager.Instance = root.ObsoleteObjectManager;
        //   loadResult.InitializeObjects();
        //   MBObjectManager.Instance.ReInitialize();
        //   GC.Collect();
        //   root.ObjectManager = objectManager;
        //   root.ObsoleteObjectManager = (ObsoleteObjectManager) null;
        //   ObsoleteObjectManager.Instance = (ObsoleteObjectManager) null;
        //   root.BeginLoading(gameManager);
        //   return root;
        // }

        private void BeginLoading(GameManagerBase gameManager)
        {
            Game.Current = this;
            this.GameType.CurrentGame = this;
            this.GameManager = gameManager;
            this.GameManager.Game = this;
            this.EventManager = new EventManager();
            this.InitializeParameters();
        }

        // private SaveResult SaveAux(MetaData metaData, string saveName, ISaveDriver driver)
        // {
        //   foreach (GameHandler component in (IEnumerable<GameHandler>) this._gameEntitySystem.Components)
        //     component.OnBeforeSave();
        //   SaveOutput saveOutput = SaveManager.Save((object) this, metaData, saveName, driver);
        //   saveOutput.PrintStatus();
        //   foreach (GameHandler component in (IEnumerable<GameHandler>) this._gameEntitySystem.Components)
        //     component.OnAfterSave();
        //   return saveOutput.Result;
        // }

        // public SaveResult Save(MetaData metaData, string saveName, ISaveDriver driver)
        // {
        //   int num = (int) this.SaveAux(metaData, saveName, driver);
        //   Common.MemoryCleanupGC();
        //   return (SaveResult) num;
        // }

        private void InitializeParameters()
        {
            ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_core_parameters"));
            this.GameType.InitializeParameters();
        }

        void IGameStateManagerOwner.OnStateStackEmpty() => this.Destroy();

        public void Destroy()
        {
            this.CurrentState = Game.State.Destroying;
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnGameEnd();
            this.GameManager.OnGameEnd(this);
            this.GameType.OnDestroy();
            this.ObjectManager.Destroy();
            this.EventManager.Clear();
            this.EventManager = (EventManager)null;
            GameStateManager.Current = (GameStateManager)null;
            this.GameStateManager = (GameStateManager)null;
            Game.Current = (Game)null;
            this.CurrentState = Game.State.Destroyed;
            Common.MemoryCleanupGC();
        }

        public void CreateGameManager() => this.GameStateManager =
            new GameStateManager((IGameStateManagerOwner)this, GameStateManager.GameStateManagerType.Game);

        public void SetRandomSeed(int randomSeed) => this._randomSeed = this.DeterministicMode ? 45153 : randomSeed;

        public void OnStateChanged(GameState oldState) => this.GameType.OnStateChanged(oldState);

        public T AddGameHandler<T>() where T : GameHandler, new() => this._gameEntitySystem.AddComponent<T>();

        public T GetGameHandler<T>() where T : GameHandler => this._gameEntitySystem.GetComponent<T>();

        public void RemoveGameHandler<T>() where T : GameHandler => this._gameEntitySystem.RemoveComponent<T>();

        public void SetRandomGenerators()
        {
            this.RandomGenerator = new MBFastRandom(this._randomSeed);
            this.DeterministicRandomGenerator = new MBFastRandom(10000);
        }

        public void Initialize()
        {
            if (this._gameEntitySystem == null)
                this._gameEntitySystem = new EntitySystem<GameHandler>();
            this.GameTextManager = new GameTextManager();
            this._gameModelManagers = new Dictionary<Type, GameModelsManager>();
            GameTexts.Initialize(this.GameTextManager);
            this.GameType.OnInitialize();
        }

        public static void RegisterTypes(GameType gameType, MBObjectManager objectManager)
        {
            gameType?.BeforeRegisterTypes(objectManager);
            objectManager.RegisterNonSerializedType<Monster>("Monster", "Monsters", 2U);
            objectManager.RegisterNonSerializedType<SkeletonScale>("Scale", "Scales", 3U);
            objectManager.RegisterType<ItemObject>("Item", "Items", 4U);
            objectManager.RegisterType<ItemModifier>("ItemModifier", "ItemModifiers", 6U);
            objectManager.RegisterType<ItemModifierGroup>("ItemModifierGroup", "ItemModifierGroups", 7U);
            objectManager.RegisterType<CharacterAttribute>("CharacterAttribute", "CharacterAttributes", 8U);
            objectManager.RegisterType<SkillObject>("Skill", "Skills", 9U);
            objectManager.RegisterType<ItemCategory>("ItemCategory", "ItemCategories", 10U);
            objectManager.RegisterType<CraftingPiece>("CraftingPiece", "CraftingPieces", 11U);
            objectManager.RegisterType<CraftingTemplate>("CraftingTemplate", "CraftingTemplates", 12U);
            objectManager.RegisterType<SiegeEngineType>("SiegeEngineType", "SiegeEngineTypes", 13U);
            objectManager.RegisterType<WeaponDescription>("WeaponDescription", "WeaponDescriptions", 14U);
            objectManager.RegisterType<MBBodyProperty>("BodyProperty", "BodyProperties", 50U);
            objectManager.RegisterNonSerializedType<MBEquipmentRoster>("EquipmentRoster", "EquipmentRosters", 51U);
            objectManager.RegisterNonSerializedType<MBCharacterSkills>("SkillSet", "SkillSets", 52U);
            gameType?.OnRegisterTypes(objectManager);
        }

        public void SetBasicModels(IEnumerable<GameModel> models) =>
            this._basicModels = this.AddGameModelsManager<BasicGameModels>(models);

        public void OnTick(float dt)
        {
            if (GameStateManager.Current == this.GameStateManager)
            {
                this.GameStateManager.OnTick(dt);
                if (this._gameEntitySystem != null)
                {
                    foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                    {
                        try
                        {
                            component.OnTick(dt);
                        }
                        catch (Exception ex)
                        {
                            Debug.Print("Exception on gameHandler tick: " + (object)ex);
                        }
                    }
                }
            }

            Action<float> afterTick = this.AfterTick;
            if (afterTick == null)
                return;
            afterTick(dt);
        }

        public void OnGameNetworkBegin()
        {
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnGameNetworkBegin();
        }

        public void OnGameNetworkEnd()
        {
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnGameNetworkEnd();
        }

        public void OnEarlyPlayerConnect(VirtualPlayer peer)
        {
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnEarlyPlayerConnect(peer);
        }

        public void OnPlayerConnect(VirtualPlayer peer)
        {
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnPlayerConnect(peer);
        }

        public void OnPlayerDisconnect(VirtualPlayer peer)
        {
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnPlayerDisconnect(peer);
        }

        public void OnGameStart()
        {
            foreach (GameHandler component in (IEnumerable<GameHandler>)this._gameEntitySystem.Components)
                component.OnGameStart();
        }

        public bool DoLoading() => this.GameType.DoLoadingForGameType();

        public void ResetRandomGenerator(int randomSeed)
        {
            this._randomSeed = randomSeed;
            this.RandomGenerator = new MBFastRandom(this._randomSeed);
        }

        public void OnMissionIsStarting(string missionName, MissionInitializerRecord rec) =>
            this.GameType.OnMissionIsStarting(missionName, rec);

        public void OnFinalize()
        {
            this.CurrentState = Game.State.Destroying;
            GameStateManager.Current.CleanStates();
        }

        public void InitializeDefaultGameObjects()
        {
            this.DefaultCharacterAttributes = new DefaultCharacterAttributes();
            this.DefaultSkills = new DefaultSkills();
            this.DefaultItemCategories = new DefaultItemCategories();
            this.DefaultSiegeEngineTypes = new DefaultSiegeEngineTypes();
        }

        public void LoadBasicFiles()
        {
            this.ObjectManager.LoadXML("Monsters");
            this.ObjectManager.LoadXML("SkeletonScales");
            this.ObjectManager.LoadXML("ItemModifiers");
            this.ObjectManager.LoadXML("ItemModifierGroups");
            this.ObjectManager.LoadXML("CraftingPieces");
            this.ObjectManager.LoadXML("WeaponDescriptions");
            this.ObjectManager.LoadXML("CraftingTemplates");
            this.ObjectManager.LoadXML("BodyProperties");
            this.ObjectManager.LoadXML("SkillSets");
        }

        public void InitializeOnCampaignStart()
        {
            this.InitializeDefaultGameObjects();
            this.DefaultItems = new DefaultItems();
            this.LoadBasicFiles();
            this.ObjectManager.LoadXML("Items");
            this.ObjectManager.LoadXML("EquipmentRosters");
            this.ObjectManager.LoadXML("partyTemplates");
            MBObjectManager.Instance.GetObject<WeaponDescription>("OneHandedBastardSwordAlternative")
                ?.SetHiddenFromUI();
            MBObjectManager.Instance.GetObject<WeaponDescription>("OneHandedBastardAxeAlternative")?.SetHiddenFromUI();
        }

        public enum State
        {
            Running,
            Destroying,
            Destroyed,
        }
    }
}