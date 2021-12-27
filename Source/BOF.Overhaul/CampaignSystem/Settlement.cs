using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem.CampaignSystem
{
  public class Settlement : 
    MBObjectBase,
    ILocatable<Settlement>,
    IMapPoint,
    ITrackableCampaignObject,
    ITrackableBase,
    ISiegeEventSide,
    IMapEntity
  {
    //[SaveableField(102)]
    public int NumberOfLordPartiesTargeting;
    //[CachedData]
    private int _numberOfLordPartiesAt;
    //[SaveableField(104)]
    public int CanBeClaimed;
    //[SaveableField(105)]
    public float ClaimValue;
    //[SaveableField(106)]
    public Hero ClaimedBy;
    //[SaveableField(107)]
    public bool HasVisited;
    //[SaveableField(108)]
    public bool IsQuestSettlement;
    //[CachedData]
    private Dictionary<IFaction, float> _valueForFaction = new Dictionary<IFaction, float>();
    //[SaveableField(110)]
    public float LastVisitTimeOfOwner;
    //[SaveableField(113)]
    private bool _isVisible;
    //[CachedData]
    private int _locatorNodeIndex;
    //[SaveableField(117)]
    private Settlement _nextLocatable;
    //[SaveableField(118)]
    private float _prosperity;
    //[SaveableField(119)]
    private float _readyMilitia;
    //[SaveableField(120)]
    private List<float> _settlementWallSectionHitPointsRatioList = new List<float>();
    public MBReadOnlyList<float> SettlementWallSectionHitPointsRatioList;
    //[CachedData]
    private List<MobileParty> _partiesCache;
    //[CachedData]
    private List<Hero> _heroesWithoutPartyCache;
    //[CachedData]
    private List<Hero> _notablesCache;
    private List<SettlementComponent> _settlementComponents;
    private Vec2 _gatePosition;
    private Vec2 _position;
    public CultureObject Culture;
    private TextObject _name;
    //[SaveableField(129)]
    private List<Village> _boundVillages;
    //[SaveableField(131)]
    private MobileParty _lastAttackerParty;
    //[SaveableField(132)]
    public int PassedHoursAfterLastThreat;
    //[SaveableField(148)]
    private List<SiegeEvent.SiegeEngineMissile> _siegeEngineMissiles;
    //[SaveableField(134)]
    public Town Town;
    //[SaveableField(135)]
    public Village Village;
    //[SaveableField(136)]
    public Hideout Hideout;
    //[SaveableField(137)]
    public bool SettlementTaken;
    //[SaveableField(139)]
    public List<Settlement.SiegeLane> SiegeLanes = new List<Settlement.SiegeLane>();
    //[SaveableField(143)]
    internal Clan _ownerClanDepricated;
    //[CachedData]
    public MilitiaPartyComponent MilitiaPartyComponent;
    //[SaveableField(145)]
    public readonly ItemRoster Stash;
    
    //[SaveableProperty(101)]
    public PartyBase Party { get; private set; }

    public int NumberOfLordPartiesAt => this._numberOfLordPartiesAt;

    //[SaveableProperty(116)]
    public int BribePaid { get; set; }

    //[SaveableProperty(111)]
    public SiegeEvent SiegeEvent { get; set; }

    //[SaveableProperty(112)]
    public bool IsActive { get; set; }

    public Hero Owner => this.OwnerClan.Leader;

    public bool IsVisible
    {
      get => this._isVisible;
      set
      {
        if (this._isVisible == value)
          return;
        this._isVisible = value;
        this.Party.OnVisibilityChanged(value);
      }
    }

    public bool IsInspected { get; set; }

    public int WallSectionCount { get; private set; }

    int ILocatable<Settlement>.LocatorNodeIndex
    {
      get => this._locatorNodeIndex;
      set => this._locatorNodeIndex = value;
    }

    //[SaveableProperty(115)]
    public float NumberOfEnemiesSpottedAround { get; set; }

    //[SaveableProperty(128)]
    public float NumberOfAlliesSpottedAround { get; set; }

    Settlement ILocatable<Settlement>.NextLocatable
    {
      get => this._nextLocatable;
      set => this._nextLocatable = value;
    }

    public float Prosperity
    {
      get => this._prosperity;
      set
      {
        this._prosperity = value;
        if ((double) this._prosperity >= 0.0)
          return;
        this._prosperity = 0.0f;
      }
    }

    public Vec2 GetPosition2D => this.Position2D;

    public float Militia
    {
      get => (this.MilitiaPartyComponent == null || !this.MilitiaPartyComponent.MobileParty.IsActive ? 0.0f : (float) this.MilitiaPartyComponent.MobileParty.Party.NumberOfAllMembers) + this._readyMilitia;
      set
      {
        int num = this.MilitiaPartyComponent == null || !this.MilitiaPartyComponent.MobileParty.IsActive ? 0 : this.MilitiaPartyComponent.MobileParty.Party.NumberOfAllMembers;
        this._readyMilitia = value - (float) num;
        if ((double) this._readyMilitia < (double) -num)
          this._readyMilitia = (float) -num;
        if ((double) this._readyMilitia >= -1.0 && (double) this._readyMilitia <= 1.0)
          return;
        if (this.MilitiaPartyComponent != null)
          this.TransferReadyMilitiasToMilitiaParty();
        else
          this.SpawnMilitiaParty();
      }
    }

    public float SettlementTotalWallHitPoints
    {
      get
      {
        float num = 0.0f;
        foreach (float sectionHitPointsRatio in this._settlementWallSectionHitPointsRatioList)
          num += sectionHitPointsRatio;
        return num * this.MaxHitPointsOfOneWallSection;
      }
    }

    public float MaxHitPointsOfOneWallSection => this.WallSectionCount == 0 ? 0.0f : this.MaxWallHitPoints / (float) this.WallSectionCount;

    public void SetWallSectionHitPointsRatioAtIndex(int index, float hitPointsRatio) => this._settlementWallSectionHitPointsRatioList[index] = MBMath.ClampFloat(hitPointsRatio, 0.0f, 1f);

    //[SaveableProperty(121)]
    public float SettlementHitPoints { get; internal set; }

    public float MaxWallHitPoints => Campaign.Current.Models.WallHitPointCalculationModel.CalculateMaximumWallHitPoint(this.Town);

    public MBReadOnlyList<MobileParty> Parties { get; private set; }

    public MBReadOnlyList<Hero> HeroesWithoutParty { get; private set; }

    public MBReadOnlyList<Hero> Notables { get; private set; }

    public IEnumerable<SettlementComponent> SettlementComponents => this._settlementComponents.AsEnumerable<SettlementComponent>();

    public Vec2 GatePosition
    {
      get => this._gatePosition;
      private set
      {
        this._gatePosition = value;
        Campaign current = Campaign.Current;
        if (current.MapSceneWrapper == null)
          return;
        this.CurrentNavigationFace = current.MapSceneWrapper.GetFaceIndex(this._gatePosition);
      }
    }

    public Vec2 Position2D
    {
      get => this._position;
      private set
      {
        this._position = value;
        Campaign.Current.SettlementLocator.UpdateParty(this);
      }
    }

    public PathFaceRecord CurrentNavigationFace { get; private set; }

    public Vec3 GetLogicalPosition()
    {
      float height = 0.0f;
      Campaign.Current.MapSceneWrapper.GetHeightAtPoint(this.Position2D, ref height);
      return new Vec3(this.Position2D.x, this.Position2D.y, height);
    }

    public IFaction MapFaction => this.Town?.MapFaction ?? this.Village?.Bound.MapFaction ?? this.Hideout?.MapFaction ?? (IFaction) null;

    public TextObject Name
    {
      get => this._name;
      set => this.SetName(value);
    }

    public TextObject EncyclopediaText { get; private set; }

    public string EncyclopediaLink => Campaign.Current.EncyclopediaManager.GetIdentifier(typeof (Settlement)) + "-" + this.StringId ?? "";

    public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetSettlementHyperlinkText(this.EncyclopediaLink, this.Name);

    public ItemRoster ItemRoster => this.Party.ItemRoster;

    public MBReadOnlyList<Village> BoundVillages { get; private set; }

    public DeterministicRandom Random => this.Party.Random;

    public MobileParty LastAttackerParty
    {
      get => this._lastAttackerParty;
      set
      {
        if (this._lastAttackerParty != value)
        {
          this._lastAttackerParty = value;
          if (value != null && (this.IsFortification || this.IsVillage))
          {
            foreach (Settlement settlement in Settlement.All)
            {
              if ((settlement.IsFortification || settlement.IsVillage) && settlement.LastAttackerParty == value)
                settlement.LastAttackerParty = (MobileParty) null;
            }
          }
          this._lastAttackerParty = value;
        }
        if (value != null)
          this.PassedHoursAfterLastThreat = 24;
        else
          this.PassedHoursAfterLastThreat = 0;
      }
    }

    //[SaveableProperty(149)]
    public SiegeEvent.SiegeEnginesContainer SiegeEngines { get; private set; }

    public MBReadOnlyList<SiegeEvent.SiegeEngineMissile> SiegeEngineMissiles { get; private set; }

    public BattleSideEnum BattleSide => BattleSideEnum.Defender;

    //[SaveableProperty(150)]
    public int NumberOfTroopsKilledOnSide { get; private set; }

    //[SaveableProperty(151)]
    public SiegeStrategy SiegeStrategy { get; private set; }

    public IEnumerable<PartyBase> SiegeParties
    {
      get
      {
        yield return this.Party;
        foreach (MobileParty party in this.Parties)
        {
          if (party.MapFaction.IsAtWarWith(this.SiegeEvent.BesiegerCamp.BesiegerParty.MapFaction) && !party.IsVillager && !party.IsCaravan && !this.InRebelliousState || this.InRebelliousState && !party.IsMilitia)
            yield return party.Party;
        }
      }
    }

    internal void AddBoundVillageInternal(Village village) => this._boundVillages.Add(village);

    internal void RemoveBoundVillageInternal(Village village) => this._boundVillages.Remove(village);

    //[SaveableProperty(133)]
    public List<CommonArea> CommonAreas { get; private set; }

    private void SetName(TextObject name)
    {
      this._name = name;
      this.SetNameAttributes();
    }

    private void SetNameAttributes()
    {
      this._name.SetTextVariable("IS_SETTLEMENT", 1);
      this._name.SetTextVariable("IS_CASTLE", this.IsCastle ? 1 : 0);
      this._name.SetTextVariable("IS_TOWN", this.IsTown ? 1 : 0);
      this._name.SetTextVariable("IS_HIDEOUT", this.IsHideout ? 1 : 0);
    }

    private void InitSettlement()
    {
      this._settlementComponents = new List<SettlementComponent>();
      this._partiesCache = new List<MobileParty>();
      this.Parties = new MBReadOnlyList<MobileParty>(this._partiesCache);
      this._heroesWithoutPartyCache = new List<Hero>();
      this.HeroesWithoutParty = new MBReadOnlyList<Hero>(this._heroesWithoutPartyCache);
      this._notablesCache = new List<Hero>();
      this.Notables = new MBReadOnlyList<Hero>(this._notablesCache);
      this._boundVillages = new List<Village>();
      this.BoundVillages = this._boundVillages.GetReadOnlyList<Village>();
      this.CurrentManagementAIState = Settlement.ManagementAIState.ConstructBuilding;
      this.SettlementHitPoints = 1f;
      this.CurrentSiegeState = Settlement.SiegeState.OnTheWalls;
      this.LastVisitTimeOfOwner = Campaign.CurrentTime;
      this.SettlementWallSectionHitPointsRatioList = new MBReadOnlyList<float>(this._settlementWallSectionHitPointsRatioList);
    }

    public bool IsTown => this.Town != null && this.Town.IsTown;

    public bool IsCastle => this.Town != null && this.Town.IsCastle;

    public bool IsFortification => this.IsTown || this.IsCastle;

    public bool IsVillage => this.Village != null;

    public bool IsHideout => this.Hideout != null;

    public bool IsStarving => this.Town != null && (double) this.Town.FoodStocks <= 0.0;

    public bool IsRaided => this.IsVillage && this.Village.VillageState == this.Village.VillageStates.Looted;

    public bool IsBooming
    {
      get
      {
        float num = 50f;
        if (this.IsTown || this.IsCastle)
          num = this.Town.Loyalty;
        else if (this.IsVillage)
          num = this.Village.Bound.Town.Loyalty;
        return (double) num > 80.0;
      }
    }

    public bool InRebelliousState => (this.IsTown || this.IsCastle) && this.Town.InRebelliousState;

    public bool IsUnderRaid => this.Party.MapEvent != null && this.Party.MapEvent.IsRaid;

    public bool IsUnderSiege => this.SiegeEvent != null && this.SiegeEvent.BesiegerCamp.SiegeParties.Any<PartyBase>();

    public bool IsUnderRebellionAttack()
    {
      if (this.Party.MapEvent != null && this.Party.MapEvent.IsSiegeAssault)
      {
        Hero owner = this.Party.MapEvent.AttackerSide.LeaderParty.MobileParty.Party.Owner;
        if (owner != null && owner.Clan.IsRebelClan)
          return true;
      }
      return false;
    }

    public T AddComponent<T>() where T : SettlementComponent
    {
      T obj = default (T);
      if (typeof (T).IsSubclassOf(typeof (SettlementComponent)))
        obj = Activator.CreateInstance(typeof (T)) as T;
      if ((object) obj != null)
      {
        obj.Owner = this.Party;
        this._settlementComponents.Add((SettlementComponent) obj);
        obj.OnStart();
      }
      return obj;
    }

    public SettlementComponent GetComponent(Type type)
    {
      for (int index = 0; index < this._settlementComponents.Count; ++index)
      {
        if (type.IsInstanceOfType((object) this._settlementComponents[index]))
          return this._settlementComponents[index];
      }
      return (SettlementComponent) null;
    }

    public T GetComponent<T>() where T : SettlementComponent
    {
      for (int index = 0; index < this._settlementComponents.Count; ++index)
      {
        if (this._settlementComponents[index] is T)
          return (T) this._settlementComponents[index];
      }
      return default (T);
    }

    public Settlement()
      : this(new TextObject("{=!}unnamed"), (LocationComplex) null, (PartyTemplateObject) null)
    {
    }

    public Settlement(TextObject name, LocationComplex locationComplex, PartyTemplateObject pt)
    {
      this._name = name;
      this._isVisible = true;
      this.IsActive = true;
      this.Party = new PartyBase(this);
      this.InitSettlement();
      this._position = Vec2.Zero;
      this.LocationComplex = locationComplex;
      this.CommonAreas = new List<CommonArea>();
      this.HasVisited = false;
      this.Stash = new ItemRoster();
    }

    private Settlement.ManagementAIState CurrentManagementAIState { get; set; }

    public void Think()
    {
      if (this.CurrentManagementAIState != Settlement.ManagementAIState.GiveQuest)
        return;
      PartyBase partyBase = (PartyBase) null;
      float maximumDistance = 1E+07f;
      foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
      {
        float distance;
        if (mobileParty.MapFaction.IsBanditFaction && Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty, this, maximumDistance, out distance))
        {
          maximumDistance = distance;
          partyBase = mobileParty.Party;
        }
      }
      if (partyBase == null)
        throw new MBNullParameterException("[DEBUG]targetParty");
    }

    //[SaveableProperty(138)]
    public LocationComplex LocationComplex { get; private set; }

    public static Settlement CurrentSettlement
    {
      get
      {
        if (PlayerCaptivity.CaptorParty != null && PlayerCaptivity.CaptorParty.IsSettlement)
          return PlayerCaptivity.CaptorParty.Settlement;
        if (PlayerEncounter.EncounterSettlement != null)
          return PlayerEncounter.EncounterSettlement;
        return MobileParty.MainParty.CurrentSettlement != null ? MobileParty.MainParty.CurrentSettlement : (Settlement) null;
      }
    }

    public float GetValue(bool countAlsoBoundedSettlements = true)
    {
      float num = 0.0f;
      if (this.IsVillage)
        num = (float) ((100000.0 + (double) this.Village.Hearth * 250.0) * (this.Village.VillageState == this.Village.VillageStates.Looted ? 0.800000011920929 : (this.Village.VillageState == this.Village.VillageStates.BeingRaided ? 0.850000023841858 : 0.800000011920929 + (0.666999995708466 + 0.333000004291534 * (double) this.Village.Settlement.SettlementHitPoints) * 0.200000002980232)));
      else if (this.IsCastle)
        num = (float) (250000.0 + (double) this.Prosperity * 1000.0);
      else if (this.IsTown)
        num = (float) (750000.0 + (double) this.Prosperity * 1000.0);
      if (countAlsoBoundedSettlements)
      {
        foreach (Village boundVillage in this.BoundVillages)
          num += boundVillage.Settlement.GetValue(false);
      }
      return num;
    }

    public override TextObject GetName() => this.Name;

    public float GetSettlementValueForFaction(IFaction faction)
    {
      float valueForFaction;
      if (!this._valueForFaction.TryGetValue(faction, out valueForFaction))
      {
        valueForFaction = Campaign.Current.Models.SettlementValueModel.CalculateValueForFaction(this, faction);
        this._valueForFaction.Add(faction, valueForFaction);
      }
      return valueForFaction;
    }

    public void CalculateSettlementValueForFactions()
    {
      this._valueForFaction.Clear();
      if (this.IsFortification)
      {
        foreach (Kingdom kingdom in Kingdom.All)
        {
          if (!kingdom.IsEliminated)
          {
            float valueForFaction = Campaign.Current.Models.SettlementValueModel.CalculateValueForFaction(this, (IFaction) kingdom);
            this._valueForFaction.Add((IFaction) kingdom, valueForFaction);
          }
        }
        this._valueForFaction.Add((IFaction) this.OwnerClan, Campaign.Current.Models.SettlementValueModel.CalculateValueForFaction(this, (IFaction) this.OwnerClan));
      }
      else
      {
        if (!this.IsVillage)
          return;
        this._valueForFaction.Add(this.MapFaction, Campaign.Current.Models.SettlementValueModel.CalculateValueForFaction(this, this.MapFaction));
      }
    }

    public override string ToString() => this.Name.ToString();

    public void OnRelatedPartyRemoved(MobileParty mobileParty)
    {
      foreach (SettlementComponent settlementComponent in this.SettlementComponents)
        settlementComponent.OnRelatedPartyRemoved(mobileParty);
    }

    internal void AddMobileParty(MobileParty mobileParty)
    {
      if (!this._partiesCache.Contains(mobileParty))
      {
        this._partiesCache.Add(mobileParty);
        if (!mobileParty.IsLordParty)
          return;
        ++this._numberOfLordPartiesAt;
      }
      else
        Debug.FailedAssert("mobileParty is already in mobileParties List!", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Settlement.cs", nameof (AddMobileParty), 991);
    }

    internal void RemoveMobileParty(MobileParty mobileParty)
    {
      if (this._partiesCache.Contains(mobileParty))
      {
        this._partiesCache.Remove(mobileParty);
        if (!mobileParty.IsLordParty)
          return;
        --this._numberOfLordPartiesAt;
      }
      else
        Debug.FailedAssert("mobileParty is not in mobileParties List", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Settlement.cs", nameof (RemoveMobileParty), 1010);
    }

    internal void AddHeroWithoutParty(Hero individual)
    {
      if (!this._heroesWithoutPartyCache.Contains(individual))
      {
        this._heroesWithoutPartyCache.Add(individual);
        this.CollectNotablesToCache();
      }
      else
        Debug.FailedAssert("Notable is already in Notable List!", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Settlement.cs", nameof (AddHeroWithoutParty), 1029);
    }

    internal void RemoveHeroWithoutParty(Hero individual)
    {
      if (this._heroesWithoutPartyCache.Contains(individual))
      {
        this._heroesWithoutPartyCache.Remove(individual);
        this.CollectNotablesToCache();
      }
      else
        Debug.FailedAssert("Notable is not in Notable List", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Settlement.cs", nameof (RemoveHeroWithoutParty), 1043);
    }

    private void CollectNotablesToCache()
    {
      this._notablesCache.Clear();
      foreach (Hero hero in this.HeroesWithoutParty)
      {
        if (hero.IsNotable)
          this._notablesCache.Add(hero);
      }
    }

    public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    {
      bool isInitialized = this.IsInitialized;
      base.Deserialize(objectManager, node);
      this.Name = new TextObject(node.Attributes["name"].Value);
      this.Position2D = new Vec2((float) Convert.ToDouble(node.Attributes["posX"].Value), (float) Convert.ToDouble(node.Attributes["posY"].Value));
      this.GatePosition = this.Position2D;
      if (node.Attributes["gate_posX"] != null)
        this.GatePosition = new Vec2((float) Convert.ToDouble(node.Attributes["gate_posX"].Value), (float) Convert.ToDouble(node.Attributes["gate_posY"].Value));
      if (!isInitialized && node.Attributes["prosperity"] != null)
        this.Prosperity = (float) Convert.ToDouble(node.Attributes["prosperity"].Value);
      this.Culture = objectManager.ReadObjectReferenceFromXml<CultureObject>("culture", node);
      this.EncyclopediaText = node.Attributes["text"] != null ? new TextObject(node.Attributes["text"].Value) : TextObject.Empty;
      if (Campaign.Current != null && Campaign.Current.MapSceneWrapper != null && !Campaign.Current.MapSceneWrapper.GetFaceIndex(this.Position2D).IsValid())
        Debug.Print("Center position of settlement(" + (object) this.GetName() + ") is invalid");
      foreach (XmlNode childNode1 in node.ChildNodes)
      {
        if (childNode1.Name == "Components")
        {
          foreach (XmlNode childNode2 in childNode1.ChildNodes)
          {
            SettlementComponent objectFromXmlNode = (SettlementComponent) objectManager.CreateObjectFromXmlNode(childNode2);
            objectFromXmlNode.Owner = this.Party;
            this._settlementComponents.Add(objectFromXmlNode);
          }
        }
        if (childNode1.Name == "Locations")
        {
          LocationComplexTemplate complexTemplate = (LocationComplexTemplate) objectManager.ReadObjectReferenceFromXml("complex_template", typeof (LocationComplexTemplate), childNode1);
          if (!isInitialized)
            this.LocationComplex = new LocationComplex(complexTemplate);
          else
            this.LocationComplex.Initialize(complexTemplate);
          foreach (XmlNode childNode3 in childNode1.ChildNodes)
          {
            if (childNode3.Name == "Location")
            {
              Location locationWithId = this.LocationComplex.GetLocationWithId(childNode3.Attributes["id"].Value);
              if (childNode3.Attributes["max_prosperity"] != null)
                locationWithId.ProsperityMax = int.Parse(childNode3.Attributes["max_prosperity"].Value);
              bool flag = false;
              for (int upgradeLevel = 0; upgradeLevel < 4; ++upgradeLevel)
              {
                string name = "scene_name" + (upgradeLevel > 0 ? "_" + (object) upgradeLevel : "");
                string sceneName = childNode3.Attributes[name] != null ? childNode3.Attributes[name].Value : "";
                flag = flag || !string.IsNullOrEmpty(sceneName);
                locationWithId.SetSceneName(upgradeLevel, sceneName);
              }
            }
          }
        }
        if (childNode1.Name == "CommonAreas")
        {
          int index = 0;
          foreach (XmlNode childNode4 in childNode1.ChildNodes)
          {
            if (childNode4.Name == "Area")
            {
              CommonArea.CommonAreaType result;
              Enum.TryParse<CommonArea.CommonAreaType>(childNode4.Attributes["type"].Value, true, out result);
              string str = childNode4.Attributes["name"].Value;
              string tag = "common_area_" + (object) (index + 1);
              if (!isInitialized)
                this.CommonAreas.Add(new CommonArea(this, tag, result, new TextObject(str)));
              else
                this.CommonAreas[index].Initialize(this, tag, result, new TextObject(str));
              ++index;
            }
          }
          foreach (CommonArea commonArea1 in this.CommonAreas)
          {
            foreach (CommonArea commonArea2 in this.CommonAreas)
              ;
          }
        }
        if (childNode1.Name == "ExcludedSiegeEquipments")
        {
          this.SiegeLanes = new List<Settlement.SiegeLane>(childNode1.ChildNodes.Count);
          foreach (XmlNode childNode5 in childNode1.ChildNodes)
          {
            Settlement.SiegeLaneEnum result1;
            Enum.TryParse<Settlement.SiegeLaneEnum>(childNode5.Attributes["id"].Value, true, out result1);
            bool result2;
            bool.TryParse(childNode5.Attributes["isGate"].Value, out result2);
            bool result3;
            bool.TryParse(childNode5.Attributes["isSiegeMachineApplicable"].Value, out result3);
            this.SiegeLanes.Add(new Settlement.SiegeLane(result1, result2, result3));
          }
        }
      }
      this.GetComponent<SettlementComponent>();
      foreach (SettlementComponent settlementComponent in this.SettlementComponents)
        settlementComponent.OnStart();
      if (!isInitialized)
      {
        Clan clan = objectManager.ReadObjectReferenceFromXml<Clan>("owner", node);
        if (clan != null && this.Town != null)
          this.Town.OwnerClan = clan;
      }
      this.SetNameAttributes();
    }

    public void OnFinishLoadState()
    {
      if (this._valueForFaction == null)
        this._valueForFaction = new Dictionary<IFaction, float>();
      if (this.IsUnderSiege)
      {
        float height = 0.0f;
        Campaign.Current.MapSceneWrapper.GetHeightAtPoint(this.GatePosition, ref height);
        this.Party.Visuals.OnBesieged(new Vec3(this.GatePosition.x, this.GatePosition.y, height));
      }
      this.WallSectionCount = !this.IsFortification ? 0 : this.Party.Visuals.GetBreacableWallFrameCount();
      this.GetComponent(typeof (SettlementComponent))?.OnFinishLoadState();
      if (!(MBSaveLoad.LastLoadedGameVersion <= ApplicationVersion.FromString("e1.6.4.286844", ApplicationVersionGameType.Singleplayer)) || this.OwnerClan == null || !this.OwnerClan.IsEliminated)
        return;
      Clan clan = FactionHelper.ChooseHeirClanForFiefs(this.OwnerClan);
      foreach (Settlement settlement in this.OwnerClan.Settlements.ToList<Settlement>())
      {
        if (settlement.IsTown || settlement.IsCastle)
        {
          Hero elementWithPredicate = clan.Lords.GetRandomElementWithPredicate<Hero>((Func<Hero, bool>) (x => !x.IsChild));
          ChangeOwnerOfSettlementAction.ApplyByDestroyClan(settlement, elementWithPredicate);
        }
      }
    }

    public void OnGameInitialized() => this.CurrentNavigationFace = Campaign.Current.MapSceneWrapper.GetFaceIndex(this.GatePosition);

    public void OnGameCreated()
    {
      this.GetComponent<SettlementComponent>().OnInit();
      this.CreateFigure();
      this.Party.Visuals.RefreshLevelMask(this.Party);
      this.WallSectionCount = !this.IsFortification ? 0 : this.Party.Visuals.GetBreacableWallFrameCount();
      for (int index = 0; index < this.WallSectionCount; ++index)
        this._settlementWallSectionHitPointsRatioList.Add(1f);
    }

    public void OnSessionStart() => this.Party?.Visuals?.SetMapIconAsDirty();

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    {
      this._settlementComponents = new List<SettlementComponent>();
      this.SettlementWallSectionHitPointsRatioList = new MBReadOnlyList<float>(this._settlementWallSectionHitPointsRatioList);
      this.BoundVillages = this._boundVillages.GetReadOnlyList<Village>();
      List<SiegeEvent.SiegeEngineMissile> siegeEngineMissiles = this._siegeEngineMissiles;
      this.SiegeEngineMissiles = siegeEngineMissiles != null ? siegeEngineMissiles.GetReadOnlyList<SiegeEvent.SiegeEngineMissile>() : (MBReadOnlyList<SiegeEvent.SiegeEngineMissile>) null;
      ((ILocatable<Settlement>) this).LocatorNodeIndex = -1;
      this._partiesCache = new List<MobileParty>();
      this.Parties = new MBReadOnlyList<MobileParty>(this._partiesCache);
      this._heroesWithoutPartyCache = new List<Hero>();
      this.HeroesWithoutParty = new MBReadOnlyList<Hero>(this._heroesWithoutPartyCache);
      this._notablesCache = new List<Hero>();
      this._valueForFaction = new Dictionary<IFaction, float>();
    }

    public static Settlement Find(string idString) => MBObjectManager.Instance.GetObject<Settlement>(idString);

    public static Settlement FindFirst(Func<Settlement, bool> predicate) => Settlement.All.FirstOrDefault<Settlement>((Func<Settlement, bool>) (x => predicate(x)));

    public static IEnumerable<Settlement> FindAll(
      Func<Settlement, bool> predicate)
    {
      return Settlement.All.Where<Settlement>((Func<Settlement, bool>) (x => predicate(x)));
    }

    public static MBReadOnlyList<Settlement> All => Campaign.Current.Settlements;

    public static Settlement GetFirst => Settlement.All.FirstOrDefault<Settlement>();

    public static IEnumerable<Settlement> FindSettlementsAroundPosition(
      Vec2 position,
      float radius,
      Func<Settlement, bool> condition = null)
    {
      return condition != null ? Campaign.Current.SettlementLocator.FindPartiesAroundPosition(position, radius, condition) : Campaign.Current.SettlementLocator.FindPartiesAroundPosition(position, radius);
    }

    public void OnPlayerEncounterFinish()
    {
      if (this.LocationComplex == null)
        return;
      this.LocationComplex.ClearTempCharacters();
    }

    TextObject ITrackableBase.GetName() => this.Name;

    public Vec3 GetPosition() => this.GetLogicalPosition();

    public float GetTrackDistanceToMainAgent() => this.GetPosition().Distance(Hero.MainHero.GetPosition());

    public bool CheckTracked(BasicCharacterObject basicCharacter) => this.Notables.Any<Hero>((Func<Hero, bool>) (t => t.CharacterObject == basicCharacter)) || this.Party.PrisonRoster.GetTroopRoster().Any<TroopRosterElement>((Func<TroopRosterElement, bool>) (t => t.Character == basicCharacter)) || this.Parties.Any<MobileParty>((Func<MobileParty, bool>) (p => p.CheckTracked(basicCharacter)));

    private void CreateFigure() => this.Party.Visuals.OnStartup(this.Party);

    //[SaveableProperty(142)]
    public Settlement.SiegeState CurrentSiegeState { get; private set; }

    public Clan OwnerClan
    {
      get
      {
        if (this.Village != null)
          return this.Village.Bound.OwnerClan;
        if (this.Town != null)
          return this.Town.OwnerClan;
        return this.IsHideout ? this.Hideout.MapFaction as Clan : (Clan) null;
      }
    }

    public bool IsAlerted => (double) this.NumberOfEnemiesSpottedAround >= 1.0;

    public void SetNextSiegeState()
    {
      if (this.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
        return;
      ++this.CurrentSiegeState;
    }

    public void ResetSiegeState() => this.CurrentSiegeState = Settlement.SiegeState.OnTheWalls;

    public void AddGarrisonParty(bool addInitialGarrison = false) => GarrisonPartyComponent.CreateGarrisonParty("garrison_party_" + this.StringId + "_" + this.OwnerClan.StringId + "_1", this, addInitialGarrison);

    protected override void AfterLoad()
    {
      if (this.SiegeEvent != null && this.SiegeEvent.BesiegerCamp.BesiegerParty == null)
        this.SiegeEvent = (SiegeEvent) null;
      this._notablesCache = new List<Hero>();
      this.Notables = new MBReadOnlyList<Hero>(this._notablesCache);
      this.CollectNotablesToCache();
      this.Party.AfterLoad();
      if (this._oldMilitaParty != null)
        this.Militia = (float) this._oldMilitaParty.MemberRoster.TotalHealthyCount + this._readyMilitia;
      this.Party.UpdateVisibilityAndInspected();
    }

    //[SaveableProperty(146)]
    public MobileParty _oldMilitaParty { get; set; }

    private void SpawnMilitiaParty()
    {
      this.MilitiaPartyComponent.CreateMilitiaParty("militias_of_" + this.StringId + "_aaa1", this);
      this.TransferReadyMilitiasToMilitiaParty();
    }

    private void TransferReadyMilitiasToMilitiaParty()
    {
      if ((double) this._readyMilitia >= 1.0)
      {
        int militiaToAdd = MathF.Floor(this._readyMilitia);
        this._readyMilitia -= (float) militiaToAdd;
        this.AddMilitiasToParty(this.MilitiaPartyComponent.MobileParty, militiaToAdd);
      }
      else
      {
        if ((int) this._readyMilitia >= -1)
          return;
        int num = MathF.Ceiling(this._readyMilitia);
        this._readyMilitia -= (float) num;
        Settlement.RemoveMilitiasFromParty(this.MilitiaPartyComponent.MobileParty, -num);
      }
    }

    private void AddMilitiasToParty(MobileParty militaParty, int militiaToAdd)
    {
      float meleeTroopRate;
      Campaign.Current.Models.SettlementMilitiaModel.CalculateMilitiaSpawnRate(this, out meleeTroopRate, out float _);
      this.AddTroopToMilitiaParty(militaParty, this.Culture.MeleeMilitiaTroop, this.Culture.MeleeEliteMilitiaTroop, meleeTroopRate, ref militiaToAdd);
      this.AddTroopToMilitiaParty(militaParty, this.Culture.RangedMilitiaTroop, this.Culture.RangedEliteMilitiaTroop, 1f, ref militiaToAdd);
    }

    private void AddTroopToMilitiaParty(
      MobileParty militaParty,
      CharacterObject militiaTroop,
      CharacterObject eliteMilitiaTroop,
      float troopRatio,
      ref int numberToAddRemaining)
    {
      if (numberToAddRemaining <= 0)
        return;
      int num = MBRandom.RoundRandomized(troopRatio * (float) numberToAddRemaining);
      float militiaSpawnChance = Campaign.Current.Models.SettlementMilitiaModel.CalculateEliteMilitiaSpawnChance(this);
      for (int index = 0; index < num; ++index)
      {
        if ((double) MBRandom.RandomFloat < (double) militiaSpawnChance)
          militaParty.MemberRoster.AddToCounts(eliteMilitiaTroop, 1);
        else
          militaParty.MemberRoster.AddToCounts(militiaTroop, 1);
      }
      numberToAddRemaining -= num;
    }

    private static void RemoveMilitiasFromParty(MobileParty militaParty, int numberToRemove)
    {
      if (militaParty.MemberRoster.TotalManCount <= numberToRemove)
      {
        militaParty.MemberRoster.Clear();
      }
      else
      {
        float num1 = (float) numberToRemove / (float) militaParty.MemberRoster.TotalManCount;
        int num2 = numberToRemove;
        for (int index = 0; index < militaParty.MemberRoster.Count; ++index)
        {
          int num3 = MBRandom.RoundRandomized((float) militaParty.MemberRoster.GetElementNumber(index) * num1);
          if (num3 > num2)
            num3 = num2;
          militaParty.MemberRoster.AddToCountsAtIndex(index, -num3, removeDepleted: false);
          num2 -= num3;
          if (num2 <= 0)
            break;
        }
        militaParty.MemberRoster.RemoveZeroCounts();
      }
    }

    public void SetSiegeStrategy(SiegeStrategy strategy) => this.SiegeStrategy = strategy;

    public void InitializeSiegeEventSide()
    {
      this.SiegeStrategy = DefaultSiegeStrategies.Custom;
      this.NumberOfTroopsKilledOnSide = 0;
      this.SiegeEngines = new SiegeEvent.SiegeEnginesContainer(BattleSideEnum.Defender, (SiegeEvent.SiegeEngineConstructionProgress) null);
      this._siegeEngineMissiles = new List<SiegeEvent.SiegeEngineMissile>();
      this.SiegeEngineMissiles = new MBReadOnlyList<SiegeEvent.SiegeEngineMissile>(this._siegeEngineMissiles);
      this.SetPrebuiltSiegeEngines();
      float height = 0.0f;
      Campaign.Current.MapSceneWrapper.GetHeightAtPoint(this.GatePosition, ref height);
      this.Party.Visuals.OnBesieged(new Vec3(this.GatePosition.x, this.GatePosition.y, height));
    }

    public void OnTroopsKilledOnSide(int killCount) => this.NumberOfTroopsKilledOnSide += killCount;

    public void AddSiegeEngineMissile(SiegeEvent.SiegeEngineMissile missile) => this._siegeEngineMissiles.Add(missile);

    public void RemoveDeprecatedMissiles() => this._siegeEngineMissiles.RemoveAll((Predicate<SiegeEvent.SiegeEngineMissile>) (missile => missile.CollisionTime.IsPast));

    private void SetPrebuiltSiegeEngines()
    {
      foreach (SiegeEngineType siegeEngine in Campaign.Current.Models.SiegeEventModel.GetPrebuiltSiegeEnginesOfSettlement(this))
      {
        float siegeEngineHitPoints = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineHitPoints(this.SiegeEvent, siegeEngine, BattleSideEnum.Defender);
        SiegeEvent.SiegeEngineConstructionProgress constructionProgress = new SiegeEvent.SiegeEngineConstructionProgress(siegeEngine, 1f, siegeEngineHitPoints);
        this.SiegeEngines.AddPrebuiltEngineToReserve(constructionProgress);
        this.SiegeEvent.CreateSiegeObject(constructionProgress, this.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender));
      }
    }

    public void GetAttackTarget(
      ISiegeEventSide siegeEventSide,
      SiegeEngineType siegeEngine,
      int siegeEngineSlot,
      out SiegeBombardTargets targetType,
      out int targetIndex)
    {
      targetType = SiegeBombardTargets.None;
      targetIndex = -1;
      int targetIndex1;
      float targetPriority;
      this.SiegeEvent.FindAttackableRangedEngineWithHighestPriority(siegeEventSide, siegeEngineSlot, out targetIndex1, out targetPriority);
      if (targetIndex1 == -1 || (double) MBRandom.RandomFloat * (double) targetPriority >= (double) targetPriority)
        return;
      targetIndex = targetIndex1;
      targetType = SiegeBombardTargets.RangedEngines;
    }

    public void FinalizeSiegeEvent()
    {
      this.ResetSiegeState();
      this.SiegeEvent = (SiegeEvent) null;
      this.Party.Visuals?.OnSiegeLifted();
      this.Party.Visuals?.RefreshLevelMask(this.Party);
      this.Party.Visuals?.SetMapIconAsDirty();
    }

    bool IMapEntity.IsMobileEntity => false;

    IMapEntity IMapEntity.AttachedEntity => (IMapEntity) null;

    IPartyVisual IMapEntity.PartyVisual => this.Party.Visuals;

    bool IMapEntity.ShowCircleAroundEntity => true;

    Vec2 IMapEntity.InteractionPosition => this.GatePosition;

    bool IMapEntity.OnMapClick(bool followModifierUsed)
    {
      if (!this.IsVisible)
        return false;
      MobileParty.MainParty.SetMoveGoToSettlement(this);
      return true;
    }

    void IMapEntity.OnOpenEncyclopedia() => Campaign.Current.EncyclopediaManager.GoToLink(this.EncyclopediaLink);

    bool IMapEntity.IsMainEntity() => false;

    void IMapEntity.OnHover() => InformationManager.AddTooltipInformation(typeof (Settlement), (object) this, (object) false);

    bool IMapEntity.IsEnemyOf(IFaction faction) => FactionManager.IsAtWarAgainstFaction(this.MapFaction, faction);

    bool IMapEntity.IsAllyOf(IFaction faction) => FactionManager.IsAlliedWithFaction(this.MapFaction, faction);

    public void GetMountAndHarnessVisualIdsForPartyIcon(
      out string mountStringId,
      out string harnessStringId)
    {
      mountStringId = "";
      harnessStringId = "";
    }

    public class SiegeLane
    {
      public Settlement.SiegeLaneEnum Id;
      //[SaveableField(1)]
      public bool IsGate;
      //[SaveableField(2)]
      public bool IsSiegeMachineApplicable;
      //[SaveableField(3)]
      public bool IsBroken;

      internal static void AutoGeneratedStaticCollectObjectsSiegeLane(
        object o,
        List<object> collectedObjects)
      {
        ((Settlement.SiegeLane) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
      }

      protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
      {
      }

      internal static object AutoGeneratedGetMemberValueIsGate(object o) => (object) ((Settlement.SiegeLane) o).IsGate;

      internal static object AutoGeneratedGetMemberValueIsSiegeMachineApplicable(object o) => (object) ((Settlement.SiegeLane) o).IsSiegeMachineApplicable;

      internal static object AutoGeneratedGetMemberValueIsBroken(object o) => (object) ((Settlement.SiegeLane) o).IsBroken;

      public SiegeLane(Settlement.SiegeLaneEnum id, bool isGate, bool isSiegeMachineApplicable)
      {
        this.Id = id;
        this.IsGate = isGate;
        this.IsSiegeMachineApplicable = isSiegeMachineApplicable;
        this.IsBroken = false;
      }
    }

    public struct SettlementEventArguments
    {
      //[SaveableField(0)]
      public Settlement TargetSettlement;

      public static void AutoGeneratedStaticCollectObjectsSettlementEventArguments(
        object o,
        List<object> collectedObjects)
      {
        ((Settlement.SettlementEventArguments) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
      }

      private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects) => collectedObjects.Add((object) this.TargetSettlement);

      internal static object AutoGeneratedGetMemberValueTargetSettlement(object o) => (object) ((Settlement.SettlementEventArguments) o).TargetSettlement;

      public SettlementEventArguments(Settlement targetSettlement) => this.TargetSettlement = targetSettlement;
    }

    public enum EventType
    {
      Undefined = -1, // 0xFFFFFFFF
      PlayerEntered = 0,
      PlayerLeft = 1,
    }

    public enum ManagementAIState
    {
      ConstructBuilding,
      MaximizeProduction,
      GiveQuest,
    }

    public enum SiegeLaneEnum
    {
      Left,
      Middle,
      Right,
    }

    public enum SiegeState
    {
      OnTheWalls,
      InTheLordsHall,
      Invalid,
    }
  }
}