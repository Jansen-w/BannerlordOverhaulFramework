using System;
using System.Collections.Generic;
using BOF.Campaign.Utility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using CharacterObject = BOF.Campaign.Character.CharacterObject;
using Hero = BOF.Campaign.Character.Hero;
using IPartyVisual = BOF.Campaign.Utility.IPartyVisual;
using MapEvent = BOF.Campaign.Utility.MapEvent;
using MapEventSide = BOF.Campaign.Map.Event.MapEventSide;
using Settlement = BOF.Campaign.MapEntity.Settlement;

namespace BOF.Campaign.Party
{
  public sealed class PartyBase : IBattleCombatant
  {
    private static readonly HashSet<TerrainType> ValidTerrainTypes = new HashSet<TerrainType>()
    {
      TerrainType.Snow,
      TerrainType.Steppe,
      TerrainType.Plain,
      TerrainType.Desert,
      TerrainType.Swamp,
      TerrainType.Dune,
      TerrainType.Bridge,
      TerrainType.Forest,
      TerrainType.ShallowRiver
    };

    
    public readonly DeterministicRandom Random;

    [CachedData]
    private int _cachedPartyMemberSizeLimit;

    [CachedData]
    private int _cachedPrisonerSizeLimit;

    [CachedData]
    private float _cachedTotalStrength;

    
    private Hero _customOwner;

    
    private int _index;

    
    private bool _isFirstTick = true;

    [CachedData]
    private int _lastMemberRosterVersionNo;

    [CachedData]
    private int _lastNumberOfMenPerTierVersionNo;

    [CachedData]
    private int _lastNumberOfMenWithHorseVersionNo;

    [CachedData]
    private int _lastPartyLeaderVersionNo;

    [CachedData]
    private CharacterObject _leader;

    
    public MapEvent _mapEventOld;

    
    private MapEventSide _mapEventSide;

    private int[] _numberOfHealthyMenPerTier;

    
    private int _numberOfMenWithHorse;

    [CachedData]
    private int _partyMemberSizeLastCheckVersion;

    [CachedData]
    private int _prisonerSizeLastCheckVersion;

    
    private int _remainingFoodPercentage;

    [CachedData]
    private IPartyVisual _visual;

    public PartyBase(MobileParty mobileParty)
      : this(mobileParty, (Settlement) null)
    {
    }

    public PartyBase(Settlement settlement)
      : this((MobileParty) null, settlement)
    {
    }

    private PartyBase(MobileParty mobileParty, Settlement settlement)
    {
      this.Index = BOFCampaign.Current.GeneratePartyId(this);
      this.MobileParty = mobileParty;
      this.Settlement = settlement;
      this.Random = new DeterministicRandom(this.IsSettlement ? 23 : 5);
      this.ItemRoster = new ItemRoster();
      this.MemberRoster = new TroopRoster(this);
      this.PrisonRoster = new TroopRoster(this);
      this.MemberRoster.NumberChangedCallback = new NumberChangedCallback(this.MemberRosterNumberChanged);
      this.PrisonRoster.NumberChangedCallback = new NumberChangedCallback(this.PrisonRosterNumberChanged);
      this.PrisonRoster.IsPrisonRoster = true;
      this._visual = Campaign.Current.VisualCreator.CreatePartyVisual();
    }

    public Vec2 Position2D => !this.IsMobile ? this.Settlement.Position2D : this.MobileParty.Position2D;

    public bool IsVisible => !this.IsMobile ? this.Settlement.IsVisible : this.MobileParty.IsVisible;

    public bool IsActive => !this.IsMobile ? this.Settlement.IsActive : this.MobileParty.IsActive;

    public SiegeEvent SiegeEvent => !this.IsMobile ? this.Settlement.SiegeEvent : this.MobileParty.SiegeEvent;

    public IPartyVisual Visuals => this._visual;


    public Settlement Settlement { get; private set; }


    public MobileParty MobileParty { get; private set; }

    public bool IsSettlement => this.Settlement != null;

    public bool IsMobile => this.MobileParty != null;


    public TroopRoster MemberRoster { get; private set; }


    public TroopRoster PrisonRoster { get; private set; }


    public ItemRoster ItemRoster { get; private set; }

    public int RemainingFoodPercentage
    {
      get => this._remainingFoodPercentage;
      set => this._remainingFoodPercentage = value;
    }

    public bool IsStarving => this._remainingFoodPercentage < 0;

    public string Id => this.MobileParty?.StringId ?? this.Settlement.StringId;

    public Hero Owner
    {
      get
      {
        Hero customOwner = this._customOwner;
        if (customOwner != null)
          return customOwner;
        return !this.IsMobile ? this.Settlement.Owner : this.MobileParty.Owner;
      }
    }

    public CharacterObject Leader
    {
      get
      {
        if (this.MemberRoster.VersionNo == this._lastPartyLeaderVersionNo)
          return this._leader;
        this._leader = this.MemberRoster.Count == 0 || this.IsMobile && this.MobileParty.IsLeaderless ? (CharacterObject) null : this.MemberRoster.GetCharacterAtIndex(0);
        this._lastPartyLeaderVersionNo = this.MemberRoster.VersionNo;
        return this._leader;
      }
    }

    public Hero LeaderHero => this.Leader?.HeroObject;

    public static PartyBase MainParty => Campaign.Current == null ? (PartyBase) null : Campaign.Current.MainParty.Party;

    public int Index
    {
      get => this._index;
      private set => this._index = value;
    }

    public bool IsValid => this.Index >= 0;

    public IMapEntity MapEntity => this.IsMobile ? (IMapEntity) this.MobileParty : (IMapEntity) this.Settlement;

    public IFaction MapFaction
    {
      get
      {
        if (this.MobileParty != null)
          return this.MobileParty.MapFaction;
        return this.Settlement != null ? this.Settlement.MapFaction : (IFaction) null;
      }
    }

    public CultureObject Culture => this.MapFaction.Culture;

    public MapEvent MapEvent => this._mapEventSide?.MapEvent;

    public MapEventSide MapEventSide
    {
      get => this._mapEventSide;
      set
      {
        if (this._mapEventSide == value)
          return;
        if (this._mapEventSide != null)
        {
          if (this.IsMobile)
            this.MobileParty.OnEventEnded(this._mapEventSide.MapEvent);
          this._mapEventSide.RemoveParty(this);
        }
        this._mapEventSide = value;
        if (this._mapEventSide != null)
          this._mapEventSide.AddParty(this);
        if (this.MobileParty == null)
          return;
        foreach (MobileParty attachedParty in this.MobileParty.AttachedParties)
          attachedParty.Party.MapEventSide = this._mapEventSide;
      }
    }

    public BattleSideEnum OpponentSide => this.Side == BattleSideEnum.Attacker ? BattleSideEnum.Defender : BattleSideEnum.Attacker;

    public int PartySizeLimit
    {
      get
      {
        int versionNo = this.MemberRoster.VersionNo;
        if (this._partyMemberSizeLastCheckVersion != versionNo || this._cachedPartyMemberSizeLimit == 0)
        {
          this._partyMemberSizeLastCheckVersion = versionNo;
          this._cachedPartyMemberSizeLimit = (int) Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(this).ResultNumber;
        }
        return this._cachedPartyMemberSizeLimit;
      }
    }

    public int PrisonerSizeLimit
    {
      get
      {
        int versionNo = this.MemberRoster.VersionNo;
        if (this._prisonerSizeLastCheckVersion != versionNo || this._cachedPrisonerSizeLimit == 0)
        {
          this._prisonerSizeLastCheckVersion = versionNo;
          this._cachedPrisonerSizeLimit = (int) Campaign.Current.Models.PartySizeLimitModel.GetPartyPrisonerSizeLimit(this).ResultNumber;
        }
        return this._cachedPrisonerSizeLimit;
      }
    }

    public ExplainedNumber PartySizeLimitExplainer => Campaign.Current.Models.PartySizeLimitModel.GetPartyMemberSizeLimit(this, true);

    public ExplainedNumber PrisonerSizeLimitExplainer => Campaign.Current.Models.PartySizeLimitModel.GetPartyPrisonerSizeLimit(this, true);

    public int NumberOfHealthyMembers => this.MemberRoster.TotalManCount - this.MemberRoster.TotalWounded;

    public int NumberOfRegularMembers => this.MemberRoster.TotalRegulars;

    public int NumberOfWoundedRegularMembers => this.MemberRoster.TotalWounded;

    public int NumberOfAllMembers => this.MemberRoster.TotalManCount;

    public int NumberOfPrisoners => this.PrisonRoster.TotalManCount;

    public int NumberOfMounts => this.ItemRoster.NumberOfMounts;

    public int NumberOfPackAnimals => this.ItemRoster.NumberOfPackAnimals;

    public IEnumerable<CharacterObject> PrisonerHeroes
    {
      get
      {
        for (int j = 0; j < this.PrisonRoster.Count; ++j)
        {
          if (this.PrisonRoster.GetElementNumber(j) > 0)
          {
            TroopRosterElement elementCopyAtIndex = this.PrisonRoster.GetElementCopyAtIndex(j);
            if (elementCopyAtIndex.Character.IsHero)
              yield return elementCopyAtIndex.Character;
          }
        }
      }
    }

    public int NumberOfMenWithHorse
    {
      get
      {
        if (this._lastNumberOfMenWithHorseVersionNo != this.MemberRoster.VersionNo)
        {
          this.RecalculateNumberOfMenWithHorses();
          this._lastNumberOfMenWithHorseVersionNo = this.MemberRoster.VersionNo;
        }
        return this._numberOfMenWithHorse;
      }
    }

    public int NumberOfMenWithoutHorse => this.NumberOfAllMembers - this.NumberOfMenWithHorse;

    public int InventoryCapacity => this.MobileParty == null ? 100 : (int) Campaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(this.MobileParty).ResultNumber;

    public float TotalStrength
    {
      get
      {
        if (this._lastMemberRosterVersionNo == this.MemberRoster.VersionNo)
          return this._cachedTotalStrength;
        this._cachedTotalStrength = this.CalculateStrength();
        this._lastMemberRosterVersionNo = this.MemberRoster.VersionNo;
        return this._cachedTotalStrength;
      }
    }


    public float AverageBearingRotation { get; set; }

    public TextObject Name
    {
      get
      {
        if (this.IsSettlement)
          return this.Settlement.Name;
        return !this.IsMobile ? TextObject.Empty : this.MobileParty.Name;
      }
    }

    public Tuple<uint, uint> PrimaryColorPair => new Tuple<uint, uint>(this.MapFaction.Color, this.MapFaction.Color2);

    public Tuple<uint, uint> AlternativeColorPair => new Tuple<uint, uint>(this.MapFaction.AlternativeColor, this.MapFaction.AlternativeColor2);

    public Banner Banner
    {
      get
      {
        if (this.LeaderHero != null)
          return this.LeaderHero.ClanBanner;
        return this.MapFaction?.Banner;
      }
    }

    int IBattleCombatant.GetTacticsSkillAmount() => this.LeaderHero != null ? this.LeaderHero.GetSkillValue(DefaultSkills.Tactics) : 0;

    public BattleSideEnum Side
    {
      get
      {
        MapEventSide mapEventSide = this.MapEventSide;
        return mapEventSide == null ? BattleSideEnum.None : mapEventSide.MissionSide;
      }
    }

    public BasicCultureObject BasicCulture => (BasicCultureObject) this.Culture;

    public BasicCharacterObject General
    {
      get
      {
        if (this.MobileParty?.Army != null)
        {
          MobileParty leaderParty = this.MobileParty.Army.LeaderParty;
          if (leaderParty == null)
            return (BasicCharacterObject) null;
          CharacterObject leader = leaderParty.Leader;
          if (leader == null)
            return (BasicCharacterObject) null;
          Hero heroObject = leader.HeroObject;
          return heroObject == null ? (BasicCharacterObject) null : (BasicCharacterObject) heroObject.CharacterObject;
        }
        Hero leaderHero = this.LeaderHero;
        return leaderHero == null ? (BasicCharacterObject) null : (BasicCharacterObject) leaderHero.CharacterObject;
      }
    }

     static void AutoGeneratedStaticCollectObjectsPartyBase(
      object o,
      List<object> collectedObjects)
    {
      ((PartyBase) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
    }

    private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
    {
      collectedObjects.Add((object) this.Random);
      collectedObjects.Add((object) this._mapEventOld);
      collectedObjects.Add((object) this._customOwner);
      collectedObjects.Add((object) this._mapEventSide);
      collectedObjects.Add((object) this.Settlement);
      collectedObjects.Add((object) this.MobileParty);
      collectedObjects.Add((object) this.MemberRoster);
      collectedObjects.Add((object) this.PrisonRoster);
      collectedObjects.Add((object) this.ItemRoster);
    }

     static object AutoGeneratedGetMemberValueSettlement(object o) => (object) ((PartyBase) o).Settlement;

     static object AutoGeneratedGetMemberValueMobileParty(object o) => (object) ((PartyBase) o).MobileParty;

     static object AutoGeneratedGetMemberValueMemberRoster(object o) => (object) ((PartyBase) o).MemberRoster;

     static object AutoGeneratedGetMemberValuePrisonRoster(object o) => (object) ((PartyBase) o).PrisonRoster;

     static object AutoGeneratedGetMemberValueItemRoster(object o) => (object) ((PartyBase) o).ItemRoster;

     static object AutoGeneratedGetMemberValueAverageBearingRotation(object o) => (object) ((PartyBase) o).AverageBearingRotation;

     static object AutoGeneratedGetMemberValueRandom(object o) => (object) ((PartyBase) o).Random;

     static object AutoGeneratedGetMemberValue_mapEventOld(object o) => (object) ((PartyBase) o)._mapEventOld;

     static object AutoGeneratedGetMemberValue_remainingFoodPercentage(object o) => (object) ((PartyBase) o)._remainingFoodPercentage;

     static object AutoGeneratedGetMemberValue_customOwner(object o) => (object) ((PartyBase) o)._customOwner;

     static object AutoGeneratedGetMemberValue_index(object o) => (object) ((PartyBase) o)._index;

     static object AutoGeneratedGetMemberValue_isFirstTick(object o) => (object) ((PartyBase) o)._isFirstTick;

     static object AutoGeneratedGetMemberValue_mapEventSide(object o) => (object) ((PartyBase) o)._mapEventSide;

     static object AutoGeneratedGetMemberValue_numberOfMenWithHorse(object o) => (object) ((PartyBase) o)._numberOfMenWithHorse;

    public void OnVisibilityChanged(bool value)
    {
      this.MapEvent?.PartyVisibilityChanged(this, value);
      CampaignEventDispatcher.Instance.OnPartyVisibilityChanged(this);
      this.Visuals?.SetVisualVisible(value);
      if (Campaign.Current.Models.MapVisibilityListener == null)
        return;
      if (!value)
      {
        Campaign.Current.Models.MapVisibilityListener.OnPartySightLost(this);
      }
      else
      {
        if (!value)
          return;
        Campaign.Current.Models.MapVisibilityListener.OnPartySighted(this);
      }
    }

    public void SetCustomOwner(Hero customOwner) => this._customOwner = customOwner;

     void AfterLoad()
    {
      if (this._mapEventOld != null)
      {
        this._mapEventSide = this._mapEventOld.AttackerSide.Parties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (p => p.Party == this)) >= 0 ? this._mapEventOld.AttackerSide : (this._mapEventOld.DefenderSide.Parties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (p => p.Party == this)) >= 0 ? this._mapEventOld.DefenderSide : (MapEventSide) null);
        this._mapEventOld = (MapEvent) null;
      }
      if (MBSaveLoad.LastLoadedGameVersion <= ApplicationVersion.FromString("e1.6.2", ApplicationVersionGameType.Singleplayer) && this.MemberRoster != (TroopRoster) null && this.IsMobile && this.MobileParty.ActualClan != Clan.PlayerClan && this.LeaderHero != null && !this.MobileParty.IsCurrentlyUsedByAQuest)
      {
        foreach (TroopRosterElement troopRosterElement in this.MemberRoster.GetTroopRoster())
        {
          if (troopRosterElement.Character.IsHero && this.LeaderHero != troopRosterElement.Character.HeroObject)
          {
            if (troopRosterElement.Character.HeroObject == Hero.MainHero)
            {
              if (!Hero.MainHero.IsPrisoner && MobileParty.MainParty.IsActive && Hero.MainHero.PartyBelongedTo != MobileParty.MainParty)
              {
                MobileParty.MainParty.MemberRoster.RemoveIf((Predicate<TroopRosterElement>) (x => x.Character.IsHero && x.Character.HeroObject.IsHumanPlayerCharacter));
                MobileParty.MainParty.MemberRoster.AddToCounts(CharacterObject.PlayerCharacter, 1, true);
              }
            }
            else
              MakeHeroFugitiveAction.Apply(troopRosterElement.Character.HeroObject);
          }
        }
      }
      this.MemberRoster?.PreAfterLoad();
      this.PrisonRoster?.PreAfterLoad();
    }

    private void InitCache()
    {
      this._partyMemberSizeLastCheckVersion = -1;
      this._prisonerSizeLastCheckVersion = -1;
      this._lastNumberOfMenWithHorseVersionNo = -1;
      this._lastNumberOfMenPerTierVersionNo = -1;
      this._lastMemberRosterVersionNo = -1;
      this._lastPartyLeaderVersionNo = -1;
    }

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData) => this.InitCache();

    public int GetNumberOfHealthyMenOfTier(int tier)
    {
      if (tier < 0)
        return 0;
      bool flag = false;
      if (this._numberOfHealthyMenPerTier == null || tier >= this._numberOfHealthyMenPerTier.Length)
      {
        this._numberOfHealthyMenPerTier = new int[Math.Max(tier, 6) + 1];
        flag = true;
      }
      else if (this._lastNumberOfMenPerTierVersionNo != this.MemberRoster.VersionNo)
        flag = true;
      if (flag)
      {
        for (int index = 0; index < this._numberOfHealthyMenPerTier.Length; ++index)
          this._numberOfHealthyMenPerTier[index] = 0;
        for (int index = 0; index < this.MemberRoster.Count; ++index)
        {
          CharacterObject characterAtIndex = this.MemberRoster.GetCharacterAtIndex(index);
          if (characterAtIndex != null && !characterAtIndex.IsHero)
          {
            int tier1 = characterAtIndex.Tier;
            if (tier1 >= 0 && tier1 < this._numberOfHealthyMenPerTier.Length)
            {
              int num = this.MemberRoster.GetElementNumber(index) - this.MemberRoster.GetElementWoundedNumber(index);
              this._numberOfHealthyMenPerTier[tier1] += num;
            }
          }
        }
        this._lastNumberOfMenPerTierVersionNo = this.MemberRoster.VersionNo;
      }
      return this._numberOfHealthyMenPerTier[tier];
    }

    private void RecalculateNumberOfMenWithHorses()
    {
      this._numberOfMenWithHorse = 0;
      for (int index = 0; index < this.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = this.MemberRoster.GetElementCopyAtIndex(index);
        if (elementCopyAtIndex.Character != null && elementCopyAtIndex.Character.IsMounted)
          this._numberOfMenWithHorse += elementCopyAtIndex.Number;
      }
    }

    public int GetNumberOfMenWith(TraitObject trait)
    {
      int num = 0;
      foreach (TroopRosterElement troopRosterElement in this.MemberRoster.GetTroopRoster())
      {
        if (troopRosterElement.Character.GetTraitLevel(trait) > 0)
          num += troopRosterElement.Number;
      }
      return num;
    }

    public int AddPrisoner(CharacterObject element, int numberToAdd) => this.PrisonRoster.AddToCounts(element, numberToAdd);

    public int AddMember(CharacterObject element, int numberToAdd, int numberToAddWounded = 0) => this.MemberRoster.AddToCounts(element, numberToAdd, woundedCount: numberToAddWounded);

    public void AddPrisoners(TroopRoster roster)
    {
      foreach (TroopRosterElement troopRosterElement in roster.GetTroopRoster())
        this.AddPrisoner(troopRosterElement.Character, troopRosterElement.Number);
    }

    public void AddMembers(TroopRoster roster) => this.MemberRoster.Add(roster);

    public override string ToString() => !this.IsSettlement ? this.MobileParty.Name.ToString() : this.Settlement.Name.ToString();

    public void PlaceRandomPositionAroundPosition(Vec2 centerPosition, float radius)
    {
      Vec2 position = new Vec2(0.0f, 0.0f);
      do
      {
        position.x = centerPosition.x + (float) ((double) MBRandom.RandomFloat * (double) radius * 2.0) - radius;
        position.y = centerPosition.y + (float) ((double) MBRandom.RandomFloat * (double) radius * 2.0) - radius;
      }
      while (!Campaign.Current.MapSceneWrapper.AreFacesOnSameIsland(Campaign.Current.MapSceneWrapper.GetFaceIndex(position), Campaign.Current.MapSceneWrapper.GetFaceIndex(centerPosition), false));
      if (!this.IsMobile)
        return;
      this.MobileParty.Position2D = position;
      this.MobileParty.SetMoveModeHold();
    }

    public int AddElementToMemberRoster(
      CharacterObject element,
      int numberToAdd,
      bool insertAtFront = false)
    {
      return this.MemberRoster.AddToCounts(element, numberToAdd, insertAtFront);
    }

    public void AddToMemberRosterElementAtIndex(int index, int numberToAdd, int woundedCount = 0) => this.MemberRoster.AddToCountsAtIndex(index, numberToAdd, woundedCount);

    public void WoundMemberRosterElements(CharacterObject elementObj, int numberToWound) => this.MemberRoster.AddToCounts(elementObj, 0, woundedCount: numberToWound);

    public void WoundMemberRosterElementsWithIndex(int elementIndex, int numberToWound) => this.MemberRoster.AddToCountsAtIndex(elementIndex, 0, numberToWound);

    private float CalculateStrength()
    {
      float num = 0.0f;
      for (int index = 0; index < this.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = this.MemberRoster.GetElementCopyAtIndex(index);
        if (elementCopyAtIndex.Character != null)
          num += (float) (elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber) * Campaign.Current.Models.MilitaryPowerModel.GetTroopPowerBasedOnContext(elementCopyAtIndex.Character);
      }
      return num;
    }

     void Tick(float realDt, float dt)
    {
      if ((this.IsMobile ? (this.MobileParty.IsActive ? 1 : 0) : (this.Settlement.IsActive ? 1 : 0)) == 0)
        return;
      if (this.IsMobile)
        this.MobileParty.CheckCookieExpiration();
      if (this._isFirstTick)
      {
        if (this.MemberRoster.Count == 0 && (double) this.Position2D.x == 0.0)
        {
          double y = (double) this.Position2D.y;
        }
        if (this.Visuals != null)
          this.Visuals.OnPartySizeChanged();
      }
      this.Tick(realDt, dt);
      this._isFirstTick = false;
    }

    private void Tick(float realDt, float dt) => this.Visuals.Tick(realDt, dt, this);

     bool GetCharacterFromPartyRank(
      int partyRank,
      out CharacterObject character,
      out PartyBase party,
      out int stackIndex,
      bool includeWoundeds = false)
    {
      for (int index = 0; index < this.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = this.MemberRoster.GetElementCopyAtIndex(index);
        int num = elementCopyAtIndex.Number - (includeWoundeds ? 0 : elementCopyAtIndex.WoundedNumber);
        partyRank -= num;
        if (partyRank < 0)
        {
          character = elementCopyAtIndex.Character;
          party = this;
          stackIndex = index;
          return true;
        }
      }
      character = (CharacterObject) null;
      party = (PartyBase) null;
      stackIndex = 0;
      return false;
    }

    public static bool IsPositionOkForTraveling(Vec2 position)
    {
      IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
      PathFaceRecord faceIndex = mapSceneWrapper.GetFaceIndex(position);
      if (!faceIndex.IsValid())
        return false;
      TerrainType faceTerrainType = mapSceneWrapper.GetFaceTerrainType(faceIndex);
      return PartyBase.ValidTerrainTypes.Contains(faceTerrainType);
    }

    private void PrisonRosterNumberChanged(
      bool numberchanged,
      bool woundednumberchanged,
      bool heroNumberChaned)
    {
      this.Visuals.OnPartySizeChanged();
    }

    private void MemberRosterNumberChanged(
      bool numberchanged,
      bool woundednumberchanged,
      bool heroNumberChaned)
    {
      this.Visuals.OnPartySizeChanged();
      if (!(numberchanged | heroNumberChaned))
        return;
      CampaignEventDispatcher.Instance.OnPartySizeChanged(this);
    }

    public void UpdateVisibilityAndInspected(float mainPartySeeingRange = 0.0f, bool tickVisuals = false)
    {
      bool isVisible = false;
      bool isInspected = false;
      if (this.IsSettlement)
      {
        isVisible = true;
        if (this.Settlement.IsHideout && !this.Settlement.Hideout.IsSpotted)
          isVisible = false;
        if (isVisible)
          isInspected = PartyBase.CalculateSettlementInspected((IMapPoint) this.Settlement, mainPartySeeingRange);
      }
      else if (this.MobileParty.IsActive && !this.MobileParty.IsCommonAreaParty)
      {
        if (Campaign.Current.TrueSight)
          isVisible = true;
        else if (this.MobileParty.CurrentSettlement == null || this.MobileParty.Leader?.HeroObject?.ClanBanner != null || this.MobileParty.MapEvent != null && this.MobileParty.MapEvent.IsSiegeAssault && this.MobileParty.Party.Side == BattleSideEnum.Attacker)
          PartyBase.CalculateVisibilityAndInspected((IMapPoint) this.MobileParty, out isVisible, out isInspected, mainPartySeeingRange);
      }
      if (this.IsSettlement)
      {
        this.Settlement.IsVisible = isVisible;
        this.Settlement.IsInspected = isInspected;
      }
      else
      {
        this.MobileParty.IsVisible = isVisible;
        this.MobileParty.IsInspected = isInspected;
      }
      if (!tickVisuals || this.IsSettlement)
        return;
      this.MobileParty.Party.Visuals?.SetMapIconAsDirty();
      this.MobileParty.Party.Visuals?.Tick(0.0f, 0.15f, this.MobileParty.Party);
    }

    private static void CalculateVisibilityAndInspected(
      IMapPoint mapPoint,
      out bool isVisible,
      out bool isInspected,
      float mainPartySeeingRange = 0.0f)
    {
      isInspected = false;
      if ((mapPoint is MobileParty mobileParty ? mobileParty.Army : (Army) null) != null && mobileParty.Army.LeaderParty.AttachedParties.IndexOf((TaleWorlds.CampaignSystem.MobileParty)mobileParty) >= 0)
      {
        isVisible = mobileParty.Army.LeaderParty.IsVisible;
      }
      else
      {
        float visibilityRangeOfMapPoint = PartyBase.CalculateVisibilityRangeOfMapPoint(mapPoint, mainPartySeeingRange);
        isVisible = (double) visibilityRangeOfMapPoint > 1.0 && mapPoint.IsActive;
        if (!isVisible)
          return;
        if (mapPoint.IsInspected)
          isInspected = true;
        else
          isInspected = 1.0 / (double) visibilityRangeOfMapPoint < (double) Campaign.Current.Models.MapVisibilityModel.GetPartyRelativeInspectionRange(mapPoint);
      }
    }

    private static bool CalculateSettlementInspected(IMapPoint mapPoint, float mainPartySeeingRange = 0.0f) => 1.0 / (double) PartyBase.CalculateVisibilityRangeOfMapPoint(mapPoint, mainPartySeeingRange) < (double) Campaign.Current.Models.MapVisibilityModel.GetPartyRelativeInspectionRange(mapPoint);

    private static float CalculateVisibilityRangeOfMapPoint(
      IMapPoint mapPoint,
      float mainPartySeeingRange)
    {
      MobileParty mainParty = MobileParty.MainParty;
      float lengthSquared = (mainParty.Position2D - mapPoint.Position2D).LengthSquared;
      float num1 = mainPartySeeingRange;
      if ((double) mainPartySeeingRange == 0.0)
        num1 = mainParty.SeeingRange;
      double num2 = (double) num1 * (double) num1 / (double) lengthSquared;
      float num3 = 0.25f;
      if (mapPoint is MobileParty)
        num3 = Campaign.Current.Models.MapVisibilityModel.GetPartySpottingDifficulty((MobileParty) mapPoint);
      double num4 = (double) num3;
      return (float) (num2 / num4);
    }

     void WeightSurroundedFaces(int weight, bool isTown, int level)
    {
    }

    public void SetAsCameraFollowParty() => Campaign.Current.CameraFollowParty = this;

     void OnFinishLoadState()
    {
      this._visual = Campaign.Current.VisualCreator.CreatePartyVisual();
      this.Visuals.OnStartup(this);
      this.Visuals.SetVisualVisible(this.IsSettlement ? this.Settlement.IsVisible : this.MobileParty.IsVisible);
      this.Visuals.SetMapIconAsDirty();
      this.MobileParty?.OnFinishLoadState();
      this.MemberRoster.NumberChangedCallback = new NumberChangedCallback(this.MemberRosterNumberChanged);
      this.PrisonRoster.NumberChangedCallback = new NumberChangedCallback(this.PrisonRosterNumberChanged);
    }

     void OnHeroAdded(Hero heroObject) => this.MobileParty?.OnHeroAdded(heroObject);

     void OnHeroRemoved(Hero heroObject) => this.MobileParty?.OnHeroRemoved(heroObject);

     void OnHeroAddedAsPrisoner(Hero heroObject) => heroObject.OnAddedToPartyAsPrisoner(this);

     void OnHeroRemovedAsPrisoner(Hero heroObject) => heroObject.OnRemovedFromPartyAsPrisoner(this);

    public void ResetTempXp() => this.MemberRoster.ClearTempXp();

    public void OnGameInitialized()
    {
      if (this.IsMobile)
      {
        this.MobileParty.OnGameInitialized();
      }
      else
      {
        if (!this.IsSettlement)
          return;
        this.Settlement.OnGameInitialized();
      }
    }
  }
}