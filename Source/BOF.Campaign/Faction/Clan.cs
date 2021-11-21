using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using BOF.Campaign.Party;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace BOF.Campaign.Faction
{
  public sealed class Clan : MBObjectBase, IFaction
  {
    private float _aggressiveness;

    private Banner _banner;

    private CharacterObject _basicTroop;

    private List<CharacterObject> _characterTemplates;

    private int _clanDebtToKingdom;

    [CachedData]
    private Vec2 _clanMidPoint;

    [CachedData]
    private List<Hero> _companionsCache;

    private PartyTemplateObject _defaultPartyTemplate;

    [CachedData]
    private List<Town> _fiefsCache;

    [CachedData]
    private List<Hero> _heroesCache;

    private Settlement _home;

    private float _influence;

    private bool _isEliminated;

    private Kingdom _kingdom;

    private Hero _leader;

    [CachedData]
    private List<Hero> _lordsCache;

    [CachedData]
    private List<Settlement> _settlementsCache;

    [CachedData]
    private MBReadOnlyList<Settlement> _settlementsReadOnlyCache;

    [CachedData]
    private List<StanceLink> _stances;

    [CachedData]
    private List<Hero> _supporterNotables;

    private int _tier;

    private int _tributeWallet;

    [CachedData]
    private bool _villagesAndSettlementsCacheIsDirty;

    [CachedData]
    private List<Village> _villagesCache;

    [CachedData]
    private MBReadOnlyList<Village> _villagesReadOnlyCache;

    [CachedData]
    private List<WarPartyComponent> _warPartyComponentsCache;

    [CachedData]
    public Action OnPartiesAndLordsCacheUpdated;

    public Clan()
    {
      this.InitMembers();
      this._isEliminated = false;
      this.NotAttackableByPlayerUntilTime = CampaignTime.Zero;
    }

    public TextObject FullName
    {
      get
      {
        TextObject empty = TextObject.Empty;
        int minClanTier = Campaign.Current.Models.ClanTierModel.MinClanTier;
        TextObject textObject = minClanTier < this.Tier ? (minClanTier + 1 != this.Tier ? (minClanTier + 2 != this.Tier ? new TextObject("{=iOrtiVLt}House of {CLAN_NAME}") : new TextObject("{=UnjmmLFm}{CLAN_NAME} Noble Clan")) : new TextObject("{=ztXDDMSI}{CLAN_NAME} Clan")) : new TextObject("{=aZne44IB}{CLAN_NAME} Family");
        textObject.SetTextVariable("CLAN_NAME", this.Name);
        return textObject;
      }
    }


    public CampaignTime LastFactionChangeTime { get; set; }

    public PartyTemplateObject DefaultPartyTemplate => this._defaultPartyTemplate != null ? this._defaultPartyTemplate : this.Culture.DefaultPartyTemplate;


    public int AutoRecruitmentExpenses { get; set; }

    public IList<CharacterObject> TemplateCharacterList => (IList<CharacterObject>) this._characterTemplates;

    public MBReadOnlyList<Village> Villages
    {
      get
      {
        if (this._villagesAndSettlementsCacheIsDirty)
          this.CollectSettlementsAndVillagesToCache();
        return this._villagesReadOnlyCache;
      }
      private set => this._villagesReadOnlyCache = value;
    }

    public bool IsNeutralClan => this == CampaignData.NeutralFaction;

    public Kingdom Kingdom
    {
      get => this._kingdom;
      set
      {
        if (this._kingdom == value)
          return;
        this.SetKingdomInternal(value);
      }
    }

    public IEnumerable<CharacterObject> DungeonPrisonersOfClan
    {
      get
      {
        foreach (SettlementComponent fief in this.Fiefs)
        {
          foreach (CharacterObject prisonerHero in fief.Settlement.Party.PrisonerHeroes)
            yield return prisonerHero;
        }
      }
    }

    public IReadOnlyList<Hero> SupporterNotables => (IReadOnlyList<Hero>) this._supporterNotables;

    [CachedData]
    public MBReadOnlyList<Hero> Companions { get; private set; }

    public float Influence
    {
      get => this._influence;
      set
      {
        if ((double) value < (double) this._influence && this.Leader != null)
          SkillLevelingManager.OnInfluenceSpent(this.Leader, value - this._influence);
        this._influence = value;
      }
    }

    public ExplainedNumber InfluenceChangeExplained => Campaign.Current.Models.ClanPoliticsModel.CalculateInfluenceChange(this, true);


    public int MercenaryAwardMultiplier { get; set; }


    public bool IsNomad { get; private set; }


    public bool IsMafia { get; private set; }


    public bool IsClanTypeMercenary { get; private set; }


    public bool IsSect { get; private set; }


    public bool IsUnderMercenaryService { get; private set; }


    private uint BannerBackgroundColorPrimary { get; set; }


    private uint BannerBackgroundColorSecondary { get; set; }


    private uint BannerIconColor { get; set; }

    [CachedData]
    private bool _midPointCalculated { get; set; }

    public static Clan PlayerClan => Campaign.Current.PlayerDefaultFaction;


    public float Renown { get; set; }

    public int Tier
    {
      get => this._tier;
      private set
      {
        int minClanTier = Campaign.Current.Models.ClanTierModel.MinClanTier;
        int maxClanTier = Campaign.Current.Models.ClanTierModel.MaxClanTier;
        if (value > maxClanTier)
          value = maxClanTier;
        else if (value < minClanTier)
          value = minClanTier;
        this._tier = value;
      }
    }

    public Settlement HomeSettlement
    {
      get => this._home;
      private set => this._home = value;
    }

    public int DebtToKingdom
    {
      get => this._clanDebtToKingdom;
      set => this._clanDebtToKingdom = value;
    }

    public int RenownRequirementForNextTier => Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(this.Tier + 1);

    public int CompanionLimit => Campaign.Current.Models.ClanTierModel.GetCompanionLimit(this);

    public int CommanderLimit => Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(this, this.Tier);

    public int Gold
    {
      get
      {
        Hero leader = this.Leader;
        return leader == null ? 0 : leader.Gold;
      }
    }

    public static MBReadOnlyList<Clan> All => Campaign.Current.Clans;

    public static IEnumerable<Clan> NonBanditFactions
    {
      get
      {
        foreach (Clan clan in Campaign.Current.Clans)
        {
          if (!clan.IsBanditFaction && CampaignData.NeutralFaction != clan)
            yield return clan;
        }
      }
    }

    public static IEnumerable<Clan> BanditFactions
    {
      get
      {
        foreach (Clan clan in Campaign.Current.Clans)
        {
          if (clan.IsBanditFaction)
            yield return clan;
        }
      }
    }

    public MBReadOnlyList<WarPartyComponent> WarPartyComponents { get; private set; }


    public CampaignTime NotAttackableByPlayerUntilTime { get; set; }


    public TextObject Name { get; private set; }


    public TextObject InformalName { get; set; }


    public CultureObject Culture { get; set; }


    public TextObject EncyclopediaText { get; private set; }

    public bool IsEliminated => this._isEliminated;

    public MBReadOnlyList<Town> Fiefs { get; private set; }

    public MBReadOnlyList<Settlement> Settlements
    {
      get
      {
        if (this._villagesAndSettlementsCacheIsDirty)
          this.CollectSettlementsAndVillagesToCache();
        return this._settlementsReadOnlyCache;
      }
      private set => this._settlementsReadOnlyCache = value;
    }

    public string EncyclopediaLink => Campaign.Current.EncyclopediaManager.GetIdentifier(typeof (Clan)) + "-" + this.StringId;

    public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetClanHyperlinkText(this.EncyclopediaLink, this.Name);

    [CachedData]
    public MBReadOnlyList<Hero> Lords { get; private set; }

    [CachedData]
    public MBReadOnlyList<Hero> Heroes { get; private set; }

    [CachedData]
    public float TotalStrength { get; private set; }

    public bool IsMapFaction => this._kingdom == null;


    public uint LabelColor { get; set; }


    public Vec2 InitialPosition { get; set; }


    public bool IsRebelClan { get; set; }


    public bool IsMinorFaction { get; private set; }


    public bool IsOutlaw { get; private set; }


    public uint Color { get; set; }


    public uint Color2 { get; set; }


    public uint AlternativeColor { get; set; }


    public uint AlternativeColor2 { get; set; }

    public CharacterObject BasicTroop
    {
      get => this._basicTroop ?? this.Culture.BasicTroop;
      set => this._basicTroop = value;
    }

    public Hero Leader => this._leader;

    public Banner Banner
    {
      get => this.Kingdom == null || this.Kingdom.RulingClan != this ? this._banner : this.Kingdom.Banner;
      private set => this._banner = value;
    }


    public bool IsBanditFaction { get; private set; }

    public bool IsKingdomFaction => false;

    public bool IsClan => true;


    public float MainHeroCrimeRating { get; set; }

    public float DailyCrimeRatingChange => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange((IFaction) this).ResultNumber;

    public ExplainedNumber DailyCrimeRatingChangeExplained => Campaign.Current.Models.CrimeModel.GetDailyCrimeRatingChange((IFaction) this, true);

    public bool IsAtWarWith(IFaction other) => FactionManager.IsAtWarAgainstFaction((IFaction) this, other);

    public Vec2 FactionMidPoint
    {
      get
      {
        if (!this._midPointCalculated)
        {
          this.UpdateFactionMidPoint();
          this._midPointCalculated = true;
        }
        return this._clanMidPoint;
      }
    }

    public IEnumerable<StanceLink> Stances => (IEnumerable<StanceLink>) this._stances;

    public IFaction MapFaction => this.Kingdom != null ? (IFaction) this.Kingdom : (IFaction) this;

    public float Aggressiveness
    {
      get => this._aggressiveness;
      internal set => this._aggressiveness = MathF.Clamp(value, 0.0f, 100f);
    }

    public int TributeWallet
    {
      get => this._tributeWallet;
      set => this._tributeWallet = value;
    }

    public StanceLink GetStanceWith(IFaction other) => FactionManager.Instance.GetStanceLinkInternal((IFaction) this, other);

    [SpecialName]
    string IFaction.get_StringId() => this.StringId;

    [SpecialName]
    MBGUID IFaction.get_Id() => this.Id;

    internal static void AutoGeneratedStaticCollectObjectsClan(
      object o,
      List<object> collectedObjects)
    {
      ((MBObjectBase) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
    }

    protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
    {
      base.AutoGeneratedInstanceCollectObjects(collectedObjects);
      collectedObjects.Add((object) this._defaultPartyTemplate);
      collectedObjects.Add((object) this._characterTemplates);
      collectedObjects.Add((object) this._kingdom);
      collectedObjects.Add((object) this._basicTroop);
      collectedObjects.Add((object) this._leader);
      collectedObjects.Add((object) this._banner);
      collectedObjects.Add((object) this._home);
      collectedObjects.Add((object) this.Name);
      collectedObjects.Add((object) this.InformalName);
      collectedObjects.Add((object) this.Culture);
      CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime((object) this.LastFactionChangeTime, collectedObjects);
      collectedObjects.Add((object) this.EncyclopediaText);
      CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime((object) this.NotAttackableByPlayerUntilTime, collectedObjects);
    }

    internal static object AutoGeneratedGetMemberValueName(object o) => (object) ((Clan) o).Name;

    internal static object AutoGeneratedGetMemberValueInformalName(object o) => (object) ((Clan) o).InformalName;

    internal static object AutoGeneratedGetMemberValueCulture(object o) => (object) ((Clan) o).Culture;

    internal static object AutoGeneratedGetMemberValueLastFactionChangeTime(object o) => (object) ((Clan) o).LastFactionChangeTime;

    internal static object AutoGeneratedGetMemberValueAutoRecruitmentExpenses(object o) => (object) ((Clan) o).AutoRecruitmentExpenses;

    internal static object AutoGeneratedGetMemberValueEncyclopediaText(object o) => (object) ((Clan) o).EncyclopediaText;

    internal static object AutoGeneratedGetMemberValueMercenaryAwardMultiplier(object o) => (object) ((Clan) o).MercenaryAwardMultiplier;

    internal static object AutoGeneratedGetMemberValueLabelColor(object o) => (object) ((Clan) o).LabelColor;

    internal static object AutoGeneratedGetMemberValueInitialPosition(object o) => (object) ((Clan) o).InitialPosition;

    internal static object AutoGeneratedGetMemberValueIsRebelClan(object o) => (object) ((Clan) o).IsRebelClan;

    internal static object AutoGeneratedGetMemberValueIsMinorFaction(object o) => (object) ((Clan) o).IsMinorFaction;

    internal static object AutoGeneratedGetMemberValueIsOutlaw(object o) => (object) ((Clan) o).IsOutlaw;

    internal static object AutoGeneratedGetMemberValueIsNomad(object o) => (object) ((Clan) o).IsNomad;

    internal static object AutoGeneratedGetMemberValueIsMafia(object o) => (object) ((Clan) o).IsMafia;

    internal static object AutoGeneratedGetMemberValueIsClanTypeMercenary(object o) => (object) ((Clan) o).IsClanTypeMercenary;

    internal static object AutoGeneratedGetMemberValueIsSect(object o) => (object) ((Clan) o).IsSect;

    internal static object AutoGeneratedGetMemberValueIsUnderMercenaryService(object o) => (object) ((Clan) o).IsUnderMercenaryService;

    internal static object AutoGeneratedGetMemberValueColor(object o) => (object) ((Clan) o).Color;

    internal static object AutoGeneratedGetMemberValueColor2(object o) => (object) ((Clan) o).Color2;

    internal static object AutoGeneratedGetMemberValueAlternativeColor(object o) => (object) ((Clan) o).AlternativeColor;

    internal static object AutoGeneratedGetMemberValueAlternativeColor2(object o) => (object) ((Clan) o).AlternativeColor2;

    internal static object AutoGeneratedGetMemberValueBannerBackgroundColorPrimary(object o) => (object) ((Clan) o).BannerBackgroundColorPrimary;

    internal static object AutoGeneratedGetMemberValueBannerBackgroundColorSecondary(object o) => (object) ((Clan) o).BannerBackgroundColorSecondary;

    internal static object AutoGeneratedGetMemberValueBannerIconColor(object o) => (object) ((Clan) o).BannerIconColor;

    internal static object AutoGeneratedGetMemberValueIsBanditFaction(object o) => (object) ((Clan) o).IsBanditFaction;

    internal static object AutoGeneratedGetMemberValueRenown(object o) => (object) ((Clan) o).Renown;

    internal static object AutoGeneratedGetMemberValueMainHeroCrimeRating(object o) => (object) ((Clan) o).MainHeroCrimeRating;

    internal static object AutoGeneratedGetMemberValueNotAttackableByPlayerUntilTime(object o) => (object) ((Clan) o).NotAttackableByPlayerUntilTime;

    internal static object AutoGeneratedGetMemberValue_defaultPartyTemplate(object o) => (object) ((Clan) o)._defaultPartyTemplate;

    internal static object AutoGeneratedGetMemberValue_isEliminated(object o) => (object) ((Clan) o)._isEliminated;

    internal static object AutoGeneratedGetMemberValue_characterTemplates(object o) => (object) ((Clan) o)._characterTemplates;

    internal static object AutoGeneratedGetMemberValue_kingdom(object o) => (object) ((Clan) o)._kingdom;

    internal static object AutoGeneratedGetMemberValue_influence(object o) => (object) ((Clan) o)._influence;

    internal static object AutoGeneratedGetMemberValue_basicTroop(object o) => (object) ((Clan) o)._basicTroop;

    internal static object AutoGeneratedGetMemberValue_leader(object o) => (object) ((Clan) o)._leader;

    internal static object AutoGeneratedGetMemberValue_banner(object o) => (object) ((Clan) o)._banner;

    internal static object AutoGeneratedGetMemberValue_tier(object o) => (object) ((Clan) o)._tier;

    internal static object AutoGeneratedGetMemberValue_home(object o) => (object) ((Clan) o)._home;

    internal static object AutoGeneratedGetMemberValue_clanDebtToKingdom(object o) => (object) ((Clan) o)._clanDebtToKingdom;

    internal static object AutoGeneratedGetMemberValue_aggressiveness(object o) => (object) ((Clan) o)._aggressiveness;

    internal static object AutoGeneratedGetMemberValue_tributeWallet(object o) => (object) ((Clan) o)._tributeWallet;

    public void ChangeClanName(TextObject name) => this.Name = name;

    public static Clan CreateClan(string stringID)
    {
      stringID = Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Clan>(stringID);
      Clan clan = new Clan();
      clan.StringId = stringID;
      Campaign.Current.CampaignObjectManager.AddClan(clan);
      return clan;
    }

    private void InitMembers()
    {
      this._companionsCache = new List<Hero>();
      this.Companions = new MBReadOnlyList<Hero>(this._companionsCache);
      this._warPartyComponentsCache = new List<WarPartyComponent>();
      this.WarPartyComponents = new MBReadOnlyList<WarPartyComponent>(this._warPartyComponentsCache);
      this._stances = new List<StanceLink>();
      this._lordsCache = new List<Hero>();
      this.Lords = new MBReadOnlyList<Hero>(this._lordsCache);
      this._heroesCache = new List<Hero>();
      this.Heroes = new MBReadOnlyList<Hero>(this._heroesCache);
      this._supporterNotables = new List<Hero>();
      this._villagesCache = new List<Village>();
      this._fiefsCache = new List<Town>();
      this._settlementsCache = new List<Settlement>();
      this.Villages = new MBReadOnlyList<Village>(this._villagesCache);
      this.Fiefs = new MBReadOnlyList<Town>(this._fiefsCache);
      this.Settlements = new MBReadOnlyList<Settlement>(this._settlementsCache);
      this._villagesAndSettlementsCacheIsDirty = true;
    }

    public void InitializeClan(
      TextObject name,
      TextObject informalName,
      CultureObject culture,
      Banner banner,
      Vec2 initialPosition = default (Vec2),
      bool isDeserialize = false)
    {
      this.ChangeClanName(name);
      this.InformalName = informalName;
      this.Culture = culture;
      this.Banner = banner;
      if (isDeserialize)
        return;
      this.ValidateInitialPosition(initialPosition);
    }

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData)
    {
      this.InitMembers();
      this._heroesCache.AddRange((IEnumerable<Hero>) this._companionsCache);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
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

    public void OnGameCreated() => this.ValidateInitialPosition(this.InitialPosition);

    private void SetKingdomInternal(Kingdom value)
    {
      if (this.Kingdom != null)
        this.LeaveKingdomInternal();
      this._kingdom = value;
      if (this.Kingdom != null)
        this.EnterKingdomInternal();
      this.UpdateBannerColorsAccordingToKingdom();
      this.LastFactionChangeTime = CampaignTime.Now;
      this.ConsiderSiegesAndMapEvents((IFaction) this._kingdom);
    }

    internal void ConsiderSiegesAndMapEvents(IFaction factionToConsiderAgainst)
    {
      foreach (PartyComponent partyComponent in this._warPartyComponentsCache.ToList<WarPartyComponent>())
        partyComponent.MobileParty.ConsiderMapEventsAndSiegesInternal(factionToConsiderAgainst);
      foreach (Town fief in this.Fiefs)
        fief.ConsiderSiegesAndMapEventsInternal(factionToConsiderAgainst);
    }

    private void EnterKingdomInternal() => this._kingdom?.AddClanInternal(this);

    private void LeaveKingdomInternal()
    {
      this.Influence = 0.0f;
      this._kingdom?.RemoveClanInternal(this);
      foreach (WarPartyComponent warPartyComponent in this._warPartyComponentsCache.ToList<WarPartyComponent>())
      {
        if (warPartyComponent.MobileParty.Army != null)
          warPartyComponent.MobileParty.Army = (Army) null;
      }
    }

    public void ClanLeaveKingdom(bool giveBackFiefs = false)
    {
      this.Influence = 0.0f;
      if (this.Kingdom != null)
      {
        foreach (Settlement settlement in Campaign.Current.Settlements)
        {
          if (settlement.IsTown && settlement.OwnerClan == this)
            SettlementHelper.TakeEnemyVillagersOutsideSettlements(settlement);
        }
      }
      this.LastFactionChangeTime = CampaignTime.Now;
      this.Kingdom = (Kingdom) null;
    }

    public void SetLeader(Hero leader)
    {
      this._leader = leader;
      if (leader == null)
        return;
      leader.Clan = this;
    }

    public int GetRelationWithClan(Clan other) => this.Leader != null && other.Leader != null ? this.Leader.GetRelation(other.Leader) : 0;

    private void UpdateFactionMidPoint()
    {
      this._clanMidPoint = FactionHelper.FactionMidPoint((IFaction) this);
      this._midPointCalculated = true;
    }

    internal void UpdateStrength()
    {
      this.TotalStrength = 0.0f;
      foreach (PartyComponent partyComponent in this._warPartyComponentsCache)
        this.TotalStrength += partyComponent.MobileParty.Party.TotalStrength;
      foreach (Town fief in this.Fiefs)
      {
        if (fief.GarrisonParty != null)
          this.TotalStrength += fief.GarrisonParty.Party.TotalStrength;
      }
    }

    internal void AddStanceInternal(StanceLink stanceLink) => this._stances.Add(stanceLink);

    internal void RemoveStanceInternal(StanceLink stanceLink) => this._stances.Remove(stanceLink);

    internal void AddWarPartyInternal(WarPartyComponent warPartyComponent)
    {
      this._warPartyComponentsCache.Add(warPartyComponent);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    internal void RemoveWarPartyInternal(WarPartyComponent warPartyComponent)
    {
      this._warPartyComponentsCache.Remove(warPartyComponent);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    public float CalculateSettlementValue(Kingdom kingdom = null)
    {
      float num = 0.0f;
      foreach (Town fief in this.Fiefs)
        num += Campaign.Current.Models.SettlementValueModel.CalculateValueForFaction(fief.Owner.Settlement, (IFaction) kingdom);
      return num;
    }

    public void StartMercenaryService() => this.IsUnderMercenaryService = true;

    public void EndMercenaryService(bool isByLeavingKingdom) => this.IsUnderMercenaryService = false;

    protected override void PreAfterLoad()
    {
      if (this._kingdom != null)
        this._kingdom.AddClanInternal(this);
      this.ValidateInitialPosition(this.InitialPosition);
      this.UpdateBannerColorsAccordingToKingdom();
      if (this._characterTemplates == null)
      {
        this._characterTemplates = new List<CharacterObject>();
        int num = 0;
        while (true)
        {
          CharacterObject characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("spc_" + this.StringId.ToLower() + "_leader_" + (object) num++);
          if (characterObject != null)
            this._characterTemplates.Add(characterObject);
          else
            break;
        }
      }
      if (!this.IsUnderMercenaryService || this.Kingdom != null)
        return;
      this.IsUnderMercenaryService = false;
    }

    protected override void AfterLoad() => this.UpdateStrength();

    public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    {
      base.Deserialize(objectManager, node);
      this.SetLeader(objectManager.ReadObjectReferenceFromXml("owner", typeof (Hero), node) as Hero);
      this.Kingdom = (Kingdom) objectManager.ReadObjectReferenceFromXml("super_faction", typeof (Kingdom), node);
      this.Tier = node.Attributes["tier"] == null ? 1 : Convert.ToInt32(node.Attributes["tier"].Value);
      this.Renown = (float) Campaign.Current.Models.ClanTierModel.CalculateInitialRenown(this);
      this.InitializeClan(new TextObject(node.Attributes["name"].Value), node.Attributes["short_name"] != null ? new TextObject(node.Attributes["short_name"].Value) : new TextObject(node.Attributes["name"].Value), (CultureObject) objectManager.ReadObjectReferenceFromXml("culture", typeof (CultureObject), node), (Banner) null, isDeserialize: true);
      this.LabelColor = node.Attributes["label_color"] == null ? 0U : Convert.ToUInt32(node.Attributes["label_color"].Value, 16);
      this.Color = node.Attributes["color"] == null ? 0U : Convert.ToUInt32(node.Attributes["color"].Value, 16);
      this.Color2 = node.Attributes["color2"] == null ? 0U : Convert.ToUInt32(node.Attributes["color2"].Value, 16);
      this.AlternativeColor = node.Attributes["alternative_color"] == null ? 0U : Convert.ToUInt32(node.Attributes["alternative_color"].Value, 16);
      this.AlternativeColor2 = node.Attributes["alternative_color2"] == null ? 0U : Convert.ToUInt32(node.Attributes["alternative_color2"].Value, 16);
      if (node.Attributes["initial_posX"] != null && node.Attributes["initial_posY"] != null)
        this.InitialPosition = new Vec2((float) Convert.ToDouble(node.Attributes["initial_posX"].Value), (float) Convert.ToDouble(node.Attributes["initial_posY"].Value));
      this.IsBanditFaction = node.Attributes["is_bandit"] != null && Convert.ToBoolean(node.Attributes["is_bandit"].Value);
      this.IsMinorFaction = node.Attributes["is_minor_faction"] != null && Convert.ToBoolean(node.Attributes["is_minor_faction"].Value);
      this.IsOutlaw = node.Attributes["is_outlaw"] != null && Convert.ToBoolean(node.Attributes["is_outlaw"].Value);
      this.IsSect = node.Attributes["is_sect"] != null && Convert.ToBoolean(node.Attributes["is_sect"].Value);
      this.IsMafia = node.Attributes["is_mafia"] != null && Convert.ToBoolean(node.Attributes["is_mafia"].Value);
      this.IsClanTypeMercenary = node.Attributes["is_clan_type_mercenary"] != null && Convert.ToBoolean(node.Attributes["is_clan_type_mercenary"].Value);
      this.IsNomad = node.Attributes["is_nomad"] != null && Convert.ToBoolean(node.Attributes["is_nomad"].Value);
      this._defaultPartyTemplate = (PartyTemplateObject) objectManager.ReadObjectReferenceFromXml("default_party_template", typeof (PartyTemplateObject), node);
      this.EncyclopediaText = node.Attributes["text"] != null ? new TextObject(node.Attributes["text"].Value) : TextObject.Empty;
      if (node.Attributes["banner_key"] != null)
      {
        this._banner = new Banner();
        this._banner.Deserialize(node.Attributes["banner_key"].Value);
      }
      else
        this._banner = Banner.CreateRandomClanBanner(this.StringId.GetDeterministicHashCode());
      this.BannerBackgroundColorPrimary = this._banner.GetPrimaryColor();
      this.BannerBackgroundColorSecondary = this._banner.GetSecondaryColor();
      this.BannerIconColor = this._banner.GetFirstIconColor();
      this.UpdateBannerColorsAccordingToKingdom();
      this._characterTemplates = new List<CharacterObject>();
      foreach (XmlNode childNode1 in node.ChildNodes)
      {
        if (childNode1.Name == "character_templates")
        {
          foreach (XmlNode childNode2 in childNode1.ChildNodes)
            this._characterTemplates.Add(objectManager.ReadObjectReferenceFromXml("id", typeof (CharacterObject), childNode2) as CharacterObject);
        }
        else if (childNode1.Name == "relationship")
        {
          IFaction faction2 = childNode1.Attributes["clan"] == null ? (IFaction) objectManager.ReadObjectReferenceFromXml("kingdom", typeof (Kingdom), childNode1) : (IFaction) objectManager.ReadObjectReferenceFromXml("clan", typeof (Clan), childNode1);
          int int32 = Convert.ToInt32(childNode1.Attributes["value"].InnerText);
          if (int32 > 0)
            FactionManager.DeclareAlliance((IFaction) this, faction2);
          else if (int32 < 0)
            FactionManager.DeclareWar((IFaction) this, faction2);
          else
            FactionManager.SetNeutral((IFaction) this, faction2);
        }
      }
    }

    public override string ToString() => "(" + (object) this.Id + ") " + (object) this.Name;

    public override TextObject GetName() => this.Name;

    private int DistanceOfTwoValues(int x, int y) => Math.Min((x < 50 ? x : 100 - x) + (y < 50 ? y : 100 - y), x - y);

    private float FindSettlementScoreForBeingHomeSettlement(Settlement settlement)
    {
      int prosperity = (int) settlement.Prosperity;
      foreach (Village boundVillage in settlement.BoundVillages)
        prosperity += (int) boundVillage.Settlement.Prosperity;
      double num1 = settlement.IsTown ? 1.0 : 0.5;
      float num2 = (float) Math.Sqrt(1000.0 + (double) prosperity) / 50f;
      float num3 = this.HomeSettlement == settlement ? 1f : 0.65f;
      float num4 = settlement.Culture == this.Culture ? 1f : 0.25f;
      float num5 = settlement.OwnerClan.Culture == this.Culture ? 1f : 0.85f;
      float num6 = settlement.OwnerClan == this ? 1f : 0.1f;
      float num7 = settlement.MapFaction == this.MapFaction ? 1f : 0.1f;
      float num8 = (float) (1.0 - (double) this.MapFaction.FactionMidPoint.Distance(settlement.Position2D) / (double) Campaign.MapDiagonal);
      float num9 = num8 * num8;
      double num10 = (double) num2;
      return (float) (num1 * num10) * num4 * num5 * num7 * num6 * num9 * num3;
    }

    public void UpdateHomeSettlement(Settlement updatedSettlement)
    {
      Settlement settlement1 = this.HomeSettlement;
      if (this.HomeSettlement == null || updatedSettlement == null || this.HomeSettlement == updatedSettlement && updatedSettlement.OwnerClan != this)
      {
        float num = 0.0f;
        foreach (Settlement settlement2 in Settlement.All)
        {
          if (settlement2.IsFortification)
          {
            float beingHomeSettlement = this.FindSettlementScoreForBeingHomeSettlement(settlement2);
            if ((double) beingHomeSettlement > (double) num)
            {
              settlement1 = settlement2;
              num = beingHomeSettlement;
            }
          }
        }
      }
      if (settlement1 == this.HomeSettlement)
        return;
      this.HomeSettlement = settlement1;
      foreach (Hero hero in this.Heroes)
        hero.UpdateHomeSettlement();
    }

    public static Clan FindFirst(Predicate<Clan> predicate) => Clan.All.FirstOrDefault<Clan>((Func<Clan, bool>) (x => predicate(x)));

    public static IEnumerable<Clan> FindAll(Predicate<Clan> predicate) => Clan.All.Where<Clan>((Func<Clan, bool>) (x => predicate(x)));

    internal void RemoveFiefInternal(Town settlement)
    {
      this._fiefsCache.Remove(settlement);
      this._midPointCalculated = false;
      if (this._kingdom != null)
        this._kingdom._midPointCalculated = false;
      this._villagesAndSettlementsCacheIsDirty = true;
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    internal void AddFiefInternal(Town settlement)
    {
      this._fiefsCache.Add(settlement);
      this._midPointCalculated = false;
      if (this._kingdom != null)
        this._kingdom._midPointCalculated = false;
      this._villagesAndSettlementsCacheIsDirty = true;
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    private void CollectSettlementsAndVillagesToCache()
    {
      this._villagesCache.Clear();
      this._settlementsCache.Clear();
      foreach (Town fief in this.Fiefs)
      {
        this._settlementsCache.Add(fief.Owner.Settlement);
        foreach (Village boundVillage in fief.Owner.Settlement.BoundVillages)
        {
          this._settlementsCache.Add(boundVillage.Owner.Settlement);
          this._villagesCache.Add(boundVillage);
        }
      }
      this._villagesAndSettlementsCacheIsDirty = false;
    }

    internal void RemoveHeroInternal(Hero hero)
    {
      if (hero.CharacterObject.Occupation != Occupation.Lord)
        return;
      this._lordsCache.Remove(hero);
      this._heroesCache.Remove(hero);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    internal void AddHeroInternal(Hero hero)
    {
      if (hero.CharacterObject.Occupation != Occupation.Lord)
        return;
      this._lordsCache.Add(hero);
      this._heroesCache.Add(hero);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    internal void AddSupporterInternal(Hero hero) => this._supporterNotables.Add(hero);

    internal void RemoveSupporterInternal(Hero hero) => this._supporterNotables.Remove(hero);

    public void AddRenown(float value, bool shouldNotify = true)
    {
      if ((double) value <= 0.0)
        return;
      this.Renown += value;
      int tier = Campaign.Current.Models.ClanTierModel.CalculateTier(this);
      if (tier <= this.Tier)
        return;
      this.Tier = tier;
      CampaignEventDispatcher.Instance.OnClanTierChanged(this, shouldNotify);
    }

    public void ResetClanRenown()
    {
      this.Renown = 0.0f;
      this.Tier = Campaign.Current.Models.ClanTierModel.CalculateTier(this);
      CampaignEventDispatcher.Instance.OnClanTierChanged(this, false);
    }

    public void OnSupportedByClan(Clan supporterClan)
    {
      DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
      int ofSupportingClan1 = diplomacyModel.GetInfluenceCostOfSupportingClan();
      if ((double) supporterClan.Influence < (double) ofSupportingClan1)
        return;
      int ofSupportingClan2 = diplomacyModel.GetInfluenceValueOfSupportingClan();
      int ofSupportingClan3 = diplomacyModel.GetRelationValueOfSupportingClan();
      supporterClan.Influence -= (float) ofSupportingClan1;
      this.Influence += (float) ofSupportingClan2;
      ChangeRelationAction.ApplyRelationChangeBetweenHeroes(supporterClan.Leader, this.Leader, ofSupportingClan3);
    }

    public static Clan CreateSettlementRebelClan(
      Settlement settlement,
      Hero owner,
      int iconMeshId = -1)
    {
      Clan clan = Clan.CreateClan(settlement.StringId + "_rebel_clan");
      TextObject textObject = new TextObject("{=2LIV2cy7}{SETTLEMENT}'s rebels");
      textObject.SetTextVariable("SETTLEMENT", settlement.Name);
      clan.InitializeClan(textObject, textObject, settlement.Culture, Banner.CreateOneColoredBannerWithOneIcon(settlement.MapFaction.Banner.GetFirstIconColor(), settlement.MapFaction.Banner.GetPrimaryColor(), iconMeshId), settlement.GatePosition);
      clan.SetLeader(owner);
      clan.LabelColor = settlement.MapFaction.LabelColor;
      clan.Color = settlement.MapFaction.Color2;
      clan.Color2 = settlement.MapFaction.Color;
      clan.IsRebelClan = true;
      clan.Tier = Campaign.Current.Models.ClanTierModel.RebelClanStartingTier;
      clan.BannerBackgroundColorPrimary = settlement.MapFaction.Banner.GetFirstIconColor();
      clan.BannerBackgroundColorSecondary = settlement.MapFaction.Banner.GetFirstIconColor();
      clan.BannerIconColor = settlement.MapFaction.Banner.GetPrimaryColor();
      clan._midPointCalculated = false;
      clan.HomeSettlement = settlement;
      return clan;
    }

    public static Clan CreateCompanionToLordClan(
      Hero hero,
      Settlement settlement,
      TextObject clanName,
      int newClanIconId)
    {
      Clan clan = Clan.CreateClan(Hero.MainHero.MapFaction.StringId + "_companion_clan");
      clan.InitializeClan(clanName, clanName, settlement.Culture, Banner.CreateOneColoredBannerWithOneIcon(settlement.MapFaction.Banner.GetFirstIconColor(), settlement.MapFaction.Banner.GetPrimaryColor(), newClanIconId), settlement.GatePosition);
      clan.Kingdom = Hero.MainHero.Clan.Kingdom;
      clan.Tier = Campaign.Current.Models.ClanTierModel.CompanionToLordClanStartingTier;
      hero.Clan = clan;
      clan.SetLeader(hero);
      ChangeOwnerOfSettlementAction.ApplyByGift(settlement, hero);
      return clan;
    }

    internal void AddCompanion(Hero hero)
    {
      this._heroesCache.Add(hero);
      this._companionsCache.Add(hero);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    internal void RemoveCompanion(Hero hero)
    {
      this._heroesCache.Remove(hero);
      this._companionsCache.Remove(hero);
      Action lordsCacheUpdated = this.OnPartiesAndLordsCacheUpdated;
      if (lordsCacheUpdated == null)
        return;
      lordsCacheUpdated();
    }

    public MobileParty CreateNewMobileParty(Hero hero)
    {
      MobileParty mobileParty;
      if (hero.CurrentSettlement != null)
      {
        Settlement currentSettlement = hero.CurrentSettlement;
        if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsMainParty)
          PartyBase.MainParty.MemberRoster.RemoveTroop(hero.CharacterObject);
        mobileParty = MobilePartyHelper.SpawnLordParty(hero, currentSettlement);
      }
      else
      {
        MobileParty partyBelongedTo = hero.PartyBelongedTo;
        partyBelongedTo?.AddElementToMemberRoster(hero.CharacterObject, -1);
        mobileParty = MobilePartyHelper.SpawnLordParty(hero, partyBelongedTo != null ? partyBelongedTo.Position2D : SettlementHelper.GetBestSettlementToSpawnAround(hero).GatePosition, 5f);
      }
      return mobileParty;
    }

    public MobileParty CreateNewMobilePartyAtPosition(Hero hero, Vec2 spawnPosition) => MobilePartyHelper.SpawnLordParty(hero, spawnPosition, 5f);

    public Dictionary<Hero, int> GetHeirApparents()
    {
      Dictionary<Hero, int> dictionary = new Dictionary<Hero, int>();
      int heroComesOfAge = Campaign.Current.Models.AgeModel.HeroComesOfAge;
      Hero leader = this.Leader;
      foreach (Hero hero in this.Leader.Clan.Heroes)
      {
        if (hero != this.Leader && hero.IsAlive && !hero.IsNotSpawned && !hero.IsDisabled && !hero.IsWanderer && !hero.IsNotable && (double) hero.Age >= (double) heroComesOfAge)
        {
          int heirSelectionPoint = Campaign.Current.Models.HeirSelectionCalculationModel.CalculateHeirSelectionPoint(hero, this.Leader, ref leader);
          dictionary.Add(hero, heirSelectionPoint);
        }
      }
      if (leader != this.Leader)
        dictionary[leader] += 5;
      return dictionary;
    }

    private void UpdateBannerColorsAccordingToKingdom()
    {
      if (this.Kingdom != null)
      {
        this.Banner?.ChangePrimaryColor(this.Kingdom.PrimaryBannerColor);
        this.Banner?.ChangeIconColors(this.Kingdom.SecondaryBannerColor);
        if (this.Kingdom.RulingClan != this)
          return;
        this._banner?.ChangePrimaryColor(this.Kingdom.PrimaryBannerColor);
        this._banner?.ChangeIconColors(this.Kingdom.SecondaryBannerColor);
      }
      else if (this.BannerBackgroundColorPrimary != 0U || this.BannerBackgroundColorSecondary != 0U || this.BannerIconColor != 0U)
      {
        this.Banner?.ChangeBackgroundColor(this.BannerBackgroundColorPrimary, this.BannerBackgroundColorSecondary);
        this.Banner?.ChangeIconColors(this.BannerIconColor);
      }
      else
      {
        if (!this.IsMinorFaction)
          return;
        this.Banner?.ChangePrimaryColor(this.Color);
        this.Banner?.ChangeIconColors((int) this.Color != (int) this.Color2 ? this.Color2 : uint.MaxValue);
      }
    }

    public void UpdateBannerColor(uint backgroundColor, uint iconColor)
    {
      this.BannerBackgroundColorPrimary = backgroundColor;
      this.BannerBackgroundColorSecondary = backgroundColor;
      this.BannerIconColor = iconColor;
    }

    internal void DeactivateClan() => this._isEliminated = true;

    private void ValidateInitialPosition(Vec2 initialPosition)
    {
      if (initialPosition.IsValid && this.InitialPosition.IsNonZero())
      {
        this.InitialPosition = initialPosition;
      }
      else
      {
        Vec2 centerPosition;
        if (this.Settlements.Count > 0)
        {
          centerPosition = this.Settlements.GetRandomElement<Settlement>().GatePosition;
        }
        else
        {
          Settlement elementWithPredicate = Settlement.All.GetRandomElementWithPredicate<Settlement>((Func<Settlement, bool>) (x => x.Culture == this.Culture));
          centerPosition = elementWithPredicate != null ? elementWithPredicate.GatePosition : Settlement.All.GetRandomElement<Settlement>().GatePosition;
        }
        this.InitialPosition = MobilePartyHelper.FindReachablePointAroundPosition((PartyBase) null, centerPosition, 150f);
      }
    }

    public class ClanExpenseInfo
    {
      public ClanExpenseInfo(MobileParty mobileParty)
      {
        this.MobileParty = mobileParty;
        this.PaymentLimit = Campaign.Current.Models.PartyWageModel.MaxWage;
        this.UnlimitedWage = true;
      }


      public MobileParty MobileParty { get; private set; }


      public int PaymentLimit { get; set; }


      public bool UnlimitedWage { get; set; }

      internal static void AutoGeneratedStaticCollectObjectsClanExpenseInfo(
        object o,
        List<object> collectedObjects)
      {
        ((Clan.ClanExpenseInfo) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
      }

      protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects) => collectedObjects.Add((object) this.MobileParty);

      internal static object AutoGeneratedGetMemberValueMobileParty(object o) => (object) ((Clan.ClanExpenseInfo) o).MobileParty;

      internal static object AutoGeneratedGetMemberValuePaymentLimit(object o) => (object) ((Clan.ClanExpenseInfo) o).PaymentLimit;

      internal static object AutoGeneratedGetMemberValueUnlimitedWage(object o) => (object) ((Clan.ClanExpenseInfo) o).UnlimitedWage;
    }
  }
}