using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.Overhaul.CampaignSystem
{
  public sealed class Kingdom : MBObjectBase, IFaction
  {
    //[SaveableField(10)]
    private List<KingdomDecision> _unresolvedDecisions = new List<KingdomDecision>();
    //[CachedData]
    private List<StanceLink> _stances = new List<StanceLink>();
    //[CachedData]
    private List<Town> _fiefsCache;
    //[CachedData]
    private MBReadOnlyList<Town> _fiefsReadOnlyCache;
    //[CachedData]
    private List<Village> _villagesCache;
    //[CachedData]
    private MBReadOnlyList<Village> _villagesReadOnlyCache;
    //[CachedData]
    private List<Settlement> _settlementsCache;
    //[CachedData]
    private MBReadOnlyList<Settlement> _settlementsReadOnlyCache;
    //[CachedData]
    private bool _partiesAndLordsCacheIsDirty;
    //[CachedData]
    private List<Hero> _heroesCache;
    //[CachedData]
    private MBReadOnlyList<Hero> _heroesReadOnlyCache;
    //[CachedData]
    private List<Hero> _lordsCache;
    //[CachedData]
    private List<WarPartyComponent> _warPartyComponentsCache;
    //[CachedData]
    private MBReadOnlyList<WarPartyComponent> _warPartiesReadOnlyCache;
    //[CachedData]
    private MBReadOnlyList<Hero> _lordsReadOnlyCache;
    //[CachedData]
    private List<Clan> _clans;
    //[SaveableField(18)]
    private Clan _rulingClan;
    //[SaveableField(20)]
    private readonly List<Army> _armies;
    private Vec2 _oldInitialPointVariable;
    //[SaveableField(23)]
    public int PoliticalStagnation;
    //[SaveableField(26)]
    private List<PolicyObject> _activePolicies;
    //[SaveableField(27)]
    public List<Kingdom.Provocation> Provocations;
    //[SaveableField(29)]
    private bool _isEliminated;
    //[SaveableField(60)]
    private float _aggressiveness;
    //[CachedData]
    private Vec2 _kingdomMidPoint;
    //[SaveableField(80)]
    private int _tributeWallet;
    //[SaveableField(81)]
    private int _kingdomBudgetWallet;

    //[SaveableProperty(1)]
    public TextObject Name { get; private set; }

    //[SaveableProperty(2)]
    public TextObject InformalName { get; private set; }

    //[SaveableProperty(3)]
    public TextObject EncyclopediaText { get; private set; }

    //[SaveableProperty(4)]
    public TextObject EncyclopediaTitle { get; private set; }

    //[SaveableProperty(5)]
    public TextObject EncyclopediaRulerTitle { get; private set; }

    public string EncyclopediaLink => Campaign.Current.EncyclopediaManager.GetIdentifier(typeof (Kingdom)) + "-" + this.StringId;

    public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetKingdomHyperlinkText(this.EncyclopediaLink, this.InformalName);

    public IReadOnlyList<KingdomDecision> UnresolvedDecisions => (IReadOnlyList<KingdomDecision>) this._unresolvedDecisions;

    protected override void AfterLoad()
    {
      if (this._unresolvedDecisions == null)
        this._unresolvedDecisions = new List<KingdomDecision>();
      if (this.InitialHomeLand == null)
      {
        if (this._oldInitialPointVariable != new Vec2() && this._oldInitialPointVariable.IsValid)
        {
          this.InitialHomeLand = SettlementHelper.FindNearestSettlementToPoint(this._oldInitialPointVariable);
        }
        else
        {
          this.InitialHomeLand = Settlement.All.GetRandomElementWithPredicate<Settlement>((Func<Settlement, bool>) (x => x.IsTown && x.MapFaction == this.MapFaction));
          if (this.InitialHomeLand == null)
            this.InitialHomeLand = Settlement.All.GetRandomElementWithPredicate<Settlement>((Func<Settlement, bool>) (x => x.IsTown));
        }
      }
      foreach (Clan clan in (IEnumerable<Clan>) this.Clans)
        clan.OnPartiesAndLordsCacheUpdated = new Action(this.OnClanPartiesAndLordsCacheUpdated);
      if (!(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.6.3", ApplicationVersionGameType.Singleplayer)))
        return;
      if (this.EncyclopediaTitle == null || this.EncyclopediaTitle.Equals(TextObject.Empty))
      {
        this.EncyclopediaTitle = new TextObject("{=ZOEamqUd}Kingdom of {NAME}");
        this.EncyclopediaTitle.SetTextVariable("NAME", this.Ruler.Clan.Name);
      }
      if (this.EncyclopediaRulerTitle != null && !this.EncyclopediaRulerTitle.Equals(TextObject.Empty))
        return;
      Kingdom kingdom = Kingdom.All.FirstOrDefault<Kingdom>((Func<Kingdom, bool>) (x => x.Culture == this.Culture && x.EncyclopediaRulerTitle != null));
      this.EncyclopediaRulerTitle = kingdom != null ? kingdom.EncyclopediaRulerTitle : TextObject.Empty;
    }

    protected override void OnBeforeLoad()
    {
      if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.6.0", ApplicationVersionGameType.Singleplayer))
      {
        base.OnBeforeLoad();
      }
      else
      {
        this.OnUnregistered();
        this.IsReady = true;
      }
    }

    public void AddDecision(KingdomDecision kingdomDecision, bool ignoreInfluenceCost = false)
    {
      if (!ignoreInfluenceCost)
      {
        Clan proposerClan = kingdomDecision.ProposerClan;
        int influenceCost = kingdomDecision.GetInfluenceCost(proposerClan);
        proposerClan.Influence -= (float) influenceCost;
      }
      bool isPlayerInvolved = kingdomDecision.DetermineChooser().Leader.IsHumanPlayerCharacter || kingdomDecision.DetermineSupporters().Any<Supporter>((Func<Supporter, bool>) (x => x.IsPlayer));
      CampaignEventDispatcher.Instance.OnKingdomDecisionAdded(kingdomDecision, isPlayerInvolved);
      if (kingdomDecision.Kingdom != Clan.PlayerClan.Kingdom)
        new KingdomElection(kingdomDecision).StartElection();
      else
        this._unresolvedDecisions.Add(kingdomDecision);
    }

    public void RemoveDecision(KingdomDecision kingdomDecision) => this._unresolvedDecisions.Remove(kingdomDecision);

    //[SaveableProperty(6)]
    public CultureObject Culture { get; private set; }

    //[SaveableProperty(17)]
    public Settlement InitialHomeLand { get; private set; }

    public Vec2 InitialPosition => this.InitialHomeLand.GatePosition;

    public bool IsMapFaction => true;

    //[SaveableProperty(8)]
    public uint LabelColor { get; private set; }

    //[SaveableProperty(9)]
    public uint Color { get; private set; }

    //[SaveableProperty(10)]
    public uint Color2 { get; private set; }

    //[SaveableProperty(11)]
    public uint AlternativeColor { get; private set; }

    //[SaveableProperty(12)]
    public uint AlternativeColor2 { get; private set; }

    //[SaveableProperty(13)]
    public uint PrimaryBannerColor { get; private set; }

    //[SaveableProperty(14)]
    public uint SecondaryBannerColor { get; private set; }

    //[SaveableProperty(15)]
    public float MainHeroCrimeRating { get; set; }

    public IEnumerable<StanceLink> Stances => (IEnumerable<StanceLink>) this._stances;

    public MBReadOnlyList<Town> Fiefs
    {
      get
      {
        if (this._partiesAndLordsCacheIsDirty)
          this.CollectPartiesAndLordsToCache();
        return this._fiefsReadOnlyCache;
      }
      private set => this._fiefsReadOnlyCache = value;
    }

    internal void AddStanceInternal(StanceLink stanceLink) => this._stances.Add(stanceLink);

    public MBReadOnlyList<Hero> Lords
    {
      get
      {
        if (this._partiesAndLordsCacheIsDirty)
          this.CollectPartiesAndLordsToCache();
        return this._lordsReadOnlyCache;
      }
      private set => this._lordsReadOnlyCache = value;
    }

    public MBReadOnlyList<Hero> Heroes
    {
      get
      {
        if (this._partiesAndLordsCacheIsDirty)
          this.CollectPartiesAndLordsToCache();
        return this._heroesReadOnlyCache;
      }
      private set => this._heroesReadOnlyCache = value;
    }

    public MBReadOnlyList<Village> Villages
    {
      get
      {
        if (this._partiesAndLordsCacheIsDirty)
          this.CollectPartiesAndLordsToCache();
        return this._villagesReadOnlyCache;
      }
      private set => this._villagesReadOnlyCache = value;
    }

    public MBReadOnlyList<Settlement> Settlements
    {
      get
      {
        if (this._partiesAndLordsCacheIsDirty)
          this.CollectPartiesAndLordsToCache();
        return this._settlementsReadOnlyCache;
      }
      private set => this._settlementsReadOnlyCache = value;
    }

    internal void RemoveStanceInternal(StanceLink stanceLink) => this._stances.Remove(stanceLink);

    public MBReadOnlyList<WarPartyComponent> WarPartyComponents
    {
      get
      {
        if (this._partiesAndLordsCacheIsDirty)
          this.CollectPartiesAndLordsToCache();
        return this._warPartiesReadOnlyCache;
      }
      private set => this._warPartiesReadOnlyCache = value;
    }

    public float DailyCrimeRatingChange => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange((IFaction) this).ResultNumber;

    public ExplainedNumber DailyCrimeRatingChangeExplained => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange((IFaction) this, true);

    public CharacterObject BasicTroop => this.Culture.BasicTroop;

    public Hero Leader => this._rulingClan?.Leader;

    //[SaveableProperty(16)]
    public Banner Banner { get; private set; }

    public bool IsBanditFaction => false;

    public bool IsMinorFaction => false;

    public bool IsKingdomFaction => true;

    public bool IsRebelClan => false;

    public bool IsClan => false;

    public bool IsOutlaw => false;

    public IReadOnlyList<Clan> Clans => (IReadOnlyList<Clan>) this._clans;

    public Clan RulingClan
    {
      get => this._rulingClan;
      set => this._rulingClan = value;
    }

    //[SaveableProperty(19)]
    public int LastArmyCreationDay { get; private set; }

    public MBReadOnlyList<Army> Armies { get; private set; }

    public override string ToString() => this.Name.ToString();

    public float TotalStrength
    {
      get
      {
        float num = 0.0f;
        int count = this._clans.Count;
        for (int index = 0; index < count; ++index)
          num += this._clans[index].TotalStrength;
        return num;
      }
    }

    //[CachedData]
    internal bool _midPointCalculated { get; set; }

    public bool IsAtWarWith(IFaction other) => FactionManager.IsAtWarAgainstFaction((IFaction) this, other);

    public IList<PolicyObject> ActivePolicies => (IList<PolicyObject>) this._activePolicies;

    public static MBReadOnlyList<Kingdom> All => Campaign.Current.Kingdoms;

    //[SaveableProperty(28)]
    public CampaignTime LastKingdomDecisionConclusionDate { get; private set; }

    public bool IsEliminated => this._isEliminated;

    public Hero Ruler => this.Leader;

    //[SaveableProperty(41)]
    public CampaignTime LastMercenaryOfferTime { get; set; }

    public IFaction MapFaction => (IFaction) this;

    //[SaveableProperty(50)]
    public CampaignTime NotAttackableByPlayerUntilTime { get; set; }

    public float Aggressiveness
    {
      get => this._aggressiveness;
      internal set => this._aggressiveness = MathF.Clamp(value, 0.0f, 100f);
    }

    public IEnumerable<MobileParty> AllParties
    {
      get
      {
        Kingdom kingdom = this;
        foreach (MobileParty mobileParty in Campaign.Current.MobileParties)
        {
          if (mobileParty.MapFaction == kingdom)
            yield return mobileParty;
        }
      }
    }

    public Vec2 FactionMidPoint
    {
      get
      {
        if (!this._midPointCalculated)
          this.UpdateFactionMidPoint();
        return this._kingdomMidPoint;
      }
    }

    //[SaveableProperty(70)]
    public int MercenaryWallet { get; internal set; }

    public int TributeWallet
    {
      get => this._tributeWallet;
      set => this._tributeWallet = value;
    }

    public int KingdomBudgetWallet
    {
      get => this._kingdomBudgetWallet;
      set => this._kingdomBudgetWallet = value;
    }

    public static Kingdom CreateKingdom(string stringID)
    {
      stringID = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Kingdom>(stringID);
      Kingdom kingdom = new Kingdom();
      kingdom.StringId = stringID;
      Campaign.Current.CampaignObjectManager.AddKingdom(kingdom);
      return kingdom;
    }

    public Kingdom()
    {
      this._armies = new List<Army>();
      this.Armies = new MBReadOnlyList<Army>(this._armies);
      this._clans = new List<Clan>();
      this._fiefsCache = new List<Town>();
      this._villagesCache = new List<Village>();
      this._settlementsCache = new List<Settlement>();
      this._heroesCache = new List<Hero>();
      this._lordsCache = new List<Hero>();
      this._warPartyComponentsCache = new List<WarPartyComponent>();
      this.Fiefs = new MBReadOnlyList<Town>(this._fiefsCache);
      this.Villages = new MBReadOnlyList<Village>(this._villagesCache);
      this.Settlements = new MBReadOnlyList<Settlement>(this._settlementsCache);
      this.Heroes = new MBReadOnlyList<Hero>(this._heroesCache);
      this.Lords = new MBReadOnlyList<Hero>(this._lordsCache);
      this.WarPartyComponents = new MBReadOnlyList<WarPartyComponent>(this._warPartyComponentsCache);
      this._partiesAndLordsCacheIsDirty = true;
      this._activePolicies = new List<PolicyObject>();
      this.Provocations = new List<Kingdom.Provocation>();
      this.EncyclopediaText = TextObject.Empty;
      this.EncyclopediaTitle = TextObject.Empty;
      this.EncyclopediaRulerTitle = TextObject.Empty;
      CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener((object) this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
      this.LastArmyCreationDay = (int) CampaignTime.Now.ToDays;
      this.PoliticalStagnation = 10 + (int) ((double) MBRandom.RandomFloat * (double) MBRandom.RandomFloat * 100.0);
      this._midPointCalculated = false;
      this._isEliminated = false;
      this.NotAttackableByPlayerUntilTime = CampaignTime.Zero;
    }

    public void InitializeKingdom(
      TextObject name,
      TextObject informalName,
      CultureObject culture,
      Banner banner,
      uint kingdomColor1,
      uint kingdomColor2,
      Settlement initialHomeland,
      TextObject encyclopediaText,
      TextObject encyclopediaTitle,
      TextObject encyclopediaRulerTitle)
    {
      this.ChangeKingdomName(name, informalName);
      this.Culture = culture;
      this.Banner = banner;
      this.Color = kingdomColor1;
      this.Color2 = kingdomColor2;
      this.PrimaryBannerColor = this.Color;
      this.SecondaryBannerColor = this.Color2;
      this.InitialHomeLand = initialHomeland;
      this.PoliticalStagnation = 100;
      this.EncyclopediaText = encyclopediaText;
      this.EncyclopediaTitle = encyclopediaTitle;
      this.EncyclopediaRulerTitle = encyclopediaRulerTitle;
      foreach (PolicyObject defaultPolicy in (IEnumerable<PolicyObject>) this.Culture.DefaultPolicyList)
        this.AddPolicy(defaultPolicy);
    }

    public void ChangeKingdomName(TextObject name, TextObject informalName)
    {
      this.Name = name;
      this.InformalName = informalName;
    }

    public void OnNewGameCreated(CampaignGameStarter starter) => this.InitialHomeLand = this.Leader.HomeSettlement;

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    {
      this._clans = new List<Clan>();
      this._fiefsCache = new List<Town>();
      this._villagesCache = new List<Village>();
      this._settlementsCache = new List<Settlement>();
      this._heroesCache = new List<Hero>();
      this._lordsCache = new List<Hero>();
      this._warPartyComponentsCache = new List<WarPartyComponent>();
      this.Armies = new MBReadOnlyList<Army>(this._armies);
      this.Fiefs = new MBReadOnlyList<Town>(this._fiefsCache);
      this.Villages = new MBReadOnlyList<Village>(this._villagesCache);
      this.Settlements = new MBReadOnlyList<Settlement>(this._settlementsCache);
      this.Heroes = new MBReadOnlyList<Hero>(this._heroesCache);
      this.Lords = new MBReadOnlyList<Hero>(this._lordsCache);
      this.WarPartyComponents = new MBReadOnlyList<WarPartyComponent>(this._warPartyComponentsCache);
      this._partiesAndLordsCacheIsDirty = true;
      this._stances = new List<StanceLink>();
      this._oldInitialPointVariable = objectLoadData.GetDataBySaveId(7) is Vec2 ? (Vec2) objectLoadData.GetDataBySaveId(7) : new Vec2();
      this.CollectPartiesAndLordsToCache();
    }

    internal void ConsiderSiegesAndMapEvents(IFaction factionToConsiderAgainst)
    {
      foreach (Clan clan in (IEnumerable<Clan>) this.Clans)
        clan.ConsiderSiegesAndMapEvents(factionToConsiderAgainst);
    }

    public bool HasPolicy(PolicyObject policy)
    {
      for (int index = 0; index < this._activePolicies.Count; ++index)
      {
        if (this._activePolicies[index] == policy)
          return true;
      }
      return false;
    }

    internal void RemoveArmyInternal(Army army) => this._armies.Remove(army);

    internal void AddArmyInternal(Army army) => this._armies.Add(army);

    public void CreateArmy(Hero armyLeader, IMapPoint target, Army.ArmyTypes selectedArmyType)
    {
      if (!armyLeader.IsActive)
      {
        Debug.Print("Failed to create army, leader - " + (object) armyLeader?.Name + " is inactive");
      }
      else
      {
        Army.ArmyTypes armyType = selectedArmyType;
        if (armyLeader?.PartyBelongedTo.LeaderHero != null)
        {
          Army army = new Army(this, armyLeader.PartyBelongedTo, armyType, target)
          {
            AIBehavior = Army.AIBehaviorFlags.Gathering
          };
          army.Gather();
          this.LastArmyCreationDay = (int) CampaignTime.Now.ToDays;
          CampaignEventDispatcher.Instance.OnArmyCreated(army);
        }
        if (armyLeader != Hero.MainHero || !(Game.Current.GameStateManager.GameStates.Single<GameState>((Func<GameState, bool>) (S => S is MapState)) is MapState mapState2))
          return;
        mapState2.OnArmyCreated(MobileParty.MainParty);
      }
    }

    public bool HasDefenderArmyForTown(Settlement siegedSettlement)
    {
      foreach (Army army in this.Armies)
      {
        if (army.ArmyType == Army.ArmyTypes.Defender && army.NextAiBehaviorObject == siegedSettlement)
          return true;
      }
      return false;
    }

    public bool HasDefenderArmyThatHasntArrivedYet(
      Settlement besiegedSettlement,
      Army otherThanThis = null)
    {
      foreach (Army army in this.Armies)
      {
        if (army != otherThanThis && army.ArmyType == Army.ArmyTypes.Defender && army.NextAiBehaviorObject == besiegedSettlement && (army.AIBehavior == Army.AIBehaviorFlags.Gathering || army.AIBehavior == Army.AIBehaviorFlags.Waiting || army.AIBehavior == Army.AIBehaviorFlags.TravellingToAssignment))
          return true;
      }
      return false;
    }

    private void UpdateFactionMidPoint()
    {
      this._kingdomMidPoint = FactionHelper.FactionMidPoint((IFaction) this);
      this._midPointCalculated = true;
    }

    public void AddProvocation(Hero actor, Kingdom.ProvocationType provocationType) => this.Provocations.Add(new Kingdom.Provocation()
    {
      actor = actor,
      provocationTime = CampaignTime.Now,
      provocationType = provocationType,
      provocatorFaction = (Kingdom) actor.MapFaction
    });

    public void OnKingdomDecisionConcluded() => this.LastKingdomDecisionConclusionDate = CampaignTime.Now;

    public void AddPolicy(PolicyObject policy)
    {
      if (this._activePolicies.Contains(policy))
        return;
      this._activePolicies.Add(policy);
    }

    public void RemovePolicy(PolicyObject policy)
    {
      if (!this._activePolicies.Contains(policy))
        return;
      this._activePolicies.Remove(policy);
    }

    public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    {
      base.Deserialize(objectManager, node);
      this.EncyclopediaText = node.Attributes["text"] != null ? new TextObject(node.Attributes["text"].Value) : TextObject.Empty;
      this.EncyclopediaTitle = node.Attributes["title"] != null ? new TextObject(node.Attributes["title"].Value) : TextObject.Empty;
      this.EncyclopediaRulerTitle = node.Attributes["ruler_title"] != null ? new TextObject(node.Attributes["ruler_title"].Value) : TextObject.Empty;
      this.InitializeKingdom(new TextObject(node.Attributes["name"].Value), node.Attributes["short_name"] != null ? new TextObject(node.Attributes["short_name"].Value) : new TextObject(node.Attributes["name"].Value), (CultureObject) objectManager.ReadObjectReferenceFromXml("culture", typeof (CultureObject), node), (Banner) null, node.Attributes["color"] == null ? 0U : Convert.ToUInt32(node.Attributes["color"].Value, 16), node.Attributes["color2"] == null ? 0U : Convert.ToUInt32(node.Attributes["color2"].Value, 16), (Settlement) null, this.EncyclopediaText, this.EncyclopediaTitle, this.EncyclopediaRulerTitle);
      this.RulingClan = objectManager.ReadObjectReferenceFromXml("owner", typeof (Hero), node) is Hero hero ? hero.Clan : (Clan) null;
      this.LabelColor = node.Attributes["label_color"] == null ? 0U : Convert.ToUInt32(node.Attributes["label_color"].Value, 16);
      this.AlternativeColor = node.Attributes["alternative_color"] == null ? 0U : Convert.ToUInt32(node.Attributes["alternative_color"].Value, 16);
      this.AlternativeColor2 = node.Attributes["alternative_color2"] == null ? 0U : Convert.ToUInt32(node.Attributes["alternative_color2"].Value, 16);
      this.PrimaryBannerColor = node.Attributes["primary_banner_color"] == null ? 0U : Convert.ToUInt32(node.Attributes["primary_banner_color"].Value, 16);
      this.SecondaryBannerColor = node.Attributes["secondary_banner_color"] == null ? 0U : Convert.ToUInt32(node.Attributes["secondary_banner_color"].Value, 16);
      if (node.Attributes["banner_key"] != null)
      {
        this.Banner = new Banner();
        this.Banner.Deserialize(node.Attributes["banner_key"].Value);
      }
      else
        this.Banner = Banner.CreateRandomClanBanner(this.StringId.GetDeterministicHashCode());
      foreach (XmlNode childNode1 in node.ChildNodes)
      {
        if (childNode1.Name == "relationships")
        {
          foreach (XmlNode childNode2 in childNode1.ChildNodes)
          {
            IFaction faction2 = childNode2.Attributes["clan"] == null ? (IFaction) objectManager.ReadObjectReferenceFromXml("kingdom", typeof (Kingdom), childNode2) : (IFaction) objectManager.ReadObjectReferenceFromXml("clan", typeof (Clan), childNode2);
            int int32 = Convert.ToInt32(childNode2.Attributes["value"].InnerText);
            if (int32 > 0)
              FactionManager.DeclareAlliance((IFaction) this, faction2);
            else if (int32 < 0)
              FactionManager.DeclareWar((IFaction) this, faction2);
            else
              FactionManager.SetNeutral((IFaction) this, faction2);
            if (childNode2.Attributes["isAtWar"] != null && Convert.ToBoolean(childNode2.Attributes["isAtWar"].Value))
              FactionManager.DeclareWar((IFaction) this, faction2);
          }
        }
        else if (childNode1.Name == "policies")
        {
          foreach (XmlNode childNode3 in childNode1.ChildNodes)
          {
            PolicyObject policy = Game.Current.ObjectManager.GetObject<PolicyObject>(childNode3.Attributes["id"].Value);
            if (policy != null)
              this.AddPolicy(policy);
          }
        }
      }
    }

    internal void RemoveClanInternal(Clan clan)
    {
      this._clans.Remove(clan);
      this._midPointCalculated = false;
      clan.OnPartiesAndLordsCacheUpdated -= new Action(this.OnClanPartiesAndLordsCacheUpdated);
      this._partiesAndLordsCacheIsDirty = true;
    }

    internal void AddClanInternal(Clan clan)
    {
      this._clans.Add(clan);
      this._midPointCalculated = false;
      clan.OnPartiesAndLordsCacheUpdated += new Action(this.OnClanPartiesAndLordsCacheUpdated);
      this._partiesAndLordsCacheIsDirty = true;
    }

    private void CollectPartiesAndLordsToCache()
    {
      this._fiefsCache.Clear();
      this._villagesCache.Clear();
      this._settlementsCache.Clear();
      this._heroesCache.Clear();
      this._lordsCache.Clear();
      this._warPartyComponentsCache.Clear();
      foreach (Clan clan in this._clans)
      {
        this._fiefsCache.AddRange((IEnumerable<Town>) clan.Fiefs);
        this._villagesCache.AddRange((IEnumerable<Village>) clan.Villages);
        this._settlementsCache.AddRange((IEnumerable<Settlement>) clan.Settlements);
        this._heroesCache.AddRange((IEnumerable<Hero>) clan.Heroes);
        this._lordsCache.AddRange((IEnumerable<Hero>) clan.Lords);
        this._warPartyComponentsCache.AddRange((IEnumerable<WarPartyComponent>) clan.WarPartyComponents);
      }
      this._partiesAndLordsCacheIsDirty = false;
    }

    private void OnClanPartiesAndLordsCacheUpdated() => this._partiesAndLordsCacheIsDirty = true;

    public void ReactivateKingdom() => this._isEliminated = false;

    internal void DeactivateKingdom() => this._isEliminated = true;

    public StanceLink GetStanceWith(IFaction other) => FactionManager.Instance.GetStanceLinkInternal((IFaction) this, other);

    [SpecialName]
    string IFaction.get_StringId() => this.StringId;

    [SpecialName]
    MBGUID IFaction.get_Id() => this.Id;

    public struct Provocation
    {
      //[SaveableField(124)]
      public CampaignTime provocationTime;
      //[SaveableField(125)]
      public Hero actor;
      //[SaveableField(126)]
      public Kingdom provocatorFaction;
      //[SaveableField(127)]
      public Kingdom.ProvocationType provocationType;
    }

    public enum ProvocationType
    {
      AttackLord,
      AttackVillager,
      AttackCaravan,
      RaidVillage,
      BesiegeTown,
      BorderPass,
    }
  }
}