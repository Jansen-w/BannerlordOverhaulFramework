using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BOF.Overhaul.CampaignSystem
{
  public class MobileParty : 
    CampaignObjectBase,
    ILocatable<MobileParty>,
    IMapPoint,
    ITrackableCampaignObject,
    ITrackableBase,
    IMapEntity
  {
    private const float AiCheckInterval = 0.25f;
    private const float PatrolRadius = 30f;
    private const float MaximumJoiningRadiusMultipicator = 2f;
    private static readonly List<(float, Vec2)> DangerousPartiesAndTheirVecs = new List<(float, Vec2)>();
    private const int FleeToNearbyPartyDistanceRadius = 10;
    private const int FleeToNearbySettlementDistanceRadius = 20;
    public const int DefaultPartyTradeInitialGold = 5000;
    //[SaveableField(1001)]
    private Settlement _currentSettlement;
    // //[CachedData]
    private List<MobileParty> _lastTargetedParties;
    // //[CachedData]
    private List<MobileParty> _attachedParties;
    //[SaveableField(1046)]
    private MobileParty _attachedTo;
    //[SaveableField(1006)]
    public float HasUnpaidWages;
    //[SaveableField(1022)]
    private Vec2 _formationPosition;
    //[SaveableField(1060)]
    private Vec2 _eventPositionAdder;
    //[SaveableField(1055)]
    private MoveModeType _partyMoveMode;
    //[SaveableField(1038)]
    private bool _aiPathMode;
    //[SaveableField(1039)]
    private bool _aiPathNeeded;
    //[SaveableField(1062)]
    private Vec2 _nextTargetPosition;
    //[SaveableField(1047)]
    private AiBehavior _defaultBehavior;
    //[SaveableField(1057)]
    private float _distanceToNextTrackSq = 2f;
    //[SaveableField(1100)]
    private Vec2 _position2D;
    private string _partyFlagObsolete;
    //[SaveableField(1024)]
    private bool _isVisible;
    //[SaveableField(1025)]
    private bool _isInspected;
    //[SaveableField(1955)]
    private CampaignTime _disorganizedUntilTime;
    ////[CachedData]
    private int _partyPureSpeedLastCheckVersion = -1;
    //[SaveableField(1027)]
    private ulong _partyLastCheckPositionVersion;
    //[SaveableField(1028)]
    private bool _partyLastCheckIsPrisoner;
    //[SaveableField(1029)]
    private float _pureSpeed = 1f;
    //[CachedData]
    private ExplainedNumber _pureSpeedExplainer;
    //[SaveableField(1030)]
    private float _lastCalculatedFinalSpeed = 1f;
    //[SaveableField(1031)]
    private bool _partyLastCheckAtNight;
    //[CachedData]
    private int _itemRosterVersionNo = -1;
    //[CachedData]
    private int _partySizeRatioLastCheckVersion = -1;
    //[CachedData]
    private int _latestUsedPaymentRatio = -1;
    //[CachedData]
    private float _cachedPartySizeRatio = 1f;
    //[CachedData]
    private int _cachedPartySizeLimit;
    //[SaveableField(1059)]
    private BesiegerCamp _besiegerCamp;
    //[SaveableField(1048)]
    private MobileParty _targetParty;
    //[SaveableField(1049)]
    private Settlement _targetSettlement;
    //[SaveableField(1053)]
    private Vec2 _targetPosition;
    //[SaveableField(1032)]
    private int _doNotAttackMainParty;
    //[SaveableField(1033)]
    public readonly bool AtCampMode;
    //[SaveableField(1034)]
    private Settlement _customHomeSettlement;
    //[SaveableField(1036)]
    private float _attackInitiative = 1f;
    //[SaveableField(1037)]
    private float _avoidInitiative = 1f;
    //[SaveableField(1035)]
    private Army _army;
    //[CachedData]
    private bool _isDisorganized;
    //[SaveableField(1959)]
    private bool _isCurrentlyUsedByAQuest;
    //[SaveableField(1956)]
    private int _partyTradeGold;
    //[SaveableField(1063)]
    private CampaignTime _ignoredUntilTime;
    //[SaveableField(1064)]
    private CampaignTime _initiativeRestoreTime;
    //[SaveableField(1065)]
    private int _numberOfFleeingsAtLastTravel;
    //[SaveableField(1071)]
    public Vec2 AverageFleeTargetDirection;
    //[CachedData]
    private PathFaceRecord _targetAiFaceIndex = PathFaceRecord.NullFaceRecord;
    //[CachedData]
    private PathFaceRecord _moveTargetAiFaceIndex = PathFaceRecord.NullFaceRecord;
    //[SaveableField(1070)]
    private Vec2 _aiPathLastPosition;
    //[CachedData]
    private PathFaceRecord _aiPathLastFace;
    private bool _aiPathNotFound;
    //[CachedData]
    private MobilePartiesAroundPositionList _partiesAroundPosition;
    //[SaveableField(1068)]
    private bool _aiBehaviorResetNeeded;
    public bool IsJoiningArmy;
    private bool _resetStarted;
    //[SaveableField(1066)]
    private CampaignTime _nextAiCheckTime;
    //[SaveableField(1069)]
    private bool _defaultBehaviorNeedsUpdate;
    //[SaveableField(1067)]
    private Vec2 _lastTrackPosition;
    //[CachedData]
    private int _locatorNodeIndex;
    //[SaveableField(1120)]
    private Clan _actualClan;
    //[SaveableField(1200)]
    private float _moraleDueToEvents;
    //[CachedData]
    private Vec2 _errorPosition;
    //[CachedData]
    private float _lastVisualSpeed;
    //[CachedData]
    private int _curTick = 3;
    //[CachedData]
    private float _cachedComputedSpeed;
    public const int ClanRoleAssignmentMinimumSkillValue = 0;
    //[SaveableField(210)]
    private PartyComponent _partyComponent;

    public static MobileParty MainParty => BOFCampaign.Current == null ? (MobileParty) null : BOFCampaign.Current.MainParty;

    public static MBReadOnlyList<MobileParty> All => BOFCampaign.Current.MobileParties;

    public static int Count => BOFCampaign.Current.MobileParties.Count;

    public static MobileParty ConversationParty => BOFCampaign.Current.ConversationManager.ConversationParty;

    ////[SaveableProperty(1021)]
    private TextObject CustomName { get; set; }

    public TextObject Name
    {
      get
      {
        if (!TextObject.IsNullOrEmpty(this.CustomName))
          return this.CustomName;
        return this._partyComponent == null ? new TextObject("{=!}unnamedMobileParty") : this._partyComponent.Name;
      }
    }

    ////[SaveableProperty(1002)]
    public Settlement LastVisitedSettlement { get; private set; }

    ////[SaveableProperty(1004)]
    public Vec2 Bearing { get; private set; }

    public MBReadOnlyList<MobileParty> AttachedParties { get; private set; }

    //[SaveableProperty(1009)]
    public float Aggressiveness { get; set; }

    //[SaveableProperty(1010)]
    public int PaymentLimit { get; set; } = BOFCampaign.Current.Models.PartyWageModel.MaxWage;

    public bool UnlimitedWage => this.PaymentLimit == BOFCampaign.Current.Models.PartyWageModel.MaxWage;

    //[SaveableProperty(1005)]
    public Vec2 ArmyPositionAdder { get; private set; }

    //[SaveableProperty(1051)]
    public Vec2 AiBehaviorTarget { get; private set; }

    //[SaveableProperty(1054)]
    public MobileParty MoveTargetParty { get; private set; }

    //[SaveableProperty(1061)]
    public Vec2 MoveTargetPoint { get; private set; }

    //[SaveableProperty(1090)]
    public MobileParty.PartyObjective Objective { get; private set; }

    //[CachedData]
    MobileParty ILocatable<MobileParty>.NextLocatable { get; set; }

    //[SaveableProperty(1019)]
    public PartyAi Ai { get; private set; }

    //[SaveableProperty(1020)]
    public PartyBase Party { get; private set; }

    //[SaveableProperty(1023)]
    public bool IsActive { get; set; }

    public float LastCachedSpeed => this._lastCalculatedFinalSpeed;

    public bool IsDisorganized
    {
      get => this._isDisorganized;
      set
      {
        float valueInHours;
        if (value)
        {
          if (!this._isDisorganized)
            this.UpdateVersionNo();
          valueInHours = BOFCampaign.Current.Models.PartyImpairmentModel.GetDisorganizedStateDuration(this, this.MapEvent != null && (this.MapEvent.IsRaid || this.MapEvent.IsSiegeAssault));
          this._isDisorganized = true;
        }
        else
        {
          if (this._isDisorganized)
            this.UpdateVersionNo();
          valueInHours = -1f;
          this._isDisorganized = false;
        }
        this._disorganizedUntilTime = CampaignTime.HoursFromNow(valueInHours);
      }
    }

    public bool IsCurrentlyUsedByAQuest => this._isCurrentlyUsedByAQuest;

    //[SaveableProperty(1050)]
    public AiBehavior ShortTermBehavior { get; private set; }

    //[SaveableProperty(1052)]
    public PartyBase AiBehaviorObject { get; private set; }

    //[SaveableProperty(1958)]
    public bool IsPartyTradeActive { get; private set; }

    public int PartyTradeGold
    {
      get => this._partyTradeGold;
      set => this._partyTradeGold = MathF.Max(value, 0);
    }

    //[SaveableProperty(1957)]
    public int PartyTradeTaxGold { get; private set; }

    //[SaveableProperty(1960)]
    public CampaignTime StationaryStartTime { get; private set; }

    //[CachedData]
    public bool ForceAiNoPathMode { get; set; }

    //[CachedData]
    public int PathBegin { get; private set; }

    //[CachedData]
    public NavigationPath Path { get; private set; }

    //[CachedData]
    public int VersionNo { get; private set; }

    //[SaveableProperty(1080)]
    public bool ShouldJoinPlayerBattles { get; set; }

    //[SaveableProperty(1082)]
    public bool IsAlerted { get; private set; }

    //[SaveableProperty(1081)]
    public bool IsDisbanding { get; set; }

    public Settlement CurrentSettlement
    {
      get => this._currentSettlement;
      set
      {
        if (value == this._currentSettlement)
          return;
        if (this._currentSettlement != null)
          this._currentSettlement.RemoveMobileParty(this);
        this._currentSettlement = value;
        if (this._currentSettlement != null)
        {
          this._currentSettlement.AddMobileParty(this);
          this.LastVisitedSettlement = value;
        }
        this._numberOfFleeingsAtLastTravel = 0;
        foreach (MobileParty attachedParty in this._attachedParties)
          attachedParty.CurrentSettlement = value;
        this.Party.Visuals?.SetMapIconAsDirty();
      }
    }

    public Settlement HomeSettlement
    {
      get
      {
        Settlement customHomeSettlement = this._customHomeSettlement;
        if (customHomeSettlement != null)
          return customHomeSettlement;
        return this._partyComponent?.HomeSettlement;
      }
    }

    public void SetCustomHomeSettlement(Settlement customHomeSettlement) => this._customHomeSettlement = customHomeSettlement;

    public MobileParty AttachedTo
    {
      get => this._attachedTo;
      set
      {
        if (this._attachedTo == value)
          return;
        this.SetAttachedToInternal(value);
      }
    }

    private void SetAttachedToInternal(MobileParty value)
    {
      if (this._attachedTo != null)
      {
        this._attachedTo.RemoveAttachedPartyInternal(this);
        if (this.Party.MapEventSide != null)
        {
          this.Party.MapEventSide.HandleMapEventEndForPartyInternal(this.Party);
          this.Party.MapEventSide = (MapEventSide) null;
        }
        if (this.BesiegerCamp != null)
          this.BesiegerCamp = (BesiegerCamp) null;
        this.OnAttachedToRemoved();
      }
      this._attachedTo = value;
      if (this._attachedTo != null)
      {
        this._attachedTo.AddAttachedPartyInternal(this);
        this.Party.MapEventSide = this._attachedTo.Party.MapEventSide;
        this.BesiegerCamp = this._attachedTo.BesiegerCamp;
        this.CurrentSettlement = this._attachedTo.CurrentSettlement;
      }
      this.Party.Visuals?.SetMapIconAsDirty();
    }

    private void AddAttachedPartyInternal(MobileParty mobileParty)
    {
      if (this._attachedParties == null)
      {
        this._attachedParties = new List<MobileParty>();
        this.AttachedParties = new MBReadOnlyList<MobileParty>(this._attachedParties);
      }
      this._attachedParties.Add(mobileParty);
      if (CampaignEventDispatcher.Instance == null)
        return;
      CampaignEventDispatcher.Instance.OnPartyAttachedAnotherParty(mobileParty);
    }

    private void RemoveAttachedPartyInternal(MobileParty mobileParty) => this._attachedParties.Remove(mobileParty);

    private void OnAttachedToRemoved()
    {
      this._errorPosition += this.ArmyPositionAdder;
      this.ArmyPositionAdder = Vec2.Zero;
      if (this.CurrentSettlement != null)
        this.SetMoveGoToSettlement(this.CurrentSettlement);
      else
        this.SetMoveModeHold();
    }

    public Army Army
    {
      get => this._army;
      set
      {
        if (this._army == value)
          return;
        this.UpdateVersionNo();
        if (this._army != null)
          this._army.OnRemovePartyInternal(this);
        this._army = value;
        if (value == null)
        {
          if (this != MobileParty.MainParty || !(Game.Current.GameStateManager.ActiveState is MapState))
            return;
          ((MapState) Game.Current.GameStateManager.ActiveState).OnLeaveArmy();
        }
        else
          this._army.OnAddPartyInternal(this);
      }
    }

    public BesiegerCamp BesiegerCamp
    {
      get => this._besiegerCamp;
      set
      {
        if (this._besiegerCamp == value)
          return;
        if (this._besiegerCamp != null)
          this.OnPartyLeftSiegeInternal();
        this._besiegerCamp = value;
        if (this._besiegerCamp != null)
          this.OnPartyJoinedSiegeInternal();
        foreach (MobileParty attachedParty in this._attachedParties)
          attachedParty.BesiegerCamp = value;
        this.Party.Visuals?.SetMapIconAsDirty();
      }
    }

    internal void ConsiderMapEventsAndSiegesInternal(IFaction factionToConsiderAgainst)
    {
      if (this.Army != null && this.Army.Kingdom != this.MapFaction)
        this.Army = (Army) null;
      if (this.CurrentSettlement != null)
      {
        IFaction mapFaction = this.CurrentSettlement.MapFaction;
        if (mapFaction != null && mapFaction.IsAtWarWith(this.MapFaction) && this.IsRaiding || this.IsMainParty && (PlayerEncounter.Current.ForceRaid || PlayerEncounter.Current.ForceSupplies || PlayerEncounter.Current.ForceVolunteers))
          return;
      }
      if (this.Party.MapEventSide != null)
      {
        IFaction mapFaction1 = this.Party.MapEventSide.OtherSide.MapFaction;
        IFaction mapFaction2 = this.Party.MapEventSide.MapFaction;
        if (mapFaction1 == null || !mapFaction1.IsAtWarWith(this.MapFaction) && mapFaction1 == factionToConsiderAgainst)
        {
          if (this.Party == PartyBase.MainParty && PlayerEncounter.Current != null)
            PlayerEncounter.Current.SetPlayerEncounterInterruptedByPeace();
          this.Party.MapEventSide = (MapEventSide) null;
        }
        else if (mapFaction2 == null || mapFaction2.IsAtWarWith(this.MapFaction) && mapFaction1 == factionToConsiderAgainst)
          this.Party.MapEventSide = (MapEventSide) null;
        if (this.Party == PartyBase.MainParty && PlayerEncounter.Current != null && PlayerEncounter.Battle != null && !PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && PlayerEncounter.Current.IsPlayerEncounterInterruptedByPeace && Game.Current.GameStateManager.ActiveState is MapState)
          PlayerEncounter.Finish();
      }
      BattleSideEnum battleSideEnum = PlayerEncounter.Battle != null ? PlayerEncounter.Battle.PlayerSide : BattleSideEnum.None;
      if (this.BesiegerCamp != null || PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.SiegeEvent != null)
      {
        Settlement settlement = this.BesiegerCamp?.SiegeEvent.BesiegedSettlement ?? PlayerEncounter.EncounterSettlement;
        MobileParty mobileParty = this.BesiegerCamp?.BesiegerParty ?? PlayerEncounter.EncounterSettlement.SiegeEvent.BesiegerCamp.BesiegerParty;
        IFaction mapFaction3 = settlement.MapFaction;
        IFaction mapFaction4 = mobileParty?.MapFaction;
        if (mapFaction3 == null || !mapFaction3.IsAtWarWith(this.MapFaction) && mapFaction3 == factionToConsiderAgainst)
        {
          if (this.Party == PartyBase.MainParty && battleSideEnum == BattleSideEnum.None)
            GameMenu.ActivateGameMenu("hostile_action_end_by_peace");
          else if (PlayerEncounter.Current != null && (PlayerEncounter.EncounteredParty == this.Party || PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.SiegeEvent != null && PlayerEncounter.EncounterSettlement.SiegeEvent == this.SiegeEvent) && this.BesiegerCamp.BesiegerParty != null && this.BesiegerCamp.BesiegerParty.Party == this.Party)
            PlayerEncounter.Current.SetPlayerEncounterInterruptedByPeace();
          this.BesiegerCamp = (BesiegerCamp) null;
        }
        else if (mapFaction4 == null || mapFaction4.IsAtWarWith(this.MapFaction) && mapFaction3 == factionToConsiderAgainst)
          this.BesiegerCamp = (BesiegerCamp) null;
      }
      if (this.CurrentSettlement != null)
      {
        IFaction mapFaction = this.CurrentSettlement.MapFaction;
        if (mapFaction != null && mapFaction == factionToConsiderAgainst && mapFaction.IsAtWarWith(this.MapFaction))
        {
          if (this.IsMainParty && !this.IsRaiding)
          {
            if (!GameStateManager.Current.ActiveState.IsMission && this.CurrentSettlement.IsFortification)
              GameMenu.SwitchToMenu("fortification_crime_rating");
          }
          else if (this.Army == null || this.Army.LeaderParty == this)
          {
            Settlement currentSettlement = this.CurrentSettlement;
            LeaveSettlementAction.ApplyForParty(this);
            SetPartyAiAction.GetActionForPatrollingAroundSettlement(this, currentSettlement);
          }
        }
      }
      if (this.Party != PartyBase.MainParty || PlayerEncounter.Current == null)
        return;
      if (PlayerEncounter.EncounteredBattle != null)
      {
        MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
        if (encounteredBattle.PlayerSide != BattleSideEnum.None)
        {
          if (!encounteredBattle.GetLeaderParty(encounteredBattle.PlayerSide.GetOppositeSide()).MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
            PlayerEncounter.Current.SetPlayerEncounterInterruptedByPeace();
        }
        else if (!encounteredBattle.InvolvedParties.Any<PartyBase>((Func<PartyBase, bool>) (x => x.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))))
          PlayerEncounter.Current.SetPlayerEncounterInterruptedByPeace();
      }
      else if (PlayerEncounter.PlayerIsDefender && PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.IsUnderSiege && !PlayerEncounter.EncounterSettlement.SiegeEvent.BesiegerCamp.BesiegerParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
        PlayerEncounter.Current.SetPlayerEncounterInterruptedByPeace();
      if (!PlayerEncounter.Current.IsPlayerEncounterInterruptedByPeace)
        return;
      if (Game.Current.GameStateManager.ActiveState is MapState)
        PlayerEncounter.Finish();
      else
        GameMenu.ActivateGameMenu("hostile_action_end_by_peace");
    }

    public AiBehavior DefaultBehavior
    {
      get => this._defaultBehavior;
      private set
      {
        if (this._defaultBehavior == value)
          return;
        this._defaultBehavior = value;
        this._defaultBehaviorNeedsUpdate = true;
        if (this == MobileParty.MainParty || this.BesiegedSettlement == null || value == AiBehavior.BesiegeSettlement || value == AiBehavior.EscortParty || value == AiBehavior.AssaultSettlement)
          return;
        this.ResetBesiegedSettlement();
      }
    }

    //[SaveableProperty(1070)]
    public Hero Scout { get; private set; }

    public void SetPartyScout(Hero hero)
    {
      this.RemoveHeroPerkRole(hero);
      this.Scout = hero;
    }

    //[SaveableProperty(1071)]
    public Hero Quartermaster { get; private set; }

    public void SetPartyQuartermaster(Hero hero)
    {
      this.RemoveHeroPerkRole(hero);
      this.Quartermaster = hero;
    }

    //[SaveableProperty(1072)]
    public Hero Engineer { get; private set; }

    public void SetPartyEngineer(Hero hero)
    {
      this.RemoveHeroPerkRole(hero);
      this.Engineer = hero;
    }

    //[SaveableProperty(1073)]
    public Hero Surgeon { get; private set; }

    public Hero EffectiveScout => this.Scout == null || this.Scout.PartyBelongedTo != this ? this.LeaderHero : this.Scout;

    public Hero EffectiveQuartermaster => this.Quartermaster == null || this.Quartermaster.PartyBelongedTo != this ? this.LeaderHero : this.Quartermaster;

    public void SetPartySurgeon(Hero hero)
    {
      this.RemoveHeroPerkRole(hero);
      this.Surgeon = hero;
    }

    public Hero EffectiveEngineer => this.Engineer == null || this.Engineer.PartyBelongedTo != this ? this.LeaderHero : this.Engineer;

    public Settlement TargetSettlement
    {
      get => this._targetSettlement;
      private set
      {
        if (value == this._targetSettlement)
          return;
        this._targetSettlement = value;
        this._defaultBehaviorNeedsUpdate = true;
      }
    }

    public Vec2 TargetPosition
    {
      get => this._targetPosition;
      private set
      {
        if (!(this._targetPosition != value))
          return;
        this._targetPosition = value;
        this._defaultBehaviorNeedsUpdate = true;
      }
    }

    public MobileParty TargetParty
    {
      get => this._targetParty;
      private set
      {
        if (value == this._targetParty)
          return;
        this._targetParty = value;
        this._defaultBehaviorNeedsUpdate = true;
      }
    }

    public MobileParty()
    {
      this._isVisible = false;
      this.IsActive = true;
      this._isCurrentlyUsedByAQuest = false;
      this.Party = new PartyBase(this);
      this.AtCampMode = false;
      this.InitMembers();
      this.InitCached();
      this.Initialize();
    }

    public Hero EffectiveSurgeon => this.Surgeon == null || this.Surgeon.PartyBelongedTo != this ? this.LeaderHero : this.Surgeon;

    private void InitMembers()
    {
      this._lastTargetedParties = new List<MobileParty>();
      if (this._attachedParties != null)
        return;
      this._attachedParties = new List<MobileParty>();
      this.AttachedParties = new MBReadOnlyList<MobileParty>(this._attachedParties);
    }

    public Hero LeaderHero => this.PartyComponent?.Leader;

    public Hero Owner => this._partyComponent?.PartyOwner;

    private void InitializeMobilePartyWithPartyTemplate(
      PartyTemplateObject pt,
      Vec2 position,
      int troopNumberLimit)
    {
      if (troopNumberLimit != 0)
        this.FillPartyStacks(pt, troopNumberLimit);
      this.CreateFigure(position, 0.0f);
      this.SetMoveModeHold();
    }

    public void InitializeMobilePartyAtPosition(
      PartyTemplateObject pt,
      Vec2 position,
      int troopNumberLimit = -1)
    {
      this.InitializeMobilePartyWithPartyTemplate(pt, position, troopNumberLimit);
    }

    public void InitializeMobilePartyAroundPosition(
      TroopRoster memberRoster,
      TroopRoster prisonerRoster,
      Vec2 position,
      float spawnRadius,
      float minSpawnRadius = 0.0f)
    {
      position = MobilePartyHelper.FindReachablePointAroundPosition(this.Party, position, spawnRadius, minSpawnRadius);
      this.InitializeMobilePartyWithRosterInternal(memberRoster, prisonerRoster, position);
    }

    public void InitializeMobilePartyAroundPosition(
      PartyTemplateObject pt,
      Vec2 position,
      float spawnRadius,
      float minSpawnRadius = 0.0f,
      int troopNumberLimit = -1)
    {
      position = MobilePartyHelper.FindReachablePointAroundPosition(this.Party, position, spawnRadius, minSpawnRadius);
      this.InitializeMobilePartyWithPartyTemplate(pt, position, troopNumberLimit);
    }

    public override void Initialize()
    {
      base.Initialize();
      this.Aggressiveness = 1f;
      this.Ai = new PartyAi(this);
      this._formationPosition.x = 10000f;
      this._formationPosition.y = 10000f;
      while ((double) this._formationPosition.LengthSquared > 0.360000014305115 || (double) this._formationPosition.LengthSquared < 0.219999998807907)
        this._formationPosition = new Vec2((float) ((double) MBRandom.RandomFloat * 1.20000004768372 - 0.600000023841858), (float) ((double) MBRandom.RandomFloat * 1.20000004768372 - 0.600000023841858));
      CampaignEventDispatcher.Instance.OnPartyVisibilityChanged(this.Party);
    }

    private void InitializeMobilePartyWithRosterInternal(
      TroopRoster memberRoster,
      TroopRoster prisonerRoster,
      Vec2 position)
    {
      this.MemberRoster.Add(memberRoster);
      this.PrisonRoster.Add(prisonerRoster);
      this.CreateFigure(position, 0.0f);
      this.SetMoveModeHold();
    }

    public Clan ActualClan
    {
      get => this._actualClan;
      set
      {
        if (this._actualClan == value)
          return;
        if (this._actualClan != null && value != null && this.PartyComponent is WarPartyComponent partyComponent)
          partyComponent.OnClanChange(this._actualClan, value);
        this._actualClan = value;
      }
    }

    internal void StartUp()
    {
      this._nextTargetPosition = this.Position2D;
      this._aiPathLastFace = PathFaceRecord.NullFaceRecord;
      this.MoveTargetPoint = this.Position2D;
      this.ForceAiNoPathMode = false;
    }

    public float RecentEventsMorale
    {
      get => this._moraleDueToEvents;
      set
      {
        this._moraleDueToEvents = value;
        if ((double) this._moraleDueToEvents < -50.0)
        {
          this._moraleDueToEvents = -50f;
        }
        else
        {
          if ((double) this._moraleDueToEvents <= 50.0)
            return;
          this._moraleDueToEvents = 50f;
        }
      }
    }

    public override string ToString() => this.StringId + ":" + (object) this.Party.Index;

    public void InitializeMobilePartyAtPosition(
      TroopRoster memberRoster,
      TroopRoster prisonerRoster,
      Vec2 position)
    {
      this.InitializeMobilePartyWithRosterInternal(memberRoster, prisonerRoster, position);
    }

    public float Morale
    {
      get
      {
        float resultNumber = BOFCampaign.Current.Models.PartyMoraleModel.GetEffectivePartyMorale(this).ResultNumber;
        return (double) resultNumber < 0.0 ? 0.0f : ((double) resultNumber > 100.0 ? 100f : resultNumber);
      }
    }

    TextObject ITrackableBase.GetName() => this.Name;

    public ExplainedNumber MoraleExplained => BOFCampaign.Current.Models.PartyMoraleModel.GetEffectivePartyMorale(this, true);

    public void ChangePartyLeader(Hero newLeader)
    {
      if (newLeader == null || !this.MemberRoster.Contains(newLeader.CharacterObject))
      {
        Debug.FailedAssert(newLeader?.Name.ToString() + " is not a member of " + (object) this.Name + "!\nParty leader did not change.", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MobileParty.cs", nameof (ChangePartyLeader), 995);
      }
      else
      {
        if (this.IsMainParty && this._partyComponent is LordPartyComponent partyComponent2)
          partyComponent2.ChangePartyOwner(newLeader);
        this.PartyComponent.ChangePartyLeader(newLeader);
      }
    }

    public void RemovePartyLeader()
    {
      if (this.LeaderHero == null)
        return;
      this.PartyComponent.ChangePartyLeader((Hero) null);
    }

    private void RecoverPositionsForNavMeshUpdate()
    {
      if (this.Position2D.IsNonZero() && !PartyBase.IsPositionOkForTraveling(this.Position2D))
        this.Position2D = SettlementHelper.FindNearestVillage(toMapPoint: ((IMapPoint) this)).GatePosition;
      if (this.MoveTargetPoint.IsNonZero() && !PartyBase.IsPositionOkForTraveling(this.MoveTargetPoint))
        this.MoveTargetPoint = this.Position2D;
      if (this.DefaultBehavior == AiBehavior.Hold || (!this.TargetPosition.IsNonZero() || PartyBase.IsPositionOkForTraveling(this.TargetPosition)) && (!this._nextTargetPosition.IsNonZero() || PartyBase.IsPositionOkForTraveling(this._nextTargetPosition)) && (!this.AiBehaviorTarget.IsNonZero() || PartyBase.IsPositionOkForTraveling(this.AiBehaviorTarget)))
        return;
      this.ForceDefaultBehaviorUpdate();
      this.SetMoveModeHold();
      this.SetNavigationModeHold();
    }

    public void OnGameInitialized()
    {
      this.RecoverPositionsForNavMeshUpdate();
      Campaign current = BOFCampaign.Current;
      if (current.MapSceneWrapper != null)
        this.CurrentNavigationFace = current.MapSceneWrapper.GetFaceIndex(this.Position2D);
      this.UpdatePathModeWithPosition(this.MoveTargetPoint);
      this.ComputePath(this.MoveTargetPoint);
      if (this == BOFCampaign.Current.CampaignObjectManager.Find<MobileParty>((Predicate<MobileParty>) (x => x.StringId == this.StringId)))
        return;
      DestroyPartyAction.Apply((PartyBase) null, this);
    }

    protected override void PreAfterLoad()
    {
      if (this._actualClan == null)
      {
        Hero owner = this.Party.Owner;
        if (owner != null)
        {
          if (owner.IsNoble || owner.CharacterObject.Occupation == Occupation.Lord || this.LeaderHero == this.Party.Owner)
          {
            if (!this.IsVillager && this.IsLordParty || this._partyFlagObsolete != null && !this._partyFlagObsolete.Contains("VillagerParty") && this._partyFlagObsolete.Contains("LordParty"))
            {
              this._actualClan = owner.Clan;
              owner.IsNoble = true;
            }
          }
          else if (owner.IsMinorFactionHero)
            this._actualClan = owner.Clan;
          else if (!owner.IsNotable && owner.Clan != null && owner.Clan.IsBanditFaction)
            this._actualClan = owner.Clan;
        }
      }
      if (this.IsBandit && this._actualClan == null)
        this._actualClan = Clan.BanditFactions.FirstOrDefault<Clan>();
      if (this.IsGarrison || this.IsMilitia)
        this._actualClan = (Clan) null;
      if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.6.3", ApplicationVersionGameType.Singleplayer) && (this.IsMilitia || this.IsGarrison))
      {
        Settlement settlement = this.IsMilitia ? (this._partyComponent as MilitiaPartyComponent).Settlement : (this._partyComponent as GarrisonPartyComponent).Settlement;
        for (int index = this.PrisonRoster.Count - 1; index >= 0; --index)
        {
          TroopRosterElement elementCopyAtIndex = this.PrisonRoster.GetElementCopyAtIndex(index);
          Hero heroObject = elementCopyAtIndex.Character.HeroObject;
          if (heroObject != null)
          {
            this.PrisonRoster.RemoveTroop(elementCopyAtIndex.Character);
            TakePrisonerAction.Apply(settlement.Party, heroObject);
          }
        }
        settlement.Party.PrisonRoster.Add(this.PrisonRoster);
        this.PrisonRoster.Clear();
      }
      if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.6.6", ApplicationVersionGameType.Singleplayer) && this.Army != null && !this.Army.LeaderPartyAndAttachedParties.Contains<MobileParty>(this) && this.SiegeEvent != null)
      {
        this.Army = (Army) null;
        this.SetMoveModeHold();
      }
      if (ApplicationVersion.FromParametersFile(ApplicationVersionGameType.Singleplayer) <= ApplicationVersion.FromString("e1.6.4", ApplicationVersionGameType.Singleplayer) && this.IsMainParty)
        this.PaymentLimit = BOFCampaign.Current.Models.PartyWageModel.MaxWage;
      if (!(MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.6.5", ApplicationVersionGameType.Singleplayer)) || !this.IsMainParty || Hero.MainHero.PartyBelongedTo == null || Hero.MainHero.PartyBelongedTo == this)
        return;
      Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(CharacterObject.PlayerCharacter, -1);
      if (this.MemberRoster.Contains(CharacterObject.PlayerCharacter))
        this.MemberRoster.AddToCounts(CharacterObject.PlayerCharacter, -1);
      this.MemberRoster.AddToCounts(CharacterObject.PlayerCharacter, 1, true);
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
      this.InitMembers();
      this.InitCached();
      if (this.IsMainParty && this.AiBehaviorObject != null && this.AiBehaviorObject.MobileParty != null && this.AiBehaviorObject.MobileParty.Army != null && this.CurrentSettlement != null)
      {
        this.ResetAiBehaviorObject();
        this.SetMoveModeHold();
        this.Position2D = this.CurrentSettlement.GatePosition;
      }
      if (this._attachedTo == null)
        return;
      this._attachedTo.AddAttachedPartyInternal(this);
    }

    private void InitCached()
    {
      this._lastTargetedParties = new List<MobileParty>();
      this._partiesAroundPosition = new MobilePartiesAroundPositionList();
      this.Path = new NavigationPath();
      this._aiPathLastFace = PathFaceRecord.NullFaceRecord;
      this.ForceAiNoPathMode = false;
      this._curTick = 3;
      ((ILocatable<MobileParty>) this).LocatorNodeIndex = -1;
      this._partySizeRatioLastCheckVersion = -1;
      this._latestUsedPaymentRatio = -1;
      this._cachedPartySizeRatio = 1f;
      this.VersionNo = 0;
      this._partyPureSpeedLastCheckVersion = -1;
      this._itemRosterVersionNo = -1;
    }

    protected override void AfterLoad()
    {
      this.Party.AfterLoad();
      if (this.PartyComponent == null && this._partyFlagObsolete != null)
      {
        string partyFlagObsolete = this._partyFlagObsolete;
        this._partyFlagObsolete = (string) null;
        if (partyFlagObsolete.Contains("CaravanParty"))
        {
          if (this.ActualClan == Clan.PlayerClan)
          {
            CharacterObject characterAtIndex = this.MemberRoster.GetCharacterAtIndex(0);
            if (characterAtIndex.IsHero)
              this._partyComponent = (PartyComponent) new CaravanPartyComponent(this.HomeSettlement, this.Party.Owner, characterAtIndex.HeroObject);
          }
          else
            this._partyComponent = (PartyComponent) new CaravanPartyComponent(this.HomeSettlement, this.Party.Owner, (Hero) null);
        }
        else if (partyFlagObsolete.Contains("VillagerParty"))
          this._partyComponent = (PartyComponent) new VillagerPartyComponent(this.HomeSettlement.Village);
        else if (partyFlagObsolete.Contains("GarrisonParty"))
          this._partyComponent = (PartyComponent) new GarrisonPartyComponent(this.HomeSettlement);
        else if (partyFlagObsolete.Contains("CommonAreaParty"))
        {
          CommonArea commonArea = this.HomeSettlement.CommonAreas.FirstOrDefault<CommonArea>((Func<CommonArea, bool>) (x => x.CommonAreaPartyObsolete == this));
          if (commonArea != null)
          {
            commonArea.CommonAreaPartyComponent = (CommonAreaPartyComponent) null;
            this._partyComponent = (PartyComponent) new CommonAreaPartyComponent(this.HomeSettlement, this.Party.Owner, commonArea);
          }
          else
            DestroyPartyAction.Apply((PartyBase) null, this);
        }
        else if (partyFlagObsolete.Contains("MilitiaParty"))
          this._partyComponent = (PartyComponent) new MilitiaPartyComponent(this.HomeSettlement);
        else if (partyFlagObsolete.Contains("LordParty"))
        {
          CharacterObject characterObject = (CharacterObject) null;
          if (this.MemberRoster.Count > 0)
            characterObject = this.MemberRoster.GetCharacterAtIndex(0);
          this._partyComponent = characterObject == null ? (PartyComponent) new LordPartyComponent(this.ActualClan.Leader, (Hero) null) : (PartyComponent) new LordPartyComponent(characterObject.HeroObject ?? this.ActualClan.Leader, characterObject.HeroObject);
        }
        else
        {
          Clan actualClan = this.ActualClan;
          if ((actualClan != null ? (actualClan.IsBanditFaction ? 1 : 0) : 0) != 0)
          {
            bool isBossParty = partyFlagObsolete.Contains("IsBanditBossParty");
            Hideout hideout = (Hideout) null;
            if (this.HomeSettlement != null)
            {
              hideout = this.HomeSettlement.Hideout;
            }
            else
            {
              float maximumDistance = float.MaxValue;
              foreach (Settlement toSettlement in Settlement.All)
              {
                float distance;
                if (toSettlement.Culture == this.ActualClan.Culture && toSettlement.IsHideout && BOFCampaign.Current.Models.MapDistanceModel.GetDistance(this, toSettlement, maximumDistance, out distance))
                {
                  maximumDistance = distance;
                  hideout = toSettlement.Hideout;
                }
              }
            }
            this._partyComponent = (PartyComponent) new BanditPartyComponent(hideout, isBossParty);
          }
        }
      }
      else if (this.PartyComponent != null && this.PartyComponent is CommonAreaPartyComponent partyComponent2 && partyComponent2.CommonArea == null)
      {
        if (this.CurrentSettlement != null && !this.CurrentSettlement.Parties.Contains(this))
          this.CurrentSettlement.AddMobileParty(this);
        this._partyComponent = (PartyComponent) null;
        DestroyPartyAction.Apply((PartyBase) null, this);
      }
      this.UpdatePartyComponentFlags();
      this.PartyComponent?.Initialize(this);
      if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("e1.6.6", ApplicationVersionGameType.Singleplayer) && (this.IsGarrison || this.IsMilitia))
        this.Party.SetCustomOwner((Hero) null);
      if (!this._disorganizedUntilTime.IsPast)
        this._isDisorganized = true;
      if (this.IsGarrison && this.MapEvent == null && this.SiegeEvent == null && this.TargetParty != null && this.CurrentSettlement != null)
        this.SetMoveModeHold();
      if (this.CurrentSettlement != null && !this.CurrentSettlement.Parties.Contains(this))
      {
        this.CurrentSettlement.AddMobileParty(this);
        foreach (MobileParty attachedParty in this._attachedParties)
        {
          if (this.Army.LeaderParty != this)
            this.CurrentSettlement.AddMobileParty(attachedParty);
        }
      }
      if (MBSaveLoad.LastLoadedGameVersion <= ApplicationVersion.FromString("e1.6.5", ApplicationVersionGameType.Singleplayer))
      {
        if (this.PartyComponent is LordPartyComponent && this.MemberRoster.TotalManCount > 0)
        {
          CharacterObject characterAtIndex = this.MemberRoster.GetCharacterAtIndex(0);
          if (characterAtIndex != null && characterAtIndex.IsHero)
            this.PartyComponent.ChangePartyLeader(characterAtIndex.HeroObject);
        }
        else if (this.PartyComponent is CaravanPartyComponent && this.Owner.Clan == Clan.PlayerClan && this.MemberRoster.TotalManCount > 0)
        {
          CharacterObject characterAtIndex = this.MemberRoster.GetCharacterAtIndex(0);
          if (characterAtIndex != null && characterAtIndex.IsHero)
            this.PartyComponent.ChangePartyLeader(characterAtIndex.HeroObject);
        }
      }
      if (this.IsMainParty || this.LeaderHero == null || this.LeaderHero.PartyBelongedTo == this || this.IsDisbanding)
        return;
      DisbandPartyAction.ApplyDisband(this);
      this.PartyComponent.ChangePartyLeader((Hero) null);
    }

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    {
      this._partyFlagObsolete = (string) objectLoadData.GetDataValueBySaveId(1011);
      if (this._partyFlagObsolete != null)
      {
        string partyFlagObsolete = this._partyFlagObsolete;
        if (partyFlagObsolete.Contains("IsDisbanding"))
          this.IsDisbanding = true;
        if (partyFlagObsolete.Contains("IsAlerted"))
          this.IsAlerted = true;
        if (partyFlagObsolete.Contains("ShouldJoinPlayerBattles"))
          this.ShouldJoinPlayerBattles = true;
      }
      if (!this.IsDisbanding || !this._isCurrentlyUsedByAQuest)
      {
        ApplicationVersion applicationVersion = metaData.GetApplicationVersion();
        if (applicationVersion.ApplicationVersionType == ApplicationVersionType.EarlyAccess && applicationVersion.Major == 1 && applicationVersion.Minor <= 5)
          this.CustomName = (TextObject) null;
      }
      if (!this.IsMainParty || this.Party.Owner == this.LeaderHero || !(this._partyComponent is LordPartyComponent partyComponent))
        return;
      partyComponent.ChangePartyOwner(this.LeaderHero);
    }

    internal void OnFinishLoadState()
    {
      if (!this.IsCommonAreaParty)
        BOFCampaign.Current.MobilePartyLocator.UpdateParty(this);
      if (this.IsLordParty && this.LeaderHero == null && !this.IsDisbanding && !this.IsMainParty)
        DisbandPartyAction.ApplyDisband(this);
      if (this.IsMainParty)
      {
        if (this.IsDisbanding)
          DisbandPartyAction.CancelDisband(this);
        foreach (TroopRosterElement troopRosterElement in this.MemberRoster.data)
        {
          if (troopRosterElement.Character == CharacterObject.PlayerCharacter && troopRosterElement.Number > 1)
          {
            this.MemberRoster.RemoveTroop(troopRosterElement.Character, troopRosterElement.Number);
            this.MemberRoster.AddToCounts(troopRosterElement.Character, 1);
            break;
          }
        }
        if (this.LeaderHero != null && (this._partyComponent is LordPartyComponent partyComponent5 ? partyComponent5.Owner : (Hero) null) != this.LeaderHero && this._partyComponent is LordPartyComponent partyComponent6)
          partyComponent6.ChangePartyOwner(this.LeaderHero);
        if (this.Army != null && this.Army.LeaderParty == this && this.Army.ArmyOwner != this.LeaderHero)
          this.Army.ArmyOwner = this.LeaderHero;
      }
      ApplicationVersion loadedGameVersion = MBSaveLoad.LastLoadedGameVersion;
      if (this.CustomName == null || this.IsDisbanding || this._isCurrentlyUsedByAQuest || loadedGameVersion.ApplicationVersionType != ApplicationVersionType.EarlyAccess || loadedGameVersion.Major != 1 || loadedGameVersion.Minor > 5)
        return;
      this.CustomName = (TextObject) null;
    }

    int ILocatable<MobileParty>.LocatorNodeIndex
    {
      get => this._locatorNodeIndex;
      set => this._locatorNodeIndex = value;
    }

    private void RemoveOneOfLastTargettedPartiesWithProbability(float probabilityToRemove)
    {
      int index1 = -1;
      for (int index2 = 0; index2 < this._lastTargetedParties.Count; ++index2)
      {
        if ((double) MBRandom.RandomFloat < (double) probabilityToRemove)
        {
          index1 = index2;
          break;
        }
      }
      if (index1 < 0)
        return;
      this._lastTargetedParties.RemoveAt(index1);
    }

    //[CachedData]
    public PathFaceRecord CurrentNavigationFace { get; private set; }

    internal void HourlyTick()
    {
      if (!this.IsActive)
        return;
      if (this._doNotAttackMainParty > 0)
        --this._doNotAttackMainParty;
      if (this.LeaderHero != null && this.CurrentSettlement != null && this.CurrentSettlement == this.LeaderHero.HomeSettlement)
        ++this.LeaderHero.PassedTimeAtHomeSettlement;
      if (this.LeaderHero != null)
        this.WorkSkills();
      if (this._numberOfFleeingsAtLastTravel > 0 && (double) MBRandom.RandomFloat < 0.0399999991059303)
        --this._numberOfFleeingsAtLastTravel;
      this.RemoveOneOfLastTargettedPartiesWithProbability(0.2f);
    }

    internal void DailyTick()
    {
      foreach (TroopRosterElement troop in this.MemberRoster.GetTroopRoster())
      {
        ExplainedNumber effectiveDailyExperience = BOFCampaign.Current.Models.PartyTrainingModel.GetEffectiveDailyExperience(this, troop);
        if (!troop.Character.IsHero)
          this.Party.MemberRoster.AddXpToTroop(MathF.Round(effectiveDailyExperience.ResultNumber * (float) troop.Number), troop.Character);
      }
      if (this.HasPerk(DefaultPerks.Bow.Trainer) && !this.IsDisbanding)
      {
        Hero hero = (Hero) null;
        int num = int.MaxValue;
        foreach (TroopRosterElement troopRosterElement in this.MemberRoster.GetTroopRoster())
        {
          if (troopRosterElement.Character.IsHero)
          {
            int skillValue = troopRosterElement.Character.HeroObject.GetSkillValue(DefaultSkills.Bow);
            if (skillValue < num)
            {
              num = skillValue;
              hero = troopRosterElement.Character.HeroObject;
            }
          }
        }
        hero?.AddSkillXp(DefaultSkills.Bow, DefaultPerks.Bow.Trainer.PrimaryBonus);
      }
      this.RecentEventsMorale -= this.RecentEventsMorale * 0.1f;
      if (this.LeaderHero != null)
        this.LeaderHero.PassedTimeAtHomeSettlement *= 0.9f;
      if (!this.IsActive || this.MapEvent != null || this == MobileParty.MainParty)
        return;
      BOFCampaign.Current.PartyUpgrader.UpgradeReadyTroops(this.Party);
    }

    internal void TickForMobileParty(
      ref MobileParty.TickLocalVariables variables,
      float dt,
      float realDt)
    {
      this.FillTickLocaleVariables(ref variables);
      this.ComputeNextMoveDistance(ref variables, dt);
      this.UpdateStationaryTimer(ref variables);
      this.SetIsDisorganizedState();
      this.DoAiPathMode(ref variables);
      this.DoUpdatePosition(ref variables, dt, realDt);
      this.DoErrorCorrections(ref variables, realDt);
    }

    internal void FillTickLocaleVariables(ref MobileParty.TickLocalVariables variables)
    {
      variables.isArmyMember = this.Army != null && this.Army.LeaderParty.AttachedParties.Contains(this);
      variables.hasMapEvent = this.MapEvent != null;
      variables.currentPosition = this.Position2D;
      variables.lastCurrentPosition = this.Position2D;
    }

    internal void ComputeNextMoveDistance(ref MobileParty.TickLocalVariables variables, float dt)
    {
      if ((double) dt > 0.0)
      {
        ++this._curTick;
        if (this._curTick > 2)
        {
          this._curTick = this._curTick == 4 ? MBRandom.RandomInt(3) : 0;
          this._cachedComputedSpeed = this.ComputeSpeed();
        }
        variables.nextMoveDistance = this._cachedComputedSpeed * dt;
      }
      else
        variables.nextMoveDistance = 0.0f;
    }

    internal void UpdateStationaryTimer(ref MobileParty.TickLocalVariables variables)
    {
      if (!this.Party.IsMobile)
        return;
      if (!this.IsMoving)
      {
        if (!(this.StationaryStartTime == CampaignTime.Never))
          return;
        this.StationaryStartTime = CampaignTime.Now;
      }
      else
        this.StationaryStartTime = CampaignTime.Never;
    }

    private void SetIsDisorganizedState()
    {
      if (this.BesiegedSettlement != null)
        this.IsDisorganized = true;
      else if (this.MapEvent != null)
      {
        this.IsDisorganized = true;
      }
      else
      {
        if (!this._isDisorganized || !this._disorganizedUntilTime.IsPast)
          return;
        this.IsDisorganized = false;
      }
    }

    internal void DoAiPathMode(ref MobileParty.TickLocalVariables variables)
    {
      if (variables.hasMapEvent || variables.isArmyMember)
        return;
      this.TickTrack();
      this.DoAIMove();
      if (!this._aiPathMode)
        return;
      bool flag;
      do
      {
        flag = false;
        this._nextTargetPosition = this.Path[this.PathBegin];
        float lengthSquared = (this._nextTargetPosition - variables.currentPosition).LengthSquared;
        if ((double) lengthSquared < (double) variables.nextMoveDistance * (double) variables.nextMoveDistance)
        {
          flag = true;
          variables.nextMoveDistance -= MathF.Sqrt(lengthSquared);
          variables.lastCurrentPosition = variables.currentPosition;
          variables.currentPosition = this._nextTargetPosition;
          ++this.PathBegin;
        }
      }
      while (flag && this.PathBegin < this.Path.Size);
      if (this.PathBegin < this.Path.Size)
        return;
      this._aiPathMode = false;
      this._aiPathNeeded = false;
      if (this.Path.Size <= 0)
        return;
      variables.currentPosition = variables.lastCurrentPosition;
      this._nextTargetPosition = this.Path[this.Path.Size - 1];
    }

    internal void DoUpdatePosition(
      ref MobileParty.TickLocalVariables variables,
      float dt,
      float realDt)
    {
      bool flag1 = variables.hasMapEvent || variables.isArmyMember && this.Army.LeaderParty.MapEvent != null;
      variables.nextPosition = new Vec2();
      Vec2 vec2_1 = variables.currentPosition + this.EventPositionAdder + this.ArmyPositionAdder;
      Vec2 vec2_2;
      Vec2 vec2_3;
      if (variables.isArmyMember)
      {
        if (variables.hasMapEvent && this.MapEvent.IsFieldBattle || flag1 && this.Army.LeaderParty.MapEvent != null && this.Army.LeaderParty.MapEvent.IsFieldBattle)
        {
          vec2_2 = Vec2.Zero;
        }
        else
        {
          Vec2 vec2_4 = flag1 ? this.Army.LeaderParty.Position2D : this.Army.LeaderParty._nextTargetPosition;
          Vec2 finalTargetPosition;
          this.Army.LeaderParty.GetTargetPositionAndFace(out finalTargetPosition, out PathFaceRecord _, out bool _);
          Vec2 vec2_5;
          if ((double) (vec2_4 - this.Army.LeaderParty.Position2D).LengthSquared >= 0.00250000017695129)
          {
            vec2_3 = vec2_4 - this.Army.LeaderParty.Position2D;
            vec2_5 = vec2_3.Normalized();
          }
          else
            vec2_5 = Vec2.FromRotation(this.Army.LeaderParty.Party.AverageBearingRotation);
          Vec2 armyFacing = vec2_5;
          Vec2 parentUnitF = armyFacing.TransformToParentUnitF(this.Army.GetRelativePositionForParty(this, armyFacing));
          vec2_2 = vec2_4 + parentUnitF - vec2_1;
          vec2_3 = finalTargetPosition + parentUnitF - vec2_1;
          if ((double) vec2_3.LengthSquared < 0.0100000007078052 || (double) vec2_2.LengthSquared < (this.Party.Visuals.EntityMoving ? 9.99999974737875E-05 : 0.0100000007078052))
            vec2_2 = Vec2.Zero;
          vec2_3 = vec2_2.LeftVec();
          vec2_3 = vec2_3.Normalized();
          float num = vec2_3.DotProduct(this.Army.LeaderParty.Position2D + parentUnitF - vec2_1);
          vec2_2.RotateCCW((double) num < 0.0 ? MathF.Max(num * 2f, -0.7853982f) : MathF.Min(num * 2f, 0.7853982f));
        }
      }
      else
        vec2_2 = (variables.hasMapEvent ? this.Party.MapEvent.Position : this._nextTargetPosition) - vec2_1;
      float num1 = vec2_2.Normalize();
      this.Party.Visuals.EntityMoving = false;
      if ((double) num1 < (double) variables.nextMoveDistance)
        variables.nextMoveDistance = num1;
      if (this.BesiegedSettlement != null)
        return;
      if ((double) variables.nextMoveDistance > 0.0 | flag1)
      {
        bool flag2 = false;
        Vec2 vec2_6 = this.Bearing;
        if ((double) num1 > 0.0)
        {
          flag2 = true;
          vec2_6 = vec2_2;
          if (!variables.isArmyMember || !flag1)
            this.Bearing = vec2_6;
          this.Party.Visuals.EntityMoving = !flag1;
        }
        else if (variables.isArmyMember & flag1)
        {
          vec2_6 = this.Army.LeaderParty.Bearing;
          this.Bearing = vec2_6;
          flag2 = true;
        }
        if (flag2)
        {
          vec2_3 = this.Bearing;
          this.Party.AverageBearingRotation += MBMath.WrapAngle(vec2_3.RotationInRadians - this.Party.AverageBearingRotation) * MathF.Min((flag1 ? realDt : dt) * 30f, 1f);
          this.Party.AverageBearingRotation = MBMath.WrapAngle(this.Party.AverageBearingRotation);
        }
        variables.nextPosition = variables.currentPosition + vec2_6 * variables.nextMoveDistance;
      }
      else
      {
        if ((double) num1 <= 0.100000001490116)
          return;
        this.Party.Visuals.EntityMoving = true;
      }
    }

    internal void DoErrorCorrections(ref MobileParty.TickLocalVariables variables, float realDt)
    {
      float lengthSquared = this._errorPosition.LengthSquared;
      if ((double) lengthSquared <= 0.0)
        return;
      if ((double) lengthSquared <= 4.0 * (double) realDt * (double) realDt)
      {
        this._errorPosition = Vec2.Zero;
      }
      else
      {
        this.Party.Visuals.EntityMoving = true;
        this._errorPosition -= this._errorPosition.Normalized() * (2f * realDt);
      }
    }

    internal void TickForMobileParty2(ref MobileParty.TickLocalVariables variables, float realDt)
    {
      PathFaceRecord currentNavigationFace;
      if ((double) variables.nextMoveDistance > 0.0 && this.BesiegedSettlement == null && (!variables.hasMapEvent || variables.isArmyMember))
      {
        if (variables.isArmyMember)
        {
          this.ArmyPositionAdder += variables.nextPosition - this.Position2D;
        }
        else
        {
          PathFaceRecord nextPathFaceRecord = variables.nextPathFaceRecord;
          currentNavigationFace = this.CurrentNavigationFace;
          if (currentNavigationFace.IsValid())
          {
            if (this.CurrentNavigationFace.FaceIslandIndex == nextPathFaceRecord.FaceIslandIndex)
            {
              this.SetPositionWithFaceRecord(variables.nextPosition, variables.nextPathFaceRecord);
              if (this.Party.MobileParty.Army != null && this.Party.MobileParty.Army.LeaderParty != this && this.Party.MobileParty.Army.LeaderParty.CurrentSettlement == this.CurrentSettlement && this.ShortTermTargetParty == this.Party.MobileParty.Army.LeaderParty && (double) (this.Position2D - this.Party.MobileParty.Army.LeaderParty.Position2D).LengthSquared < 0.5)
              {
                this.Party.MobileParty.Army.AddPartyToMergedParties(this);
                if (this.Party.MobileParty == MobileParty.MainParty)
                  BOFCampaign.Current.CameraFollowParty = this.Party.MobileParty.Army.LeaderParty.Party;
                CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
              }
            }
            else if (!this._aiPathNotFound && !this.ForceAiNoPathMode)
              this._aiPathNeeded = true;
            else if (!this._aiPathNotFound && this.ForceAiNoPathMode)
              this.SetNavigationModeHold();
          }
        }
      }
      if (this.Party == PartyBase.MainParty && BOFCampaign.Current.GetSimplifiedTimeControlMode() != CampaignTimeControlMode.Stop)
      {
        currentNavigationFace = this.CurrentNavigationFace;
        if (currentNavigationFace.IsValid())
        {
          float seeingRange = MobileParty.MainParty.SeeingRange;
          foreach (MobileParty mobileParty in MobileParty.FindPartiesAroundPosition(MobileParty.MainParty.Position2D, seeingRange + 25f, (Func<MobileParty, bool>) (x => !x.IsMilitia && !x.IsGarrison)))
            mobileParty.Party.UpdateVisibilityAndInspected(seeingRange);
          foreach (Settlement settlement in Settlement.FindSettlementsAroundPosition(MobileParty.MainParty.Position2D, seeingRange + 25f))
            settlement.Party.UpdateVisibilityAndInspected(seeingRange);
        }
      }
      if (!this.Party.Visuals.IsVisibleOrFadingOut())
        return;
      MatrixFrame identity = MatrixFrame.Identity;
      identity.origin = this.GetVisualPosition();
      if (variables.isArmyMember)
      {
        MatrixFrame frame = this.Party.Visuals.GetFrame();
        Vec2 vec2 = identity.origin.AsVec2 - frame.origin.AsVec2;
        float length = vec2.Length;
        if ((double) length / (double) realDt > 20.0)
        {
          identity.rotation.RotateAboutUp(this.Party.AverageBearingRotation);
          this._lastVisualSpeed = this.ComputeSpeed();
        }
        else
        {
          this._lastVisualSpeed = length / (float) ((double) realDt * 0.25 * 1.29999995231628);
          float a = MBMath.LerpRadians(frame.rotation.f.AsVec2.RotationInRadians, (vec2 + Vec2.FromRotation(this.Party.AverageBearingRotation) * 0.01f).RotationInRadians, 6f * realDt, 0.03f * realDt, 10f * realDt);
          identity.rotation.RotateAboutUp(a);
        }
      }
      else
      {
        identity.rotation.RotateAboutUp(this.Party.AverageBearingRotation);
        this._lastVisualSpeed = this.ComputeSpeed();
      }
      this.Party.Visuals.SetFrame(ref identity);
    }

    private void TickTrack()
    {
      if ((double) this._lastTrackPosition.DistanceSquared(this.Position2D) <= (double) this._distanceToNextTrackSq)
        return;
      this._distanceToNextTrackSq = (float) (4.0 + 2.0 * (double) MBRandom.RandomFloat);
      if (this != MobileParty.MainParty)
      {
        Vec2 trackPosition = 0.5f * (this._lastTrackPosition + this.Position2D);
        Vec2 trackDirection = this.Position2D - this._lastTrackPosition;
        double num = (double) trackDirection.Normalize();
        BOFCampaign.Current.AddTrack(this, trackPosition, trackDirection);
      }
      else
        this._distanceToNextTrackSq = 30f;
      this._lastTrackPosition = this.Position2D;
    }

    private void WorkSkills()
    {
      int getHourOfDay = CampaignTime.Now.GetHourOfDay;
      if (this.IsMoving)
      {
        if (getHourOfDay % 4 != 1)
          return;
        this.CheckAthletics();
        this.CheckRiding();
        this.CheckScouting();
      }
      else
      {
        if (this.CurrentSettlement == null || getHourOfDay % 2 != 1)
          return;
        this.WorkTrainingSkill();
      }
    }

    private void WorkTrainingSkill()
    {
      int upgradeXpFromTraining = BOFCampaign.Current.Models.PartyTrainingModel.GetHourlyUpgradeXpFromTraining(this);
      int maxTrainedTroops = BOFCampaign.Current.Models.PartyTrainingModel.GetMaxTrainedTroops(this);
      bool flag = false;
      if (upgradeXpFromTraining <= 0)
        return;
      for (int index = 0; index < maxTrainedTroops; ++index)
      {
        int upgadeTarget = this.FindUpgadeTarget();
        if (upgadeTarget >= 0)
        {
          int elementXp = this.MemberRoster.GetElementXp(upgadeTarget);
          this.MemberRoster.AddXpToTroopAtIndex(upgradeXpFromTraining, upgadeTarget);
          if (this.MemberRoster.GetElementXp(upgadeTarget) > elementXp && !flag)
            flag = true;
        }
      }
    }

    public Vec2 EventPositionAdder
    {
      get => this._eventPositionAdder;
      set
      {
        this._errorPosition += this._eventPositionAdder;
        this._eventPositionAdder = value;
        this._errorPosition -= this._eventPositionAdder;
      }
    }

    private void CheckRiding()
    {
      if (this == MobileParty.MainParty)
      {
        foreach (TroopRosterElement troopRosterElement in this.MemberRoster.GetTroopRoster())
        {
          if (troopRosterElement.Character.IsHero && !troopRosterElement.Character.Equipment.Horse.IsEmpty)
            SkillLevelingManager.OnTravelOnHorse(troopRosterElement.Character.HeroObject, this.ComputeSpeed());
        }
      }
      else
      {
        if (this.LeaderHero == null || this.LeaderHero.CharacterObject.Equipment.Horse.IsEmpty)
          return;
        SkillLevelingManager.OnTravelOnHorse(this.LeaderHero, this.ComputeSpeed());
      }
    }

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

    private void CheckAthletics()
    {
      if (this == MobileParty.MainParty)
      {
        foreach (TroopRosterElement troopRosterElement in this.MemberRoster.GetTroopRoster())
        {
          if (troopRosterElement.Character.IsHero && troopRosterElement.Character.Equipment.Horse.IsEmpty)
            SkillLevelingManager.OnTravelOnFoot(troopRosterElement.Character.HeroObject, this.ComputeSpeed());
        }
      }
      else
      {
        if (this.LeaderHero == null || !this.LeaderHero.CharacterObject.Equipment.Horse.IsEmpty)
          return;
        SkillLevelingManager.OnTravelOnFoot(this.LeaderHero, this.ComputeSpeed());
      }
    }

    private void CheckScouting()
    {
      if (this == MobileParty.MainParty || this.LeaderHero == null)
        return;
      SkillLevelingManager.OnTravel(this.LeaderHero, this.IsCaravan, BOFCampaign.Current.MapSceneWrapper.GetFaceTerrainType(this.CurrentNavigationFace));
    }

    public Vec2 Position2D
    {
      get => this._position2D;
      set
      {
        if (!(this._position2D != value))
          return;
        this._position2D = value;
        Campaign current = BOFCampaign.Current;
        if (!this.IsCommonAreaParty)
          current.MobilePartyLocator.UpdateParty(this);
        if (this.Army != null && this.Army.LeaderParty == this && this.BesiegedSettlement == null)
        {
          foreach (MobileParty attachedParty in this.Army.LeaderParty.AttachedParties)
          {
            if (attachedParty != this.Army.LeaderParty)
            {
              attachedParty.ArmyPositionAdder += attachedParty.Position2D - this._position2D;
              attachedParty.Position2D = value;
            }
          }
        }
        if (current.MapSceneWrapper == null)
          return;
        this.CurrentNavigationFace = current.MapSceneWrapper.GetFaceIndex(this._position2D);
      }
    }

    public void SetPositionWithFaceRecord(Vec2 value, PathFaceRecord record)
    {
      if (!(this._position2D != value))
        return;
      this._position2D = value;
      Campaign current = BOFCampaign.Current;
      if (!this.IsCommonAreaParty)
        current.MobilePartyLocator.UpdateParty(this);
      if (this.Army != null && this.Army.LeaderParty == this && this.BesiegedSettlement == null)
      {
        foreach (MobileParty attachedParty in this.Army.LeaderParty.AttachedParties)
        {
          if (attachedParty != this.Army.LeaderParty)
          {
            attachedParty.ArmyPositionAdder += attachedParty.Position2D - this._position2D;
            attachedParty.Position2D = value;
          }
        }
      }
      this.CurrentNavigationFace = record;
    }

    private int FindUpgadeTarget()
    {
      for (int index1 = 0; index1 < 3; ++index1)
      {
        int index2 = MBRandom.RandomInt(this.MemberRoster.Count);
        CharacterObject characterAtIndex = this.MemberRoster.GetCharacterAtIndex(index2);
        if (!characterAtIndex.IsHero && characterAtIndex.UpgradeTargets.Length != 0)
          return index2;
      }
      return -1;
    }

    internal void ForceDefaultBehaviorUpdate() => this._defaultBehaviorNeedsUpdate = true;

    public void SetCustomName(TextObject name) => this.CustomName = name;

    public void SetMoveModeHold()
    {
      this.DefaultBehavior = AiBehavior.Hold;
      this.ShortTermBehavior = AiBehavior.Hold;
      this.TargetSettlement = (Settlement) null;
      this.TargetParty = (MobileParty) null;
    }

    public void SetPartyUsedByQuest(bool isActivelyUsed) => this._isCurrentlyUsedByAQuest = isActivelyUsed;

    public void ResetTargetParty() => this.TargetParty = (MobileParty) null;

    public void SetMoveEngageParty(MobileParty party)
    {
      this.DefaultBehavior = AiBehavior.EngageParty;
      this.TargetParty = party;
      this.TargetSettlement = (Settlement) null;
    }

    public void SetMoveGoAroundParty(MobileParty party)
    {
      this.DefaultBehavior = AiBehavior.GoAroundParty;
      this.TargetParty = party;
      this.TargetSettlement = (Settlement) null;
    }

    public void SetMoveGoToSettlement(Settlement settlement)
    {
      this.DefaultBehavior = AiBehavior.GoToSettlement;
      this.TargetSettlement = settlement;
      this.TargetParty = (MobileParty) null;
      this.TargetPosition = settlement.GatePosition;
      this._numberOfFleeingsAtLastTravel = 0;
    }

    public void SetMoveGoToPoint(Vec2 point)
    {
      this.DefaultBehavior = AiBehavior.GoToPoint;
      this.TargetPosition = point;
      this.TargetSettlement = (Settlement) null;
      this.TargetParty = (MobileParty) null;
    }

    public bool IsInspected
    {
      get => this.Army != null && this.Army == MobileParty.MainParty.Army || this._isInspected;
      set => this._isInspected = value;
    }

    public void SetMoveEscortParty(MobileParty mobileParty)
    {
      this.DefaultBehavior = AiBehavior.EscortParty;
      this.TargetParty = mobileParty;
      this.TargetSettlement = (Settlement) null;
    }

    public void SetMovePatrolAroundPoint(Vec2 point)
    {
      this.TargetParty = (MobileParty) null;
      this.TargetSettlement = (Settlement) null;
      this.DefaultBehavior = AiBehavior.PatrolAroundPoint;
      this.TargetPosition = point;
      this._aiBehaviorResetNeeded = true;
    }

    public void SetMovePatrolAroundSettlement(Settlement settlement)
    {
      this.SetMovePatrolAroundPoint(settlement.GatePosition);
      this.TargetSettlement = settlement;
      this.TargetParty = (MobileParty) null;
    }

    public void SetMoveRaidSettlement(Settlement settlement)
    {
      this.DefaultBehavior = AiBehavior.RaidSettlement;
      this.TargetSettlement = settlement;
      this.TargetParty = (MobileParty) null;
    }

    public void SetMoveBesiegeSettlement(Settlement settlement)
    {
      if (this.BesiegedSettlement != null && this.BesiegedSettlement != settlement)
        this.ResetBesiegedSettlement();
      this.DefaultBehavior = AiBehavior.BesiegeSettlement;
      this.TargetSettlement = settlement;
      this.TargetParty = (MobileParty) null;
    }

    public void SetMoveDefendSettlement(Settlement settlement)
    {
      this.DefaultBehavior = AiBehavior.DefendSettlement;
      this.TargetSettlement = settlement;
      this.TargetParty = (MobileParty) null;
    }

    public void SetInititave(float attackInitiative, float avoidInitiative, float hoursUntilReset)
    {
      if (this == MobileParty.MainParty)
        return;
      this._attackInitiative = attackInitiative;
      this._avoidInitiative = avoidInitiative;
      this._initiativeRestoreTime = CampaignTime.HoursFromNow(hoursUntilReset);
    }

    public void IgnoreForHours(float hours) => this._ignoredUntilTime = CampaignTime.HoursFromNow(hours);

    public void IgnoreByOtherPartiesTill(CampaignTime time) => this._ignoredUntilTime = time;

    public void ResetBesiegedSettlement()
    {
      if (this._resetStarted)
        return;
      this._resetStarted = true;
      if (this.BesiegerCamp != null)
        this.BesiegerCamp = (BesiegerCamp) null;
      this._resetStarted = false;
    }

    public Vec2 GetPosition2D => this.Position2D;

    public int TotalWage => (int) BOFCampaign.Current.Models.PartyWageModel.GetTotalWage(this).ResultNumber;

    public ExplainedNumber TotalWageExplained => BOFCampaign.Current.Models.PartyWageModel.GetTotalWage(this, true);

    public MapEvent MapEvent => this.Party.MapEvent;

    internal void OnRemovedFromArmyInternal()
    {
      this.Ai.SetAIState(AIState.Undefined);
      this.ResetTargetParty();
      if (!this.IsActive || this.LeaderHero == null)
        return;
      if (this.BesiegedSettlement != null && this.Army.LeaderParty != this)
      {
        if (this.BesiegedSettlement.SiegeEvent.BesiegerCamp.SiegeParties.Contains<PartyBase>(this.Party))
          return;
        if (this != MobileParty.MainParty)
        {
          if (this.MapEvent != null)
            return;
          this.SetMoveBesiegeSettlement(this.BesiegedSettlement);
        }
        else
          this.SetMoveModeHold();
      }
      else if (this.CurrentSettlement == null)
        this.SetMoveModeHold();
      else if (this.CurrentSettlement != null && this.CurrentSettlement == this.BesiegedSettlement || this.MapEvent != null)
        LeaveSettlementAction.ApplyForParty(this);
      else
        this.SetMoveGoToSettlement(this.CurrentSettlement);
    }

    private void OnRemoveParty()
    {
      this.Army = (Army) null;
      this.CurrentSettlement = (Settlement) null;
      this.AttachedTo = (MobileParty) null;
      this.BesiegerCamp = (BesiegerCamp) null;
      List<Settlement> settlementList = new List<Settlement>();
      if (this.CurrentSettlement != null)
        settlementList.Add(this.CurrentSettlement);
      else if ((this.IsGarrison || this.IsMilitia || this.IsBandit || this.IsVillager) && this.HomeSettlement != null)
        settlementList.Add(this.HomeSettlement);
      this.PartyComponent?.Finish();
      this.ActualClan = (Clan) null;
      BOFCampaign.Current.CampaignObjectManager.RemoveMobileParty(this);
      foreach (Settlement settlement in settlementList)
        settlement.OnRelatedPartyRemoved(this);
    }

    public void SetPartyObjective(MobileParty.PartyObjective objective) => this.Objective = objective;

    public void UpdateVersionNo() => ++this.VersionNo;

    private void ComputeCachedPureSpeed(out bool isBaseSpeedUpdated)
    {
      isBaseSpeedUpdated = false;
      if (this._partyPureSpeedLastCheckVersion == this.VersionNo && this._itemRosterVersionNo == this.Party.ItemRoster.VersionNo)
        return;
      this._pureSpeedExplainer = BOFCampaign.Current.Models.PartySpeedCalculatingModel.CalculatePureSpeed(this);
      this._pureSpeed = this._pureSpeedExplainer.ResultNumber;
      this.UpdateCachedPureSpeed();
      isBaseSpeedUpdated = true;
    }

    private void UpdateCachedPureSpeed()
    {
      this._partyPureSpeedLastCheckVersion = this.VersionNo;
      this._itemRosterVersionNo = this.Party.ItemRoster.VersionNo;
    }

    public TroopRoster MemberRoster => this.Party.MemberRoster;

    public float GetCachedPureSpeed()
    {
      this.ComputeCachedPureSpeed(out bool _);
      return this._pureSpeed;
    }

    public TroopRoster PrisonRoster => this.Party.PrisonRoster;

    public float ComputeSpeed()
    {
      if (this.Army != null && this.Army.LeaderParty.AttachedParties.Contains(this))
      {
        Vec2 armyFacing = ((this.Army.LeaderParty.MapEvent != null ? this.Army.LeaderParty.Position2D : this.Army.LeaderParty._nextTargetPosition) - this.Army.LeaderParty.Position2D).Normalized();
        float num = this.Bearing.DotProduct(this.Army.LeaderParty.Position2D + armyFacing.TransformToParentUnitF(this.Army.GetRelativePositionForParty(this, armyFacing)) - this.VisualPosition2DWithoutError);
        return this.Army.LeaderParty.ComputeSpeed() * MBMath.ClampFloat((float) (1.0 + (double) num * 1.0), 0.7f, 1.3f);
      }
      bool isBaseSpeedUpdated;
      this.ComputeCachedPureSpeed(out isBaseSpeedUpdated);
      ulong num1 = (ulong) ((int) ((double) this.Party.MobileParty.Position2D.x * 100.0) % 100) * 100000UL + (ulong) ((double) this.Party.MobileParty.Position2D.y * 100.0);
      Hero leaderHero = this.LeaderHero;
      bool flag = !this.IsActive || leaderHero == null || leaderHero.PartyBelongedToAsPrisoner != null;
      bool isNight = BOFCampaign.Current.IsNight;
      if (isBaseSpeedUpdated || (long) this._partyLastCheckPositionVersion != (long) num1 || this._partyLastCheckIsPrisoner != flag || this._partyLastCheckAtNight != isNight)
      {
        this._partyLastCheckPositionVersion = num1;
        this._partyLastCheckIsPrisoner = flag;
        this._partyLastCheckAtNight = isNight;
        this._lastCalculatedFinalSpeed = BOFCampaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(this, this._pureSpeedExplainer).ResultNumber;
      }
      return this.Army == null && this.DefaultBehavior == AiBehavior.EscortParty && this.TargetParty != null && (double) this.LastCachedSpeed > (double) this.TargetParty.LastCachedSpeed ? this.TargetParty.ComputeSpeed() : this._lastCalculatedFinalSpeed;
    }

    public float ComputeVisualSpeed() => this._lastVisualSpeed;

    public ItemRoster ItemRoster => this.Party.ItemRoster;

    public bool IsSpotted() => this.IsVisible;

    internal void OnEventEnded(MapEvent mapEvent)
    {
      if (mapEvent == null || mapEvent.DiplomaticallyFinished || !mapEvent.IsRaid && !mapEvent.IsFieldBattle && !mapEvent.IsSiegeAssault && !mapEvent.IsSiegeOutside)
        return;
      this.SetIsDisorganizedState();
    }

    public bool IsMainParty => this == MobileParty.MainParty;

    public int AddElementToMemberRoster(
      CharacterObject element,
      int numberToAdd,
      bool insertAtFront = false)
    {
      return this.Party.AddElementToMemberRoster(element, numberToAdd, insertAtFront);
    }

    public int AddPrisoner(CharacterObject element, int numberToAdd) => this.Party.AddPrisoner(element, numberToAdd);

    public IFaction MapFaction => this.ActualClan == null ? (this.Party.Owner == null ? (this.HomeSettlement == null ? (this.LeaderHero != null ? this.LeaderHero.MapFaction : (IFaction) CampaignData.NeutralFaction) : this.HomeSettlement.OwnerClan.MapFaction) : (this.Party.Owner != Hero.MainHero ? (!this.Party.Owner.IsNotable ? (!this.IsMilitia && !this.IsGarrison && !this.IsVillager || this.HomeSettlement?.OwnerClan == null ? (this.IsCaravan || this.IsBanditBossParty ? this.Party.Owner.MapFaction : (!this._isCurrentlyUsedByAQuest || this.Party.Owner == null ? (this.LeaderHero != null ? this.LeaderHero.MapFaction : (IFaction) CampaignData.NeutralFaction) : this.Party.Owner.MapFaction)) : this.HomeSettlement.OwnerClan.MapFaction) : this.Party.Owner.HomeSettlement.MapFaction) : this.Party.Owner.MapFaction)) : this.ActualClan.MapFaction;

    public void SetDoNotAttackMainParty(int hours)
    {
      if (hours <= this._doNotAttackMainParty)
        return;
      this._doNotAttackMainParty = hours;
      this.AddCookie<DoNotAttackMainPartyCookie>(CampaignTime.Hours((float) hours));
    }

    public TextObject ArmyName => this.Army == null || this.Army.LeaderParty != this ? this.Name : this.Army.Name;

    private bool CanAvoid(MobileParty targetParty) => (double) targetParty.Aggressiveness > 0.00999999977648258 && targetParty.Party.IsMobile || targetParty.IsGarrison;

    public SiegeEvent SiegeEvent => this.BesiegerCamp?.SiegeEvent;

    internal bool CanAttack(MobileParty targetParty) => ((targetParty != MobileParty.MainParty || !Game.Current.CheatMode ? 1 : (CampaignCheats.MainPartyIsAttackable ? 1 : 0)) & (targetParty != MobileParty.MainParty ? (true ? 1 : 0) : (this.GetCookie<DoNotAttackMainPartyCookie>() == null ? 1 : 0))) != 0;

    public float Food => (float) this.Party.RemainingFoodPercentage * 0.01f + (float) this.TotalFoodAtInventory;

    public Vec3 GetLogicalPosition()
    {
      float height = 0.0f;
      BOFCampaign.Current.MapSceneWrapper.GetHeightAtPoint(this.Position2D, ref height);
      return new Vec3(this.Position2D.x, this.Position2D.y, height);
    }

    public int TotalFoodAtInventory => this.ItemRoster.TotalFood;

    public Vec3 GetVisualPosition()
    {
      float height = 0.0f;
      BOFCampaign.Current.MapSceneWrapper.GetHeightAtPoint(this.VisualPosition2DWithoutError + this._errorPosition, ref height);
      return new Vec3(this.Position2D.x + this.EventPositionAdder.x + this.ArmyPositionAdder.x + this._errorPosition.x, this.Position2D.y + this.EventPositionAdder.y + this.ArmyPositionAdder.y + this._errorPosition.y, height);
    }

    public float TotalWeightCarried => this.ItemRoster.TotalWeight;

    private float CalculateStanceScore(MobileParty otherParty)
    {
      if (FactionManager.IsAtWarAgainstFaction(this.MapFaction, otherParty.MapFaction))
        return 1f;
      return FactionManager.IsAlliedWithFaction(this.MapFaction, otherParty.MapFaction) ? -1f : 0.0f;
    }

    public float SeeingRange => BOFCampaign.Current.Models.MapVisibilityModel.GetPartySpottingRange(this).ResultNumber;

    private bool IsEnemy(PartyBase party) => FactionManager.IsAtWarAgainstFaction(party.MapFaction, this.MapFaction);

    public bool NeedTargetReset => this._numberOfFleeingsAtLastTravel >= 6;

    private bool IsEnemy(MobileParty assaulterParty) => this.IsEnemy(assaulterParty.Party);

    public int NumberOfFleeingsAtLastTravel => this._numberOfFleeingsAtLastTravel;

    private bool IsAlly(PartyBase party) => party.MapFaction == this.MapFaction;

    public Settlement BesiegedSettlement => this.BesiegerCamp?.SiegeEvent.BesiegedSettlement;

    private bool IsAlly(MobileParty party) => this.IsAlly(party.Party);

    private float AttackInitiative => !this._initiativeRestoreTime.IsPast ? this._attackInitiative : 1f;

    public float GetTotalStrengthWithFollowers()
    {
      PartyBase partyBase = this.Party.MobileParty.DefaultBehavior == AiBehavior.EscortParty ? this.Party.MobileParty.TargetParty.Party : this.Party;
      float num = partyBase.TotalStrength;
      if (partyBase.MobileParty.Army != null && partyBase.MobileParty == partyBase.MobileParty.Army.LeaderParty)
      {
        num = 0.0f;
        foreach (MobileParty party in partyBase.MobileParty.Army.Parties)
          num += party.Party.TotalStrength;
      }
      return num;
    }

    private float AvoidInitiative => !this._initiativeRestoreTime.IsPast ? this._avoidInitiative : 1f;

    private void FillPartyStacks(PartyTemplateObject pt, int troopNumberLimit = -1)
    {
      if (this.IsBandit)
      {
        float num1 = (float) (0.400000005960464 + 0.800000011920929 * (double) MiscHelper.GetGameProcess());
        int num2 = MBRandom.RandomInt(2);
        float num3 = num2 == 0 ? MBRandom.RandomFloat : (float) ((double) MBRandom.RandomFloat * (double) MBRandom.RandomFloat * (double) MBRandom.RandomFloat * 4.0);
        float num4 = num2 == 0 ? (float) ((double) num3 * 0.800000011920929 + 0.200000002980232) : 1f + num3;
        float randomFloat1 = MBRandom.RandomFloat;
        float randomFloat2 = MBRandom.RandomFloat;
        float randomFloat3 = MBRandom.RandomFloat;
        float f1 = pt.Stacks.Count > 0 ? (float) pt.Stacks[0].MinValue + num1 * num4 * randomFloat1 * (float) (pt.Stacks[0].MaxValue - pt.Stacks[0].MinValue) : 0.0f;
        float f2 = pt.Stacks.Count > 1 ? (float) pt.Stacks[1].MinValue + num1 * num4 * randomFloat2 * (float) (pt.Stacks[1].MaxValue - pt.Stacks[1].MinValue) : 0.0f;
        float f3 = pt.Stacks.Count > 2 ? (float) pt.Stacks[2].MinValue + num1 * num4 * randomFloat3 * (float) (pt.Stacks[2].MaxValue - pt.Stacks[2].MinValue) : 0.0f;
        this.AddElementToMemberRoster(pt.Stacks[0].Character, MBRandom.RoundRandomized(f1));
        if (pt.Stacks.Count > 1)
          this.AddElementToMemberRoster(pt.Stacks[1].Character, MBRandom.RoundRandomized(f2));
        if (pt.Stacks.Count <= 2)
          return;
        this.AddElementToMemberRoster(pt.Stacks[2].Character, MBRandom.RoundRandomized(f3));
      }
      else if (this.IsVillager)
      {
        for (int index = 0; index < pt.Stacks.Count; ++index)
          this.AddElementToMemberRoster(pt.Stacks[0].Character, troopNumberLimit);
      }
      else if (troopNumberLimit < 0)
      {
        float gameProcess = MiscHelper.GetGameProcess();
        for (int index = 0; index < pt.Stacks.Count; ++index)
        {
          int numberToAdd = (int) ((double) gameProcess * (double) (pt.Stacks[index].MaxValue - pt.Stacks[index].MinValue)) + pt.Stacks[index].MinValue;
          this.AddElementToMemberRoster(pt.Stacks[index].Character, numberToAdd);
        }
      }
      else
      {
        for (int index1 = 0; index1 < troopNumberLimit; ++index1)
        {
          int index2 = -1;
          float num5 = 0.0f;
          for (int index3 = 0; index3 < pt.Stacks.Count; ++index3)
            num5 += (float) ((!this.IsGarrison || !pt.Stacks[index3].Character.IsRanged ? (!this.IsGarrison || pt.Stacks[index3].Character.IsMounted ? 1.0 : 2.0) : 6.0) * ((double) (pt.Stacks[index3].MaxValue + pt.Stacks[index3].MinValue) / 2.0));
          float num6 = MBRandom.RandomFloat * num5;
          for (int index4 = 0; index4 < pt.Stacks.Count; ++index4)
          {
            num6 -= (float) ((!this.IsGarrison || !pt.Stacks[index4].Character.IsRanged ? (!this.IsGarrison || pt.Stacks[index4].Character.IsMounted ? 1.0 : 2.0) : 6.0) * ((double) (pt.Stacks[index4].MaxValue + pt.Stacks[index4].MinValue) / 2.0));
            if ((double) num6 < 0.0)
            {
              index2 = index4;
              break;
            }
          }
          if (index2 < 0)
            index2 = 0;
          this.AddElementToMemberRoster(pt.Stacks[index2].Character, 1);
        }
      }
    }

    public bool IsGoingToSettlement => this._defaultBehavior == AiBehavior.GoToSettlement;

    private void OnPartyJoinedSiegeInternal()
    {
      this._besiegerCamp.AddSiegePartyInternal(this);
      this._besiegerCamp.SetSiegeCampPartyPosition(this);
      this.SetIsDisorganizedState();
    }

    public MobileParty ShortTermTargetParty => this.AiBehaviorObject?.MobileParty;

    private void OnPartyLeftSiegeInternal()
    {
      this._besiegerCamp.RemoveSiegePartyInternal(this);
      this.EventPositionAdder = Vec2.Zero;
      this._errorPosition = Vec2.Zero;
      this._disorganizedUntilTime = CampaignTime.HoursFromNow(BOFCampaign.Current.Models.PartyImpairmentModel.GetDisorganizedStateDuration(this, true));
    }

    public Settlement ShortTermTargetSettlement => this.AiBehaviorObject?.Settlement;

    public void SetAsMainParty() => this.SetInititave(0.0f, 0.0f, 1E+08f);

    private Vec2 ShortTermTargetPosition => this.AiBehaviorTarget;

    public bool IsHolding => this.DefaultBehavior == AiBehavior.Hold;

    public bool HasPerk(PerkObject perk, bool checkSecondaryRole = false)
    {
      switch (checkSecondaryRole ? (int) perk.SecondaryRole : (int) perk.PrimaryRole)
      {
        case 2:
          return this.LeaderHero != null && (this.LeaderHero.Clan?.Leader?.GetPerkValue(perk) ?? false);
        case 4:
          return this.Army?.LeaderParty?.LeaderHero?.GetPerkValue(perk) ?? false;
        case 5:
          Hero leaderHero1 = this.LeaderHero;
          return leaderHero1 != null && leaderHero1.GetPerkValue(perk);
        case 7:
          Hero surgeon = this.Surgeon;
          if ((surgeon != null ? (surgeon.GetPerkValue(perk) ? 1 : 0) : 0) != 0)
            return true;
          Hero leaderHero2 = this.LeaderHero;
          return leaderHero2 != null && leaderHero2.GetPerkValue(perk);
        case 8:
          Hero engineer = this.Engineer;
          if ((engineer != null ? (engineer.GetPerkValue(perk) ? 1 : 0) : 0) != 0)
            return true;
          Hero leaderHero3 = this.LeaderHero;
          return leaderHero3 != null && leaderHero3.GetPerkValue(perk);
        case 9:
          Hero scout = this.Scout;
          if ((scout != null ? (scout.GetPerkValue(perk) ? 1 : 0) : 0) != 0)
            return true;
          Hero leaderHero4 = this.LeaderHero;
          return leaderHero4 != null && leaderHero4.GetPerkValue(perk);
        case 10:
          Hero quartermaster = this.Quartermaster;
          if ((quartermaster != null ? (quartermaster.GetPerkValue(perk) ? 1 : 0) : 0) != 0)
            return true;
          Hero leaderHero5 = this.LeaderHero;
          return leaderHero5 != null && leaderHero5.GetPerkValue(perk);
        case 11:
          foreach (TroopRosterElement troopRosterElement in this.MemberRoster.GetTroopRoster())
          {
            if (troopRosterElement.Character.IsHero && troopRosterElement.Character.HeroObject.GetPerkValue(perk))
              return true;
          }
          return false;
        case 12:
          Debug.FailedAssert("personal perk is called in party", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MobileParty.cs", nameof (HasPerk), 2994);
          Hero leaderHero6 = this.LeaderHero;
          return leaderHero6 != null && leaderHero6.GetPerkValue(perk);
        default:
          return false;
      }
    }

    public bool IsEngaging => this.DefaultBehavior == AiBehavior.EngageParty;

    public SkillEffect.PerkRole GetHeroPerkRole(Hero hero)
    {
      if (this.Engineer == hero)
        return SkillEffect.PerkRole.Engineer;
      if (this.Quartermaster == hero)
        return SkillEffect.PerkRole.Quartermaster;
      if (this.Surgeon == hero)
        return SkillEffect.PerkRole.Surgeon;
      return this.Scout == hero ? SkillEffect.PerkRole.Scout : SkillEffect.PerkRole.None;
    }

    public void RemoveHeroPerkRole(Hero hero)
    {
      if (this.Engineer == hero)
        this.Engineer = (Hero) null;
      if (this.Quartermaster == hero)
        this.Quartermaster = (Hero) null;
      if (this.Surgeon == hero)
        this.Surgeon = (Hero) null;
      if (this.Scout != hero)
        return;
      this.Scout = (Hero) null;
    }

    public bool IsRaiding => this.DefaultBehavior == AiBehavior.RaidSettlement;

    internal Hero GetRoleHolder(SkillEffect.PerkRole perkRole)
    {
      switch (perkRole)
      {
        case SkillEffect.PerkRole.PartyLeader:
          return this.LeaderHero;
        case SkillEffect.PerkRole.Surgeon:
          return this.Surgeon;
        case SkillEffect.PerkRole.Engineer:
          return this.Engineer;
        case SkillEffect.PerkRole.Scout:
          return this.Scout;
        case SkillEffect.PerkRole.Quartermaster:
          return this.Quartermaster;
        default:
          return (Hero) null;
      }
    }

    internal bool IsCurrentlyEngagingSettlement => this.ShortTermBehavior == AiBehavior.GoToSettlement || this.ShortTermBehavior == AiBehavior.RaidSettlement || this.ShortTermBehavior == AiBehavior.AssaultSettlement;

    public int GetNumDaysForFoodToLast()
    {
      int num = this.ItemRoster.TotalFood * 100;
      if (this == MobileParty.MainParty)
        num += this.Party.RemainingFoodPercentage;
      return (int) ((double) num / (100.0 * -(double) this.FoodChange));
    }

    internal bool IsCurrentlyEngagingParty => this.ShortTermBehavior == AiBehavior.EngageParty;

    public Vec3 GetPosition() => this.GetLogicalPosition();

    public bool IsCurrentlyGoingToSettlement => this.ShortTermBehavior == AiBehavior.GoToSettlement;

    public float GetTrackDistanceToMainAgent() => this.GetPosition().Distance(Hero.MainHero.GetPosition());

    public float PartySizeRatio
    {
      get
      {
        int versionNo = this.Party.MemberRoster.VersionNo;
        float cachedPartySizeRatio = this._cachedPartySizeRatio;
        if (this._partySizeRatioLastCheckVersion != versionNo || this == MobileParty.MainParty)
        {
          this._partySizeRatioLastCheckVersion = versionNo;
          this._cachedPartySizeRatio = (float) this.Party.NumberOfAllMembers / (float) this.Party.PartySizeLimit;
          cachedPartySizeRatio = this._cachedPartySizeRatio;
        }
        return cachedPartySizeRatio;
      }
    }

    public int LimitedPartySize
    {
      get
      {
        if (this.PaymentLimit <= 0 || this.UnlimitedWage)
          return this.Party.PartySizeLimit;
        int versionNo = this.Party.MemberRoster.VersionNo;
        int paymentLimit = this.Party.MobileParty.PaymentLimit;
        if (this._latestUsedPaymentRatio == paymentLimit && this != MobileParty.MainParty)
          return this._cachedPartySizeLimit;
        this._latestUsedPaymentRatio = paymentLimit;
        int characterWage = BOFCampaign.Current.Models.PartyWageModel.GetCharacterWage(3);
        int num = MathF.Max(1, MathF.Min((this.PaymentLimit - Math.Min(this.LeaderHero == null || this.Party.Owner == null || this.Party.Owner.Clan == null || this.LeaderHero == this.Party.Owner.Clan.Leader ? 0 : this.LeaderHero.CharacterObject.TroopWage, this.TotalWage)) / characterWage + 1, this.Party.PartySizeLimit));
        this._cachedPartySizeLimit = num;
        return num;
      }
    }

    public bool CheckTracked(BasicCharacterObject basicCharacter) => this.MemberRoster.GetTroopRoster().Any<TroopRosterElement>((Func<TroopRosterElement, bool>) (t => t.Character == basicCharacter)) || this.PrisonRoster.GetTroopRoster().Any<TroopRosterElement>((Func<TroopRosterElement, bool>) (t => t.Character == basicCharacter));

    public Vec2 VisualPosition2DWithoutError => this.Position2D + this.EventPositionAdder + this.ArmyPositionAdder;

    public void RemoveParty()
    {
      this.IsActive = false;
      this.IsVisible = false;
      Campaign current = BOFCampaign.Current;
      this.Party.Visuals?.OnPartyRemoved();
      this.AttachedTo = (MobileParty) null;
      this.BesiegerCamp = (BesiegerCamp) null;
      this.ReleaseHeroPrisoners();
      this.ItemRoster.Clear();
      this.MemberRoster.Reset();
      this.PrisonRoster.Reset();
      BOFCampaign.Current.MobilePartyLocator.RemoveParty(this);
      CampaignEventDispatcher.Instance.OnPartyRemoved(this.Party);
      GC.SuppressFinalize((object) this.Party);
      foreach (MobileParty mobileParty in current.MobileParties)
      {
        bool flag = false;
        if (mobileParty.AiBehaviorObject == this.Party)
        {
          mobileParty.ResetAiBehaviorObject();
          flag = true;
        }
        if (mobileParty.TargetParty != null && mobileParty.TargetParty == this)
        {
          mobileParty.ResetTargetParty();
          flag = true;
        }
        if (flag && mobileParty.TargetSettlement != null && (mobileParty.MapEvent == null || mobileParty.MapEvent.IsFinalized) && mobileParty.DefaultBehavior == AiBehavior.GoToSettlement)
        {
          Settlement targetSettlement = mobileParty.TargetSettlement;
          mobileParty.SetMoveModeHold();
          mobileParty.SetNavigationModeHold();
          mobileParty.SetMoveGoToSettlement(targetSettlement);
          flag = false;
        }
        if (flag)
        {
          mobileParty.SetMoveModeHold();
          mobileParty.SetNavigationModeHold();
        }
      }
      this.OnRemoveParty();
      this._customHomeSettlement = (Settlement) null;
    }

    public bool IsMoving
    {
      get
      {
        if (this.MapEvent != null || this.BesiegedSettlement != null || this.CurrentSettlement != null || this.ShortTermBehavior == AiBehavior.Hold)
          return false;
        return this.ShortTermBehavior != AiBehavior.GoToPoint || this.Position2D != this.TargetPosition;
      }
    }

    private void ReleaseHeroPrisoners()
    {
      for (int index = 0; index < this.PrisonRoster.Count; ++index)
      {
        if (this.PrisonRoster.GetElementNumber(index) > 0)
        {
          TroopRosterElement elementCopyAtIndex = this.PrisonRoster.GetElementCopyAtIndex(index);
          if (elementCopyAtIndex.Character.IsHero && !elementCopyAtIndex.Character.IsPlayerCharacter)
            EndCaptivityAction.ApplyByRemovedParty(elementCopyAtIndex.Character.HeroObject);
        }
      }
    }

    public bool ShouldBeIgnored => this._ignoredUntilTime.IsFuture;

    private void CreateFigure(Vec2 position, float spawnRadius)
    {
      Vec2 pointNearPosition = BOFCampaign.Current.MapSceneWrapper.GetAccessiblePointNearPosition(position, spawnRadius);
      this.Position2D = new Vec2(pointNearPosition.x, pointNearPosition.y);
      Vec2 vec2 = new Vec2(1f, 0.0f);
      float angleInRadians = (float) ((double) MBRandom.RandomFloat * 2.0 * 3.14159274101257);
      vec2.RotateCCW(angleInRadians);
      this.Bearing = vec2;
      this.Party.UpdateVisibilityAndInspected();
      this.Party.Visuals.OnStartup(this.Party);
      this.StartUp();
    }

    public MoveModeType PartyMoveMode => this._partyMoveMode;

    internal void SendPartyToReachablePointAroundPosition(
      Vec2 centerPosition,
      float distanceLimit,
      float innerCenterMinimumDistanceLimit = 0.0f)
    {
      this.SetMoveGoToPoint(MobilePartyHelper.FindReachablePointAroundPosition(this.Party, centerPosition, distanceLimit, innerCenterMinimumDistanceLimit, true));
    }

    public float FoodChange => BOFCampaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyFoodConsumptionf(this).ResultNumber;

    internal void TeleportPartyToReachablePointAroundPosition(
      Vec2 centerPosition,
      float distanceLimit,
      float innerCenterMinimumDistanceLimit = 0.0f)
    {
      this.Position2D = MobilePartyHelper.FindReachablePointAroundPosition(this.Party, centerPosition, distanceLimit, innerCenterMinimumDistanceLimit, true);
    }

    public ExplainedNumber FoodChangeExplained => BOFCampaign.Current.Models.MobilePartyFoodConsumptionModel.CalculateDailyFoodConsumptionf(this, true);

    private bool GetAccessableTargetPointInDirection(
      out Vec2 targetPoint,
      Vec2 direction,
      float distance,
      Vec2 alternativePosition,
      int neededTriesForAlternative,
      float rotationChangeLimitAddition = 0.1f)
    {
      targetPoint = this.Position2D;
      float num1 = 2f * rotationChangeLimitAddition;
      float num2 = 1f;
      bool flag = false;
      int num3 = 0;
      while (!flag)
      {
        Vec2 vec2 = direction;
        float randomFloat = MBRandom.RandomFloat;
        vec2.RotateCCW((randomFloat - 0.5f) * num1);
        targetPoint = this.Position2D + vec2 * distance * num2;
        ++num3;
        num1 += rotationChangeLimitAddition;
        num2 *= 0.97f;
        PathFaceRecord faceIndex = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(targetPoint);
        if (faceIndex.IsValid() && BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(faceIndex, this.CurrentNavigationFace, false) && ((double) targetPoint.x > (double) BOFCampaign.Current.MinSettlementX - 50.0 || (double) targetPoint.x > (double) this.Position2D.x) && ((double) targetPoint.y > (double) BOFCampaign.Current.MinSettlementY - 50.0 || (double) targetPoint.y > (double) this.Position2D.y) && ((double) targetPoint.x < (double) BOFCampaign.Current.MaxSettlementX + 50.0 || (double) targetPoint.x < (double) this.Position2D.x) && ((double) targetPoint.y < (double) BOFCampaign.Current.MaxSettlementY + 50.0 || (double) targetPoint.y < (double) this.Position2D.y))
          flag = num3 >= neededTriesForAlternative || PartyAi.CheckIfThereIsAnyHugeObstacleBetweenPartyAndTarget(this, targetPoint);
        if (num3 >= neededTriesForAlternative)
        {
          flag = true;
          targetPoint = alternativePosition;
        }
      }
      return flag;
    }

    public ExplainedNumber SpeedExplanation => BOFCampaign.Current.Models.PartySpeedCalculatingModel.CalculateFinalSpeed(this, BOFCampaign.Current.Models.PartySpeedCalculatingModel.CalculatePureSpeed(this, true));

    private bool ComputePath(Vec2 newTargetPosition)
    {
      bool flag = false;
      if (this.CurrentNavigationFace.IsValid())
      {
        this._targetAiFaceIndex = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(newTargetPosition);
        if (this._targetAiFaceIndex.IsValid())
          flag = BOFCampaign.Current.MapSceneWrapper.GetPathBetweenAIFaces(this.CurrentNavigationFace, this._targetAiFaceIndex, this.Position2D, newTargetPosition, 0.1f, this.Path);
        else
          Debug.FailedAssert("Path finding target is not valid", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MobileParty.cs", nameof (ComputePath), 3387);
      }
      this.PathBegin = 0;
      if (!flag)
        this._aiPathMode = false;
      return flag;
    }

    public float HealingRateForRegulars => BOFCampaign.Current.Models.PartyHealingModel.GetDailyHealingForRegulars(this).ResultNumber;

    private void UpdatePathModeWithPosition(Vec2 newTargetPosition)
    {
      this.MoveTargetPoint = newTargetPosition;
      this._moveTargetAiFaceIndex = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(newTargetPosition);
    }

    public ExplainedNumber HealingRateForRegularsExplained => BOFCampaign.Current.Models.PartyHealingModel.GetDailyHealingForRegulars(this, true);

    private void GetTargetPositionAndFace(
      out Vec2 finalTargetPosition,
      out PathFaceRecord finalTargetNavigationFace,
      out bool forceNoPathMode)
    {
      finalTargetPosition = this.Position2D;
      finalTargetNavigationFace = this.CurrentNavigationFace;
      forceNoPathMode = false;
      if (this.PartyMoveMode == MoveModeType.Point)
      {
        finalTargetPosition = this.MoveTargetPoint;
        finalTargetNavigationFace = this._moveTargetAiFaceIndex;
        forceNoPathMode = this.ForceAiNoPathMode;
      }
      else if (this.PartyMoveMode == MoveModeType.Party)
      {
        if (!this.MoveTargetParty.Party.IsValid)
          return;
        finalTargetPosition = this.MoveTargetParty.Position2D;
        finalTargetNavigationFace = this.MoveTargetParty.CurrentNavigationFace;
      }
      else
      {
        if (this.PartyMoveMode != MoveModeType.Escort)
          return;
        if (this.MoveTargetParty != null && this.MoveTargetParty.Party.IsValid && this.MoveTargetParty.CurrentSettlement == null && this.CurrentSettlement == null)
        {
          if ((double) this.MoveTargetParty.Position2D.DistanceSquared(this.Position2D) > 25.0)
          {
            finalTargetPosition = this.MoveTargetParty.Position2D;
            finalTargetNavigationFace = this.MoveTargetParty.CurrentNavigationFace;
          }
          else
          {
            if (this.LeaderHero != null && this.IsJoiningArmy && this.LeaderHero.PartyBelongedTo != null && this.LeaderHero.PartyBelongedTo.Army != null)
            {
              CampaignEventDispatcher.Instance.OnPartyArrivedArmy(this);
              this.IsJoiningArmy = false;
            }
            float num = this.Army != null ? 0.0f : (this.MoveTargetParty.DefaultBehavior != AiBehavior.DefendSettlement && this.MoveTargetParty.DefaultBehavior != AiBehavior.BesiegeSettlement && this.MoveTargetParty.DefaultBehavior != AiBehavior.RaidSettlement || (double) this.Party.MobileParty.Position2D.DistanceSquared(this.MoveTargetParty.AiBehaviorTarget) >= 100.0 ? 2.4f : 1.2f);
            while (true)
            {
              do
              {
                finalTargetPosition = this.MoveTargetParty.Position2D + num * this._formationPosition;
                PathFaceRecord faceIndex1 = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(this.Position2D);
                finalTargetNavigationFace = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(finalTargetPosition);
                PathFaceRecord currentNavigationFace1 = this.MoveTargetParty.CurrentNavigationFace;
                Vec2.StackArray6Vec2 stackArray6Vec2_1 = new Vec2.StackArray6Vec2();
                PathFaceRecord.StackArray6PathFaceRecord array6PathFaceRecord1 = new PathFaceRecord.StackArray6PathFaceRecord();
                for (int index = 0; index < 6; ++index)
                {
                  stackArray6Vec2_1[index] = new Vec2((float) (((double) finalTargetPosition.x * (double) index + (double) this.Position2D.x * (double) (6 - index)) / 6.0), (float) (((double) finalTargetPosition.y * (double) index + (double) this.Position2D.y * (double) (6 - index)) / 6.0));
                  array6PathFaceRecord1[index] = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(stackArray6Vec2_1[index]);
                }
                bool flag1 = BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(faceIndex1, finalTargetNavigationFace, false);
                PathFaceRecord currentNavigationFace2;
                for (int index = 0; index < 6 & flag1; ++index)
                {
                  int faceIndex2 = faceIndex1.FaceIndex;
                  currentNavigationFace2 = array6PathFaceRecord1[index];
                  int faceIndex3 = currentNavigationFace2.FaceIndex;
                  if (faceIndex2 != faceIndex3 && !BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(faceIndex1, array6PathFaceRecord1[index], false))
                    flag1 = false;
                }
                if (flag1)
                {
                  Vec2.StackArray6Vec2 stackArray6Vec2_2 = new Vec2.StackArray6Vec2();
                  PathFaceRecord.StackArray6PathFaceRecord array6PathFaceRecord2 = new PathFaceRecord.StackArray6PathFaceRecord();
                  for (int index = 0; index < 6; ++index)
                  {
                    stackArray6Vec2_2[index] = new Vec2((float) (((double) finalTargetPosition.x * (double) index + (double) this.MoveTargetParty.Position2D.x * (double) (6 - index)) / 6.0), (float) (((double) finalTargetPosition.y * (double) index + (double) this.MoveTargetParty.Position2D.y * (double) (6 - index)) / 6.0));
                    array6PathFaceRecord2[index] = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(stackArray6Vec2_2[index]);
                  }
                  bool flag2 = BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(finalTargetNavigationFace, currentNavigationFace1, false);
                  for (int index = 0; index < 6 & flag2; ++index)
                  {
                    currentNavigationFace2 = this.MoveTargetParty.CurrentNavigationFace;
                    int faceIndex4 = currentNavigationFace2.FaceIndex;
                    currentNavigationFace2 = array6PathFaceRecord2[index];
                    int faceIndex5 = currentNavigationFace2.FaceIndex;
                    if (faceIndex4 != faceIndex5 && !BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(this.MoveTargetParty.CurrentNavigationFace, array6PathFaceRecord2[index], false))
                      flag2 = false;
                  }
                  if (flag2)
                    goto label_38;
                }
                num = num * 0.75f - 0.1f;
                if ((double) num < 0.100000001490116)
                  goto label_39;
              }
              while ((double) num > 0.300000011920929);
              num = 0.0f;
            }
label_38:
            return;
label_39:;
          }
        }
        else if (this.CurrentSettlement != null)
          this.SetMoveGoToSettlement(this.CurrentSettlement);
        else
          this.SetMoveModeHold();
      }
    }

    public float HealingRateForHeroes => BOFCampaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(this).ResultNumber;

    public void ResetNavigationFace() => this.CurrentNavigationFace = BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(this._position2D);

    public ExplainedNumber HealingRateForHeroesExplained => BOFCampaign.Current.Models.PartyHealingModel.GetDailyHealingHpForHeroes(this, true);

    public void DisableAi() => this.Ai.DisableAi();

    public ExplainedNumber SeeingRangeExplanation => BOFCampaign.Current.Models.MapVisibilityModel.GetPartySpottingRange(this, true);

    public void EnableAi() => this.Ai.EnableAi();

    public int InventoryCapacity => (int) BOFCampaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(this).ResultNumber;

    private void OnAiTick() => this.OnAiTickInternal();

    public ExplainedNumber InventoryCapacityExplainedNumber => BOFCampaign.Current.Models.InventoryCapacityModel.CalculateInventoryCapacity(this, true);

    public MapEventSide MapEventSide
    {
      get => this.Party.MapEventSide;
      set => this.Party.MapEventSide = value;
    }

    internal void TickAi(float dt)
    {
      if (this._defaultBehaviorNeedsUpdate)
      {
        this._nextAiCheckTime = CampaignTime.Now;
        this._defaultBehaviorNeedsUpdate = false;
      }
      if (this._nextAiCheckTime.IsFuture)
        return;
      this.OnAiTick();
      this._nextAiCheckTime = CampaignTime.Now + CampaignTime.Seconds((long) (0.25 * (0.600000023841858 + 0.100000001490116 * (double) MBRandom.RandomFloat) * (this.ShortTermTargetParty != MobileParty.MainParty || this.ShortTermBehavior != AiBehavior.EngageParty ? 1.0 : 0.5) * 60.0 * 60.0));
    }

    public static IEnumerable<MobileParty> FindPartiesAroundPosition(
      Vec2 position,
      float radius,
      Func<MobileParty, bool> condition = null)
    {
      return condition == null ? BOFCampaign.Current.MobilePartyLocator.FindPartiesAroundPosition(position, radius) : BOFCampaign.Current.MobilePartyLocator.FindPartiesAroundPosition(position, radius, condition);
    }

    private void OnAiTickInternal()
    {
      if (this.MapEvent != null)
        return;
      if (this == MobileParty.MainParty && MobileParty.MainParty.DefaultBehavior == AiBehavior.EngageParty && !MobileParty.MainParty.TargetParty.IsVisible)
        MobileParty.MainParty.SetMoveModeHold();
      if (this.Ai.IsDisabled)
      {
        if (!this.Ai.IsDisabled || !this.Ai.EnableAgainAtHourIsPast())
          return;
        this.Ai.EnableAi();
      }
      else
      {
        if (this.Army != null && this.Army.LeaderParty.AttachedParties.Contains(this))
          return;
        AiBehavior bestAiBehavior;
        PartyBase behaviorParty;
        Vec2 bestTargetPoint;
        this.GetBehaviors(out bestAiBehavior, out behaviorParty, out bestTargetPoint);
        if (this.Ai.IsDisabled && behaviorParty == null)
          return;
        this.SetAiBehavior(bestAiBehavior, behaviorParty, bestTargetPoint);
      }
    }

    private void DoAIMove()
    {
      if (this.Army != null && this.Army.LeaderParty.AttachedParties.Contains(this))
      {
        this._aiPathMode = false;
      }
      else
      {
        Vec2 finalTargetPosition;
        PathFaceRecord finalTargetNavigationFace;
        bool forceNoPathMode;
        this.GetTargetPositionAndFace(out finalTargetPosition, out finalTargetNavigationFace, out forceNoPathMode);
        if (this._aiPathMode)
        {
          float num = (double) this._aiPathLastPosition.DistanceSquared(this.Position2D) > 108.0 ? 3f : this._aiPathLastPosition.DistanceSquared(this.Position2D) / 36f;
          if (forceNoPathMode || finalTargetNavigationFace.FaceIndex != this._aiPathLastFace.FaceIndex || (double) this._aiPathLastPosition.DistanceSquared(finalTargetPosition) > (double) num)
          {
            this._aiPathMode = false;
            this._aiPathLastFace = PathFaceRecord.NullFaceRecord;
          }
        }
        if (!this._aiPathMode && !forceNoPathMode && !this._aiPathNotFound)
        {
          if ((finalTargetNavigationFace.FaceIndex != this._aiPathLastFace.FaceIndex || this._aiPathNeeded) && finalTargetNavigationFace.IsValid())
          {
            if (this.CurrentNavigationFace.FaceIndex != finalTargetNavigationFace.FaceIndex || this._aiPathNeeded)
            {
              this._aiPathNotFound = !this.ComputePath(finalTargetPosition);
              this._aiPathNeeded = false;
              if (!this._aiPathNotFound)
              {
                this._aiPathLastFace = finalTargetNavigationFace;
                this._aiPathLastPosition = finalTargetPosition;
                this._aiPathMode = true;
              }
            }
            else
              this._aiPathMode = false;
          }
          else if (finalTargetNavigationFace.FaceIndex == this._aiPathLastFace.FaceIndex && this.CurrentNavigationFace.FaceIndex != finalTargetNavigationFace.FaceIndex)
            this._aiPathMode = true;
        }
        if (this._aiPathMode)
          return;
        this._nextTargetPosition = finalTargetPosition;
      }
    }

    internal void OnHeroAdded(Hero hero) => hero.OnAddedToParty(this);

    public void ResetAiBehaviorObject() => this.AiBehaviorObject = (PartyBase) null;

    internal void OnHeroRemoved(Hero hero) => hero.OnRemovedFromParty(this);

    public void RecalculateShortTermAi()
    {
      if (this.DefaultBehavior == AiBehavior.RaidSettlement)
      {
        this.ShortTermBehavior = AiBehavior.RaidSettlement;
        this.AiBehaviorObject = this.TargetSettlement.Party;
      }
      else if (this.DefaultBehavior == AiBehavior.BesiegeSettlement)
      {
        this.ShortTermBehavior = AiBehavior.BesiegeSettlement;
        this.AiBehaviorObject = this.TargetSettlement.Party;
      }
      else if (this.DefaultBehavior == AiBehavior.GoToSettlement)
      {
        this.ShortTermBehavior = AiBehavior.GoToSettlement;
        this.AiBehaviorObject = this.TargetSettlement.Party;
      }
      else if (this.DefaultBehavior == AiBehavior.EngageParty)
      {
        this.ShortTermBehavior = AiBehavior.EngageParty;
        this.AiBehaviorObject = this.TargetParty.Party;
      }
      else if (this.DefaultBehavior == AiBehavior.DefendSettlement)
      {
        this.ShortTermBehavior = AiBehavior.GoToPoint;
        this.AiBehaviorObject = this.TargetSettlement.Party;
      }
      else
      {
        if (this.DefaultBehavior != AiBehavior.EscortParty)
          return;
        this.ShortTermBehavior = AiBehavior.EscortParty;
        this.AiBehaviorObject = this.TargetParty.Party;
      }
    }

    public static Hero GetMainPartySkillCounsellor(SkillObject skill) => MobileParty.GetHeroWithHighestSkill(PartyBase.MainParty, skill);

    private void GetBehaviors(
      out AiBehavior bestAiBehavior,
      out PartyBase behaviorParty,
      out Vec2 bestTargetPoint)
    {
      bestAiBehavior = this.DefaultBehavior;
      MobileParty mobileParty = this.TargetParty;
      bestTargetPoint = this.TargetPosition;
      Vec2 avarageEnemyVec = new Vec2(0.0f, 0.0f);
      if (BOFCampaign.Current.GameStarted && this != MobileParty.MainParty && this.BesiegedSettlement == null && (this.DefaultBehavior != AiBehavior.GoToSettlement || this.TargetSettlement.Town == null || this.CurrentSettlement == this.TargetSettlement || !this.TargetSettlement.Town.IsTaken) && (this.Army == null || !this.Army.LeaderParty.AttachedParties.Contains(this)))
      {
        AiBehavior bestInitiativeBehavior;
        MobileParty bestInitiativeTargetParty;
        float bestInitiativeBehaviorScore;
        this.GetBestInitiativeBehavior(out bestInitiativeBehavior, out bestInitiativeTargetParty, out bestInitiativeBehaviorScore, out avarageEnemyVec);
        if (!this.Ai.DoNotMakeNewDecisions || bestInitiativeTargetParty != null && this.TargetSettlement != null && (bestInitiativeTargetParty.MapEvent != null && bestInitiativeTargetParty.MapEvent.MapEventSettlement == this.TargetSettlement || bestInitiativeTargetParty.BesiegedSettlement == this.TargetSettlement))
        {
          if (this.ShortTermBehavior == AiBehavior.EngageParty && this.ShortTermTargetParty != null && ((double) bestInitiativeBehaviorScore < 1.0 || bestInitiativeBehavior != AiBehavior.EngageParty || bestInitiativeTargetParty != this.ShortTermTargetParty))
            this._lastTargetedParties.Add(this.ShortTermTargetParty);
          if ((double) bestInitiativeBehaviorScore > 1.0)
          {
            bestAiBehavior = bestInitiativeBehavior;
            mobileParty = bestInitiativeTargetParty;
          }
          else if (this.ShortTermBehavior == AiBehavior.FleeToPoint || this.ShortTermBehavior == AiBehavior.FleeToGate)
          {
            double num1 = (double) this.ShortTermTargetPosition.DistanceSquared(this.Position2D);
            float speed = this.ComputeSpeed();
            double num2 = (double) speed * (double) speed * 0.25 * 0.25;
            if (num1 >= num2)
            {
              bestAiBehavior = AiBehavior.FleeToPoint;
              mobileParty = this.ShortTermTargetParty;
            }
          }
        }
      }
      this.IsAlerted = false;
      AiBehavior shortTermBehavior = bestAiBehavior;
      Vec2 vec2 = bestTargetPoint;
      Settlement shortTermTargetSettlement = this.TargetSettlement;
      MobileParty shortTermTargetParty = mobileParty;
      switch (bestAiBehavior)
      {
        case AiBehavior.GoToSettlement:
          if (this.CurrentSettlement == this.TargetSettlement)
          {
            this.GetInSettlementBehavior(ref shortTermBehavior, ref shortTermTargetParty);
            break;
          }
          break;
        case AiBehavior.BesiegeSettlement:
          if (!this.IsMainParty)
          {
            this.GetBesiegeBehavior(out shortTermBehavior, out vec2, out shortTermTargetSettlement);
            break;
          }
          break;
        case AiBehavior.EngageParty:
          this.Party.MobileParty.ShortTermBehavior = AiBehavior.EngageParty;
          break;
        case AiBehavior.GoAroundParty:
          this.GetGoAroundPartyBehavior(this.TargetParty, out shortTermBehavior, out vec2, out shortTermTargetParty);
          break;
        case AiBehavior.FleeToPoint:
          if (this.DefaultBehavior == AiBehavior.PatrolAroundPoint)
            this._aiBehaviorResetNeeded = true;
          this.IsAlerted = true;
          this.GetFleeBehavior(out shortTermBehavior, out vec2, ref shortTermTargetSettlement, mobileParty, avarageEnemyVec);
          break;
        case AiBehavior.PatrolAroundPoint:
          this.GetPatrolBehavior(out shortTermBehavior, out vec2, out shortTermTargetParty, this.TargetPosition);
          break;
        case AiBehavior.EscortParty:
          this.GetFollowBehavior(ref shortTermBehavior, ref shortTermTargetSettlement, ref shortTermTargetParty, mobileParty);
          break;
        case AiBehavior.DefendSettlement:
          if (this.TargetSettlement.LastAttackerParty != null && this.TargetSettlement.LastAttackerParty.IsActive)
          {
            this.GetGoAroundPartyBehavior(this.TargetSettlement.LastAttackerParty, out shortTermBehavior, out vec2, out shortTermTargetParty);
            break;
          }
          break;
      }
      bestAiBehavior = shortTermBehavior;
      bestTargetPoint = vec2;
      if (shortTermTargetParty != null)
        mobileParty = shortTermTargetParty;
      if (bestAiBehavior == AiBehavior.GoToSettlement || bestAiBehavior == AiBehavior.RaidSettlement || bestAiBehavior == AiBehavior.AssaultSettlement || bestAiBehavior == AiBehavior.BesiegeSettlement || bestAiBehavior == AiBehavior.DefendSettlement && mobileParty == null)
        behaviorParty = shortTermTargetSettlement != null ? shortTermTargetSettlement.Party : this.TargetSettlement.Party;
      else
        behaviorParty = mobileParty?.Party;
    }

    private static Hero GetHeroWithHighestSkill(PartyBase party, SkillObject skill)
    {
      Hero hero = (Hero) null;
      int num = 0;
      for (int index = 0; index < party.MemberRoster.Count; ++index)
      {
        CharacterObject characterAtIndex = party.MemberRoster.GetCharacterAtIndex(index);
        if (characterAtIndex.IsHero && !characterAtIndex.HeroObject.IsWounded)
        {
          int skillValue = characterAtIndex.GetSkillValue(skill);
          if (skillValue >= num)
          {
            num = skillValue;
            hero = characterAtIndex.HeroObject;
          }
        }
      }
      return hero ?? party.LeaderHero;
    }

    private void GetInSettlementBehavior(
      ref AiBehavior shortTermBehavior,
      ref MobileParty shortTermTargetParty)
    {
      if (this.MapEvent == null)
        return;
      MobileParty mobileParty = this.MapEvent.AttackerSide.LeaderParty.MobileParty;
      if (!this.IsEnemy(mobileParty))
        return;
      shortTermBehavior = AiBehavior.EngageParty;
      shortTermTargetParty = mobileParty;
    }

    private void GetFollowBehavior(
      ref AiBehavior shortTermBehavior,
      ref Settlement shortTermTargetSettlement,
      ref MobileParty shortTermTargetParty,
      MobileParty followedParty)
    {
      int shortTermBehavior1 = (int) followedParty.ShortTermBehavior;
      shortTermBehavior = AiBehavior.EscortParty;
      if (!followedParty.IsActive)
      {
        shortTermBehavior = AiBehavior.Hold;
      }
      else
      {
        if (followedParty.CurrentSettlement == null)
          return;
        shortTermBehavior = AiBehavior.GoToSettlement;
        shortTermTargetSettlement = followedParty.CurrentSettlement;
      }
    }

    private void GetBesiegeBehavior(
      out AiBehavior shortTermBehavior,
      out Vec2 shortTermTargetPoint,
      out Settlement shortTermTargetSettlement)
    {
      if (this.TargetSettlement != null)
      {
        if (this.TargetSettlement.SiegeEvent != null && this.TargetSettlement.SiegeEvent.BesiegerCamp.BesiegerParty == this && this.TargetSettlement.SiegeEvent.BesiegerCamp.IsReadyToBesiege)
        {
          shortTermTargetSettlement = this.TargetSettlement;
          shortTermBehavior = AiBehavior.AssaultSettlement;
        }
        else if (this.BesiegedSettlement == this.TargetSettlement)
        {
          shortTermTargetSettlement = (Settlement) null;
          shortTermBehavior = AiBehavior.Hold;
        }
        else
        {
          shortTermTargetSettlement = this.TargetSettlement;
          shortTermBehavior = AiBehavior.GoToSettlement;
        }
      }
      else
      {
        shortTermTargetSettlement = (Settlement) null;
        shortTermBehavior = AiBehavior.GoToPoint;
      }
      shortTermTargetPoint = this.TargetSettlement.GatePosition;
    }

    private void GetDefendBehavior(
      out AiBehavior shortTermBehavior,
      out Vec2 shortTermTargetPoint,
      out MobileParty shortTermTargetParty,
      out Settlement shortTermTargetSettlement)
    {
      shortTermTargetSettlement = this.TargetSettlement;
      shortTermTargetPoint = this.TargetSettlement.Position2D;
      if (this.TargetSettlement.SiegeEvent != null)
      {
        shortTermTargetParty = this.TargetSettlement.SiegeEvent.BesiegerCamp.BesiegerParty;
        if (this.CurrentSettlement != this.TargetSettlement && this.TargetSettlement != null)
        {
          Vec2 vec2_1 = this.TargetSettlement.SiegeEvent.BesiegerCamp.BesiegerParty.GetVisualPosition().AsVec2 - this.TargetSettlement.GatePosition;
          Vec2 vec2_2 = new Vec2(vec2_1.Y, vec2_1.X);
          double num1 = (double) vec2_2.Normalize();
          Vec2 vec2_3 = vec2_2 * ((float) this.Party.Random.GetValue(0) / 100f);
          bool flag = false;
          for (int index1 = 0; index1 < 2 && !flag; ++index1)
          {
            int num2 = 7;
            for (int index2 = 1; index2 <= num2; ++index2)
            {
              shortTermTargetPoint = this.TargetSettlement.GatePosition + vec2_3 * ((float) (num2 - index2) / (float) (num2 - 1)) + vec2_1 * ((float) index2 / (float) num2) + this._formationPosition;
              if (BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(this.Position2D), BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(shortTermTargetPoint), false) && PartyBase.IsPositionOkForTraveling(shortTermTargetPoint))
              {
                flag = true;
                break;
              }
            }
          }
        }
        shortTermBehavior = this.CurrentSettlement != this.TargetSettlement || this.TargetSettlement == null ? AiBehavior.GoToPoint : AiBehavior.GoToSettlement;
      }
      else if (this.TargetSettlement.Party.MapEvent != null && this.TargetSettlement.Party.MapEvent.IsRaid)
      {
        shortTermTargetParty = this.TargetSettlement.Party.MapEvent.AttackerSide.LeaderParty.MobileParty;
        bool flag = false;
        for (int index3 = 5; index3 >= 0 && !flag; --index3)
        {
          for (int index4 = 0; index4 < 2; ++index4)
          {
            shortTermTargetPoint = this.TargetSettlement.Party.MapEvent.AttackerSide.LeaderParty.MobileParty.Position2D + (index4 == 0 ? this._formationPosition : -this._formationPosition) * (float) index3;
            if (index3 > 0 && BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(this.Position2D), BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(shortTermTargetPoint), false))
            {
              flag = true;
              break;
            }
          }
        }
        shortTermBehavior = AiBehavior.GoToPoint;
      }
      else if (this.TargetSettlement.LastAttackerParty != null)
      {
        this.GetGoAroundPartyBehavior(this.TargetSettlement.LastAttackerParty, out shortTermBehavior, out shortTermTargetPoint, out shortTermTargetParty);
        shortTermTargetSettlement = (Settlement) null;
      }
      else
      {
        shortTermTargetParty = (MobileParty) null;
        shortTermTargetSettlement = this.TargetSettlement;
        shortTermTargetPoint = this.TargetSettlement.GatePosition;
        shortTermBehavior = AiBehavior.GoToPoint;
      }
    }

    private void GetFleeBehavior(
      out AiBehavior fleeBehavior,
      out Vec2 fleeTargetPoint,
      ref Settlement fleeTargetSettlement,
      MobileParty partyToFleeFrom,
      Vec2 avarageEnemyVec)
    {
      fleeBehavior = this.ShortTermBehavior;
      fleeTargetPoint = this.ShortTermTargetPosition;
      if (this.CurrentSettlement != null)
      {
        fleeBehavior = AiBehavior.GoToSettlement;
        fleeTargetSettlement = this.CurrentSettlement;
      }
      else if (this.DefaultBehavior == AiBehavior.GoToSettlement && (partyToFleeFrom.MapEvent != null && partyToFleeFrom.MapEvent.MapEventSettlement == this.TargetSettlement || partyToFleeFrom.BesiegedSettlement == this.TargetSettlement))
      {
        fleeBehavior = AiBehavior.FleeToPoint;
        fleeTargetPoint = this.Position2D;
      }
      else if (this.ShortTermBehavior != AiBehavior.FleeToPoint || this.ShortTermTargetParty != partyToFleeFrom || (double) this.ShortTermTargetPosition.Distance(this.Position2D) < (double) this.ComputeSpeed() * 0.25)
      {
        ++this._numberOfFleeingsAtLastTravel;
        fleeBehavior = AiBehavior.FleeToPoint;
        fleeTargetPoint = this.Position2D;
        Vec2 vec2 = partyToFleeFrom.Position2D - this.Position2D;
        float num1 = MathF.Min(3f, vec2.Length);
        float num2 = (float) (2.0 * (double) MBRandom.RandomFloat - 1.0);
        float num3 = (float) (2.0 * (double) MBRandom.RandomFloat - 1.0);
        double num4 = (double) vec2.Normalize();
        this.AverageFleeTargetDirection = new Vec2((this.AverageFleeTargetDirection.x * (float) (this._numberOfFleeingsAtLastTravel - 1) + vec2.x) / (float) this._numberOfFleeingsAtLastTravel, (this.AverageFleeTargetDirection.y * (float) (this._numberOfFleeingsAtLastTravel - 1) + vec2.y) / (float) this._numberOfFleeingsAtLastTravel);
        double num5 = (double) this.AverageFleeTargetDirection.Normalize();
        vec2 += 3f * avarageEnemyVec;
        double num6 = (double) vec2.Normalize();
        Vec2 direction = -vec2 + 0.1f * this.Bearing + new Vec2(num2 * (num1 / 10f), num3 * (num1 / 10f));
        double num7 = (double) direction.Normalize();
        float distance = this.ComputeSpeed() + 3f;
        if (this.DefaultBehavior == AiBehavior.EngageParty)
          distance *= 0.65f;
        fleeBehavior = AiBehavior.FleeToPoint;
        double num8 = (double) direction.Normalize();
        this.GetAccessableTargetPointInDirection(out fleeTargetPoint, direction, distance, this.Position2D, 100);
      }
      Vec2 fleeDirection = fleeTargetPoint - this.Position2D;
      double num = (double) fleeDirection.Normalize();
      if (this.IsLordParty && this.MapFaction.IsKingdomFaction)
      {
        (AiBehavior aiBehavior2, Settlement settlement2) = this.GetBehaviorForNearbySettlementToFlee(partyToFleeFrom.Position2D, fleeDirection);
        if (aiBehavior2 != AiBehavior.None)
        {
          fleeBehavior = aiBehavior2;
          if (aiBehavior2 == AiBehavior.GoToSettlement)
            fleeTargetSettlement = settlement2;
          else if (aiBehavior2 == AiBehavior.FleeToGate)
            fleeTargetPoint = settlement2.GatePosition;
        }
      }
      MobileParty nearbyPartyToFlee = this.GetNearbyPartyToFlee(partyToFleeFrom, fleeDirection);
      if (nearbyPartyToFlee == null)
        return;
      fleeBehavior = AiBehavior.FleeToParty;
      fleeTargetPoint = nearbyPartyToFlee.Position2D;
    }

    private MobileParty GetNearbyPartyToFlee(
      MobileParty partyToFleeFrom,
      Vec2 fleeDirection)
    {
      IEnumerable<MobileParty> partiesAroundPosition = BOFCampaign.Current.MobilePartyLocator.FindPartiesAroundPosition(this.Position2D, 10f);
      (MobileParty, float) valueTuple = ((MobileParty) null, 0.0f);
      foreach (MobileParty mobileParty in partiesAroundPosition)
      {
        if (!mobileParty.IsMilitia && !mobileParty.IsGarrison && !mobileParty.IsCaravan && mobileParty != this && (mobileParty.MapFaction == this.MapFaction || mobileParty.IsBandit && this.IsBandit) && mobileParty.AttachedTo == null && mobileParty.CurrentSettlement == null && mobileParty.MapEvent == null)
        {
          float num1 = mobileParty.Army == null || !mobileParty.Army.LeaderPartyAndAttachedParties.Contains<MobileParty>(this) ? mobileParty.Party.TotalStrength : mobileParty.Army.TotalStrength;
          if ((double) num1 > (double) this.Party.TotalStrength && (this.IsBandit || (double) num1 + (double) this.Party.TotalStrength > (double) partyToFleeFrom.Party.TotalStrength * 0.5))
          {
            Vec2 vec2 = mobileParty.Position2D - this.Position2D;
            float length1 = vec2.Length;
            if ((double) length1 >= 1.0 || (double) mobileParty.LastCachedSpeed >= (double) this.LastCachedSpeed)
            {
              float length2 = (partyToFleeFrom.Position2D - mobileParty.Position2D).Length;
              double num2 = (double) vec2.Normalize();
              if ((double) vec2.DistanceSquared(fleeDirection) < 0.300000011920929 + 0.100000001490116 * (10.0 - (double) Math.Min(10f, length1)) && (double) length1 < (double) length2 * 0.860000014305115)
              {
                float num3 = (float) ((double) length1 * 0.660000026226044 + (double) num1 * 0.340000003576279);
                if ((double) valueTuple.Item2 < (double) num3)
                {
                  valueTuple.Item1 = mobileParty;
                  valueTuple.Item2 = num3;
                }
              }
            }
          }
        }
      }
      return valueTuple.Item1;
    }

    private (AiBehavior, Settlement) GetBehaviorForNearbySettlementToFlee(
      Vec2 partyToFleeFromPosition,
      Vec2 fleeDirection)
    {
      foreach (Settlement settlement in BOFCampaign.Current.SettlementLocator.FindPartiesAroundPosition(this.Position2D, 20f))
      {
        if (settlement.IsFortification && settlement.MapFaction == this.MapFaction && !settlement.IsUnderSiege)
        {
          Vec2 vec2 = settlement.GatePosition - this.Position2D;
          float length1 = vec2.Length;
          float length2 = (partyToFleeFromPosition - settlement.GatePosition).Length;
          if ((double) length1 < (double) length2 * 0.860000014305115)
          {
            if ((double) length1 > 1.0)
              return (AiBehavior.FleeToGate, settlement);
            this.Ai.DisableForHours(6);
            return (AiBehavior.GoToSettlement, settlement);
          }
          double num = (double) vec2.Normalize();
          if ((double) vec2.DistanceSquared(fleeDirection) < 0.300000011920929 + 0.100000001490116 * (10.0 - (double) MathF.Min(10f, length1)))
            return (double) length1 > 1.0 ? (AiBehavior.FleeToGate, settlement) : (AiBehavior.GoToSettlement, settlement);
        }
      }
      return (AiBehavior.None, (Settlement) null);
    }

    private void GetGoAroundPartyBehavior(
      MobileParty targetParty,
      out AiBehavior goAroundPartyBehavior,
      out Vec2 goAroundPartyTargetPoint,
      out MobileParty goAroundPartyTargetParty)
    {
      Vec2 position2D = targetParty.Position2D;
      goAroundPartyTargetPoint = position2D;
      goAroundPartyTargetParty = targetParty;
      Vec2 vec2_1 = this.Position2D - position2D;
      float length = vec2_1.Length;
      double num1 = (double) vec2_1.Normalize();
      bool flag = false;
      Vec2 v = goAroundPartyTargetPoint;
      for (int index1 = 5; index1 >= 0 && !flag; --index1)
      {
        int num2 = (this.LeaderHero != null ? this.LeaderHero.RandomValueRarelyChanging % 9 : 5) - 4;
        for (int index2 = 1; index2 <= 2 && !flag; ++index2)
        {
          for (int index3 = num2; index3 < num2 + 9; ++index3)
          {
            Vec2 vec2_2 = vec2_1;
            vec2_2.RotateCCW((float) ((double) index3 / 9.0 * 1.57079637050629 * (double) index2 * 0.5 * ((double) MathF.Min(MathF.Max(0.0f, length - 3.6f), 9f) / 9.0)));
            Vec2 position = position2D + vec2_2 * 3f * 1.2f * ((float) index1 / 5f);
            if (BOFCampaign.Current.MapSceneWrapper.AreFacesOnSameIsland(BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(this.Position2D), BOFCampaign.Current.MapSceneWrapper.GetFaceIndex(position), false) && PartyBase.IsPositionOkForTraveling(position))
            {
              flag = true;
              v = position;
              break;
            }
          }
        }
      }
      if (flag)
      {
        double randomFloat = (double) MBRandom.RandomFloat;
        double num3 = 0.200000002980232 * (double) this.ShortTermTargetPosition.DistanceSquared(v);
        goAroundPartyTargetPoint = randomFloat < num3 || (double) targetParty.GetCachedPureSpeed() > (double) this.GetCachedPureSpeed() ? v : this.ShortTermTargetPosition;
        double speed1 = (double) this.ComputeSpeed();
        double speed2 = (double) targetParty.ComputeSpeed();
        goAroundPartyBehavior = AiBehavior.GoToPoint;
      }
      else
      {
        goAroundPartyBehavior = AiBehavior.EngageParty;
        goAroundPartyTargetParty = targetParty;
      }
    }

    private void GetPatrolBehavior(
      out AiBehavior patrolBehavior,
      out Vec2 patrolTargetPoint,
      out MobileParty patrolTargetParty,
      Vec2 patrollingCenterPoint)
    {
      double num1 = (double) this.ShortTermTargetPosition.DistanceSquared(this.Position2D);
      patrolBehavior = AiBehavior.GoToPoint;
      patrolTargetPoint = this.ShortTermTargetPosition;
      patrolTargetParty = (MobileParty) null;
      if (this.ShortTermBehavior == AiBehavior.GoToSettlement || this.ShortTermBehavior == AiBehavior.EngageParty || this.ShortTermBehavior == AiBehavior.FleeToPoint)
        this._aiBehaviorResetNeeded = true;
      if (num1 >= 1.0 && !this._aiBehaviorResetNeeded)
        return;
      if ((double) MBRandom.RandomFloat < 0.125 || this._aiBehaviorResetNeeded)
      {
        Vec2 vec2 = patrollingCenterPoint - this.Position2D;
        float num2 = vec2.Normalize();
        float num3 = this.IsCurrentlyUsedByAQuest ? MathF.Max(num2 * 0.25f, 15f) : MathF.Max(num2 * 0.25f, 30f);
        if ((double) num2 > (double) num3 * (3.20000004768372 / (this.IsCurrentlyUsedByAQuest ? 1.5 : 1.0)))
        {
          patrolBehavior = AiBehavior.GoToPoint;
          patrolTargetPoint = this.TargetSettlement != null ? this.TargetSettlement.GatePosition : patrollingCenterPoint;
        }
        else
        {
          float num4 = this.TargetSettlement == null || (double) this.TargetSettlement.NumberOfEnemiesSpottedAround <= 1.0 ? 0.0f : MathF.Sqrt(this.TargetSettlement.NumberOfEnemiesSpottedAround) - 1f;
          float num5 = MathF.Max(0.0f, MathF.Min(0.9f, (float) ((double) num2 / ((double) num3 / ((this.TargetSettlement == null || this.TargetSettlement.MapFaction != this.MapFaction ? 0.0 : (double) num4) + 1.0)) - 0.400000005960464)));
          Vec2 direction = (1f - num5) * this.Bearing + num5 * vec2;
          direction.RotateCCW((float) (((double) MBRandom.RandomFloat - 0.300000011920929) * 0.150000005960464));
          double num6 = (double) direction.Normalize();
          float num7 = (float) (0.5 + 0.5 * (double) MBRandom.RandomFloat);
          float rotationChangeLimitAddition = (double) num2 > 120.0 ? 0.2f : ((double) num2 > 60.0 ? 0.4f : ((double) num2 > 30.0 ? 0.6f : 1f));
          this.GetAccessableTargetPointInDirection(out patrolTargetPoint, direction, num3 * num7, patrollingCenterPoint, 20, rotationChangeLimitAddition);
        }
        this._aiBehaviorResetNeeded = false;
      }
      else
        patrolTargetPoint = this.Position2D;
    }

    private void GetBestInitiativeBehavior(
      out AiBehavior bestInitiativeBehavior,
      out MobileParty bestInitiativeTargetParty,
      out float bestInitiativeBehaviorScore,
      out Vec2 avarageEnemyVec)
    {
      MobileParty.DangerousPartiesAndTheirVecs.Clear();
      bestInitiativeBehaviorScore = 0.0f;
      bestInitiativeTargetParty = (MobileParty) null;
      bestInitiativeBehavior = AiBehavior.None;
      avarageEnemyVec = Vec2.Zero;
      if (this.CurrentSettlement != null && (this.IsGarrison || this.IsMilitia || this.IsBandit))
        return;
      List<MobileParty> partiesAroundPosition = this._partiesAroundPosition.GetPartiesAroundPosition(this.Position2D, 9f);
      foreach (MobileParty mobileParty in partiesAroundPosition)
      {
        if ((mobileParty.MapEvent == null || MobileParty.MainParty.MapEvent == null || MobileParty.MainParty.MapEvent != mobileParty.MapEvent || MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty || mobileParty == MobileParty.MainParty) && mobileParty != this && mobileParty.IsActive && this.IsEnemy(mobileParty) && !mobileParty.ShouldBeIgnored && (mobileParty.CurrentSettlement == null || mobileParty.IsGarrison || mobileParty.IsLordParty) && this.CurrentSettlement?.SiegeEvent == null && (!mobileParty.IsGarrison || this.IsBandit) && (mobileParty.BesiegerCamp == null || mobileParty.BesiegerCamp.BesiegerParty == mobileParty) && (mobileParty.Army == null || mobileParty.Army.LeaderParty == mobileParty || mobileParty.AttachedTo == null) && (mobileParty.MapEvent == null || mobileParty == MobileParty.MainParty || mobileParty.Party.MapEvent.MapEventSettlement != null || mobileParty.Party == mobileParty.Party.MapEvent.GetLeaderParty(BattleSideEnum.Attacker) || mobileParty.Party == mobileParty.Party.MapEvent.GetLeaderParty(BattleSideEnum.Defender)) && (mobileParty.MapEvent == null || this.IsEnemy(mobileParty.MapEvent.AttackerSide.LeaderParty) != this.IsEnemy(mobileParty.MapEvent.DefenderSide.LeaderParty)))
        {
          Vec2 v1 = mobileParty.BesiegedSettlement != null ? mobileParty.VisualPosition2DWithoutError : mobileParty.Position2D;
          Vec2 vec2_1 = this.Position2D;
          float num1 = vec2_1.Distance(v1);
          if ((double) num1 < 6.0)
          {
            float num2 = 1f + MathF.Max(0.0f, (float) (((double) num1 - 1.0) / 4.0));
            float num3 = (double) num2 > 2.0 ? 2f : num2;
            float num4 = (float) (0.00999999977648258 + (this.Army == null || this != this.Army.LeaderParty ? (double) this.Party.TotalStrength : (double) this.Army.TotalStrength));
            float aggressiveness = this.Aggressiveness;
            float num5 = 0.0f;
            float num6 = 0.01f;
            if (mobileParty.BesiegerCamp != null)
            {
              foreach (PartyBase siegeParty in mobileParty.SiegeEvent.BesiegerCamp.SiegeParties)
                num6 += siegeParty.TotalStrength;
            }
            else
              num6 += mobileParty.Army != null ? mobileParty.Army.TotalStrength : mobileParty.Party.TotalStrength;
            foreach (MobileParty party in partiesAroundPosition)
            {
              if ((this.MapFaction != party.MapFaction || party.BesiegedSettlement == null) && (party.MapEvent == null || party.MapEvent == mobileParty.MapEvent) && party.AttachedTo == null && party != this && party != mobileParty)
              {
                Vec2 v2 = party.BesiegedSettlement != null ? party.VisualPosition2DWithoutError : party.Position2D;
                double num7;
                if (party == mobileParty)
                {
                  vec2_1 = this.Position2D;
                  num7 = (double) vec2_1.Distance(v2);
                }
                else
                  num7 = (double) v2.Distance(v1);
                float num8 = (float) num7;
                if ((double) num8 <= 6.0 && (party.BesiegerCamp == null || party.BesiegerCamp.BesiegerParty == party))
                {
                  PartyBase aiBehaviorObject = party.AiBehaviorObject;
                  if (party.Army != null)
                    aiBehaviorObject = party.Army.LeaderParty.AiBehaviorObject;
                  bool flag1 = aiBehaviorObject != null && (aiBehaviorObject == mobileParty.Party || aiBehaviorObject.MapEvent != null && aiBehaviorObject.MapEvent == mobileParty.Party.MapEvent);
                  bool flag2 = this.Army != null && this.Army == party.Army && this.Army.LeaderPartyAndAttachedParties.Contains<MobileParty>(this) || mobileParty.Army != null && mobileParty.Army == party.Army || mobileParty.BesiegedSettlement != null && mobileParty.BesiegedSettlement == party.BesiegedSettlement || (double) num1 > 3.0 & flag1 || (double) num8 > 3.0 & flag1 && mobileParty != MobileParty.MainParty && (MobileParty.MainParty.Army == null || mobileParty != MobileParty.MainParty.Army.LeaderParty);
                  if (flag2 || (double) num8 < 3.0 * (double) num3)
                  {
                    float num9 = MathF.Min(1f, flag2 ? 1f : ((double) num8 < 3.0 ? 1f : (float) (1.0 - ((double) num8 - 3.0) / (3.0 * ((double) num3 - 1.0)))));
                    bool flag3 = mobileParty.MapEvent != null && mobileParty.MapEvent == party.MapEvent;
                    float num10 = party.Army == null || party.Army.LeaderParty != party ? party.Party.TotalStrength : party.Army.TotalStrength;
                    if ((((double) party.Aggressiveness > 0.00999999977648258 ? 1 : (party.IsGarrison ? 1 : 0)) | (flag3 ? 1 : 0)) != 0 && party.MapFaction == mobileParty.MapFaction)
                    {
                      if (party.BesiegerCamp != null)
                      {
                        foreach (PartyBase siegeParty in party.SiegeEvent.BesiegerCamp.SiegeParties)
                        {
                          if (siegeParty.MobileParty.AttachedTo == null)
                            num6 += siegeParty.MobileParty.Army != null ? siegeParty.MobileParty.Army.TotalStrength : siegeParty.TotalStrength;
                        }
                      }
                      else
                        num6 += num10 * num9;
                    }
                    if (this.IsAlly(party))
                    {
                      bool flag4 = (double) party.Aggressiveness > 0.00999999977648258 || party.CurrentSettlement != null && party.CurrentSettlement == mobileParty.CurrentSettlement;
                      bool flag5 = mobileParty != MobileParty.MainParty || party.CanAttack(MobileParty.MainParty);
                      bool flag6 = party.CurrentSettlement == null || !party.CurrentSettlement.IsHideout;
                      if ((flag3 || flag4 & flag5 & flag6) && (party.CurrentSettlement?.SiegeEvent == null || mobileParty != party.CurrentSettlement.SiegeEvent.BesiegerCamp.BesiegerParty))
                      {
                        if (party.BesiegerCamp != null)
                        {
                          foreach (PartyBase siegeParty in party.SiegeEvent.BesiegerCamp.SiegeParties)
                          {
                            if (siegeParty.MobileParty.AttachedTo == null)
                            {
                              num4 += siegeParty.MobileParty.Army != null ? siegeParty.MobileParty.Army.TotalStrength : siegeParty.TotalStrength;
                              if ((double) siegeParty.MobileParty.Aggressiveness > (double) aggressiveness)
                                aggressiveness = siegeParty.MobileParty.Aggressiveness;
                            }
                          }
                        }
                        else
                        {
                          num4 += num10 * num9;
                          if ((double) party.Aggressiveness > (double) aggressiveness)
                            aggressiveness = party.Aggressiveness;
                          if (party.CurrentSettlement != null)
                            num5 += num10 * num9;
                        }
                      }
                    }
                  }
                }
              }
            }
            if (this.CurrentSettlement != null)
              num4 -= num5;
            if (mobileParty.LastVisitedSettlement != null && mobileParty.LastVisitedSettlement.IsVillage && (double) mobileParty.Position2D.DistanceSquared(mobileParty.LastVisitedSettlement.Position2D) < 1.0 && mobileParty.LastVisitedSettlement.MapFaction.IsAtWarWith(this.MapFaction))
              num6 += 20f;
            float localAdvantage = num4 / num6 * (!this.IsCaravan && !this.IsVillager || mobileParty != MobileParty.MainParty ? 1f : 0.6f);
            if (mobileParty.IsCaravan)
            {
              if (this.IsBandit)
              {
                float gameProcess = MiscHelper.GetGameProcess();
                localAdvantage *= (float) (2.40000009536743 - 0.899999976158142 * (double) gameProcess);
              }
              else if (this.LeaderHero != null && this.LeaderHero.IsMinorFactionHero)
                localAdvantage *= 1.5f;
            }
            if (mobileParty.MapEvent != null && mobileParty.MapEvent.IsSiegeAssault && mobileParty == mobileParty.MapEvent.AttackerSide.LeaderParty.MobileParty)
            {
              float settlementAdvantage = BOFCampaign.Current.Models.CombatSimulationModel.GetSettlementAdvantage(mobileParty.MapEvent.MapEventSettlement);
              if ((double) num5 * (double) MathF.Sqrt(settlementAdvantage) > (double) num6)
                continue;
            }
            float avoidScore;
            float attackScore;
            this.CalculateInitiativeScoresForEnemy(mobileParty, out avoidScore, out attackScore, localAdvantage, aggressiveness);
            if (mobileParty.CurrentSettlement != null && mobileParty.MapEvent == null)
              attackScore = 0.0f;
            if ((double) avoidScore > 1.0)
            {
              List<(float, Vec2)> partiesAndTheirVecs = MobileParty.DangerousPartiesAndTheirVecs;
              double num11 = (double) avoidScore;
              vec2_1 = v1 - this.Position2D;
              Vec2 vec2_2 = vec2_1.Normalized();
              (float, Vec2) valueTuple = ((float) num11, vec2_2);
              partiesAndTheirVecs.Add(valueTuple);
            }
            if ((double) avoidScore > (double) bestInitiativeBehaviorScore || (double) avoidScore > 1.0 && bestInitiativeBehavior == AiBehavior.EngageParty)
            {
              bestInitiativeBehavior = AiBehavior.FleeToPoint;
              bestInitiativeTargetParty = mobileParty;
              bestInitiativeBehaviorScore = avoidScore;
            }
            if ((double) attackScore > (double) bestInitiativeBehaviorScore && ((double) bestInitiativeBehaviorScore < 1.0 || bestInitiativeBehavior == AiBehavior.EngageParty))
            {
              bestInitiativeBehavior = AiBehavior.EngageParty;
              bestInitiativeTargetParty = mobileParty;
              bestInitiativeBehaviorScore = attackScore;
            }
          }
        }
      }
      this._partiesAroundPosition.ClearParties();
      if (bestInitiativeBehavior != AiBehavior.FleeToPoint && bestInitiativeBehavior != AiBehavior.FleeToGate)
        return;
      float num12 = 0.0f;
      for (int index1 = 0; index1 < 8; ++index1)
      {
        Vec2 v = new Vec2(MathF.Sin((float) ((double) index1 / 8.0 * 3.14159274101257 * 2.0)), MathF.Cos((float) ((double) index1 / 8.0 * 3.14159274101257 * 2.0)));
        float num13 = 0.0f;
        for (int index2 = 0; index2 < MobileParty.DangerousPartiesAndTheirVecs.Count; ++index2)
        {
          float num14 = MobileParty.DangerousPartiesAndTheirVecs[index2].Item2.DistanceSquared(v);
          if ((double) num14 > 1.0)
            num14 = (float) (1.0 + ((double) num14 - 1.0) * 0.5);
          num13 += num14 * MobileParty.DangerousPartiesAndTheirVecs[index2].Item1;
        }
        if ((double) num13 > (double) num12)
        {
          avarageEnemyVec = -v;
          num12 = num13;
        }
      }
    }

    private void SetAiBehavior(
      AiBehavior newAiBehavior,
      PartyBase targetPartyFigure,
      Vec2 bestTargetPoint)
    {
      this.ShortTermBehavior = newAiBehavior;
      this.AiBehaviorObject = targetPartyFigure;
      this.AiBehaviorTarget = bestTargetPoint;
      this.UpdateBehavior();
    }

    private void UpdateBehavior()
    {
      if (this.ShortTermBehavior == AiBehavior.GoToPoint || this.ShortTermBehavior == AiBehavior.FleeToPoint || this.ShortTermBehavior == AiBehavior.FleeToGate || this.ShortTermBehavior == AiBehavior.FleeToParty)
        this.SetNavigationModePoint(this.AiBehaviorTarget);
      else if ((this.ShortTermBehavior == AiBehavior.GoToSettlement || this.ShortTermBehavior == AiBehavior.RaidSettlement || this.ShortTermBehavior == AiBehavior.AssaultSettlement || this.ShortTermBehavior == AiBehavior.BesiegeSettlement) && this.AiBehaviorObject != null && this.AiBehaviorObject.IsValid)
      {
        this.SetNavigationModePoint(this.AiBehaviorObject.Settlement.GatePosition);
      }
      else
      {
        switch (this.ShortTermBehavior)
        {
          case AiBehavior.Hold:
            this.SetNavigationModeHold();
            break;
          case AiBehavior.EngageParty:
            this.SetNavigationModeParty(this.AiBehaviorObject.MobileParty);
            break;
          case AiBehavior.FleeToPoint:
          case AiBehavior.FleeToGate:
          case AiBehavior.FleeToParty:
          case AiBehavior.DefendSettlement:
            break;
          case AiBehavior.EscortParty:
            this.SetNavigationModeEscort(this.AiBehaviorObject.MobileParty);
            break;
          default:
            Debug.FailedAssert("false", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MobileParty.cs", nameof (UpdateBehavior), 4892);
            break;
        }
      }
      if (this.CurrentSettlement == null || this.IsCurrentlyGoingToSettlement && this.ShortTermTargetSettlement == this.CurrentSettlement || this.IsMainParty)
        return;
      LeaveSettlementAction.ApplyForParty(this);
    }

    internal void SetNavigationModeHold()
    {
      this._partyMoveMode = MoveModeType.Hold;
      this._aiPathMode = false;
      this._aiPathNeeded = false;
      this._nextTargetPosition = this.Position2D;
      this.MoveTargetParty = (MobileParty) null;
    }

    internal void SetNavigationModePoint(Vec2 newTargetPosition)
    {
      this._partyMoveMode = MoveModeType.Point;
      this.UpdatePathModeWithPosition(newTargetPosition);
      this._aiPathNotFound = false;
      this.MoveTargetParty = (MobileParty) null;
    }

    internal void SetNavigationModeParty(MobileParty targetParty)
    {
      this._partyMoveMode = MoveModeType.Party;
      this.MoveTargetParty = targetParty;
      this._aiPathNotFound = false;
    }

    internal void SetNavigationModeEscort(MobileParty targetParty)
    {
      if (this._partyMoveMode == MoveModeType.Escort && this.MoveTargetParty == targetParty)
        return;
      this._partyMoveMode = MoveModeType.Escort;
      this.MoveTargetParty = targetParty;
      this._aiPathNotFound = false;
    }

    private void CalculateInitiativeScoresForEnemy(
      MobileParty enemyParty,
      out float avoidScore,
      out float attackScore,
      float localAdvantage,
      float maxAggressiveness)
    {
      Vec2 v = (enemyParty.BesiegedSettlement != null ? enemyParty.GetVisualPosition().AsVec2 : enemyParty.Position2D) - this.Position2D;
      float length = v.Length;
      bool flag = enemyParty.MapEvent != null || enemyParty.BesiegedSettlement != null;
      float num1 = 1f;
      if (enemyParty.IsCaravan)
        num1 = this.IsBandit ? 2f : (this.Army == null ? 1.5f : 1f);
      else if (enemyParty.IsBandit || (double) enemyParty.Aggressiveness < 0.100000001490116)
        num1 = 0.7f;
      else if ((this.DefaultBehavior == AiBehavior.GoAroundParty || this.ShortTermBehavior == AiBehavior.GoAroundParty) && enemyParty != this.TargetParty)
        num1 = 0.7f;
      int num2 = 0;
      for (int index = 0; index < this._lastTargetedParties.Count; ++index)
      {
        if (enemyParty == this._lastTargetedParties[index])
          ++num2;
      }
      if (num2 > 0)
        num1 *= (float) (3.0 / ((double) num2 + 3.0));
      if (enemyParty.MapEvent == null && (double) this.GetCachedPureSpeed() < (double) enemyParty.GetCachedPureSpeed() * 1.10000002384186 && (this.DefaultBehavior != AiBehavior.GoAroundParty || this.TargetParty != enemyParty) && (this.DefaultBehavior != AiBehavior.DefendSettlement || enemyParty != this.TargetSettlement.LastAttackerParty))
      {
        float b = MathF.Max(0.5f, (float) (((double) this.GetCachedPureSpeed() + 0.100000001490116) / ((double) enemyParty.GetCachedPureSpeed() + 0.100000001490116))) / 1.1f;
        num1 *= MathF.Max(0.8f, b) * MathF.Max(0.8f, b);
      }
      float num3 = this.IsCaravan || this.IsVillager ? 0.9f : 0.7f;
      float num4 = 4.8f * num1;
      float num5 = 4.8f * num3;
      float num6 = (float) ((double) num4 * ((1.0 + (this.Army == null || this.Army.LeaderParty == null || enemyParty.BesiegedSettlement != this.Army.LeaderParty.TargetSettlement && (this.Army.LeaderParty.TargetSettlement == null || enemyParty != this.Army.LeaderParty.TargetSettlement.LastAttackerParty) ? (double) this.AttackInitiative : 1.0)) / 2.0) * (enemyParty.Army != null ? (double) MathF.Pow((float) enemyParty.Army.Parties.Count, 0.33f) : 1.0));
      float num7 = num5 * (float) ((1.0 + (double) this.AvoidInitiative) / 2.0);
      if (flag || this.DefaultBehavior == AiBehavior.EngageParty && this.TargetParty == enemyParty || this.DefaultBehavior == AiBehavior.GoAroundParty && this.TargetParty == enemyParty)
      {
        num7 = 2.88f;
        num6 = 7.2f;
      }
      float num8 = num6 / (length + 1E-05f);
      float num9 = num7 / (length + 1E-05f);
      float num10 = MBMath.ClampFloat(!flag ? num8 * num8 * num8 : 1f, 0.05f, 1f);
      float num11 = MBMath.ClampFloat(num9 * num9 * num9, 0.05f, 1f);
      float num12 = 1f;
      float num13 = 1f;
      if (enemyParty.IsMoving && (this.DefaultBehavior != AiBehavior.GoAroundParty || this.TargetParty != enemyParty))
      {
        float num14 = (float) (((double) this.GetCachedPureSpeed() + 0.100000001490116) / ((double) enemyParty.GetCachedPureSpeed() + 0.100000001490116));
        float num15 = enemyParty.IsLordParty ? 4.5f : 3f;
        float num16;
        if ((double) num14 < 1.10000002384186 && (double) length < (double) num15 && (double) length > 0.5)
        {
          float f = enemyParty.Bearing.DotProduct(v);
          float num17 = 2f;
          float num18 = num17 * 0.5f;
          if ((double) f > (double) num17)
            num13 = 0.0f;
          else if ((double) f > (double) num17 * 0.5)
            num13 = (float) (1.0 - ((double) f - (double) num18) / ((double) num17 - (double) num18));
          else if ((double) f < 0.0)
            num13 = (float) (1.0 + (1.0 + (1.10000002384186 / (double) num14 - 1.0) * (double) MathF.Min(1f, MathF.Abs(f) / 3f) - 1.0) * (1.0 - (double) MathF.Max(0.0f, length - num15 * 0.5f) / (double) num15 * 0.5 * 0.670000016689301));
          num16 = MathF.Pow(MathF.Pow(num14 / 1.1f, 1f - MathF.Max(0.8f, 0.5f * num13)), MathF.Min(2.5f, length - 1f));
        }
        else
          num16 = 1f;
        num12 = MBMath.ClampFloat(num16, 0.0001f, 1f);
      }
      float num19 = MBMath.ClampFloat((float) (0.5 * (1.0 + (double) localAdvantage)), 0.05f, 2f);
      float num20 = MBMath.ClampFloat((double) localAdvantage < 1.0 ? MBMath.ClampFloat(1f / localAdvantage, 0.05f, 2f) : 0.0f, 0.05f, 2f);
      float stanceScore = this.CalculateStanceScore(enemyParty);
      float num21 = !enemyParty.IsLordParty || enemyParty.LeaderHero == null || !enemyParty.LeaderHero.IsNoble ? this.AttackInitiative : 1f;
      if ((double) this.Aggressiveness < 0.01)
        maxAggressiveness = this.Aggressiveness;
      float num22 = enemyParty.MapEvent == null || (double) maxAggressiveness <= 0.100000001490116 ? maxAggressiveness : MathF.Max((float) (1.0 + (enemyParty.MapEvent.IsSallyOut ? 0.300000011920929 : 0.0)), maxAggressiveness);
      float num23 = this.DefaultBehavior != AiBehavior.DefendSettlement || (enemyParty.BesiegedSettlement == null || this.AiBehaviorObject != enemyParty.BesiegedSettlement.Party) && (enemyParty.MapEvent == null || enemyParty.MapEvent.MapEventSettlement == null || this.AiBehaviorObject != enemyParty.MapEvent.MapEventSettlement.Party) ? 1f : 1.1f;
      attackScore = this.CanAttack(enemyParty) ? 1.06f * num23 * num10 * num19 * stanceScore * num12 * num22 * num21 : 0.0f;
      if (this.Army != null && this.Army.ArmyType == Army.ArmyTypes.Defender && enemyParty.MapEvent != null)
      {
        Settlement mapEventSettlement = enemyParty.MapEvent.MapEventSettlement;
        IMapPoint aiBehaviorObject = this.Army.AiBehaviorObject;
      }
      float num24 = !enemyParty.IsLordParty || enemyParty.LeaderHero == null || !enemyParty.LeaderHero.IsNoble ? this.AvoidInitiative : 1f;
      if ((double) attackScore < 1.0)
        avoidScore = this.CanAvoid(enemyParty) ? (float) (0.943396270275116 * (double) num24 * (double) num11 * ((double) stanceScore > 0.00999999977648258 ? 1.0 : 0.0)) * num20 : 0.0f;
      else
        avoidScore = 0.0f;
    }

    public bool ComputeIsWaiting()
    {
      if ((double) (2f * this.Position2D - this.TargetPosition - this._nextTargetPosition).LengthSquared < 9.99999993922529E-09 || this.DefaultBehavior == AiBehavior.Hold)
        return true;
      return (this.DefaultBehavior == AiBehavior.EngageParty || this.DefaultBehavior == AiBehavior.EscortParty) && this.AiBehaviorObject != null && this.AiBehaviorObject.IsValid && this.AiBehaviorObject.IsActive && this.AiBehaviorObject.IsMobile && this.AiBehaviorObject.MobileParty.CurrentSettlement != null;
    }

    bool IMapEntity.ShowCircleAroundEntity => true;

    void IMapEntity.OnOpenEncyclopedia()
    {
      if (!this.IsLordParty)
        return;
      BOFCampaign.Current.EncyclopediaManager.GoToLink(this.LeaderHero.EncyclopediaLink);
    }

    bool IMapEntity.OnMapClick(bool followModifierUsed)
    {
      if (this.IsMainParty)
        MobileParty.MainParty.SetMoveModeHold();
      else if (followModifierUsed)
        MobileParty.MainParty.SetMoveEscortParty(this);
      else
        MobileParty.MainParty.SetMoveEngageParty(this);
      return true;
    }

    void IMapEntity.OnHover()
    {
      if (this.Army?.LeaderParty == this)
        InformationManager.AddTooltipInformation(typeof (Army), (object) this.Army, (object) false, (object) true);
      else
        InformationManager.AddTooltipInformation(typeof (MobileParty), (object) this, (object) false, (object) true);
    }

    bool IMapEntity.IsEnemyOf(IFaction faction) => FactionManager.IsAtWarAgainstFaction(this.MapFaction, faction);

    bool IMapEntity.IsAllyOf(IFaction faction) => FactionManager.IsAlliedWithFaction(this.MapFaction, faction);

    public void GetMountAndHarnessVisualIdsForPartyIcon(
      out string mountStringId,
      out string harnessStringId)
    {
      mountStringId = "";
      harnessStringId = "";
      this._partyComponent?.GetMountAndHarnessVisualIdsForPartyIcon(this.Party, out mountStringId, out harnessStringId);
    }

    Vec2 IMapEntity.InteractionPosition => this.Position2D;

    bool IMapEntity.IsMobileEntity => true;

    IMapEntity IMapEntity.AttachedEntity => (IMapEntity) this.AttachedTo;

    IPartyVisual IMapEntity.PartyVisual => this.Party.Visuals;

    bool IMapEntity.IsMainEntity() => this.IsMainParty;

    public void InitializePartyTrade(int initialGold)
    {
      this.IsPartyTradeActive = true;
      this.PartyTradeGold = initialGold;
    }

    public void CheckPartyNeedsUpdate()
    {
      if (!this._defaultBehaviorNeedsUpdate)
        return;
      this.TickAi(0.0f);
      EncounterManager.HandleEncounterForMobileParty(this, 0.0f);
    }

    public void AddTaxGold(int amount) => this.PartyTradeTaxGold += amount;

    public static MobileParty CreateParty(
      string stringId,
      PartyComponent component = null,
      PartyComponent.OnPartyComponentCreatedDelegate delegateFunction = null)
    {
      stringId = BOFCampaign.Current.CampaignObjectManager.FindNextUniqueStringId<MobileParty>(stringId);
      MobileParty mobileParty = new MobileParty();
      mobileParty.StringId = stringId;
      mobileParty._partyComponent = component;
      mobileParty.UpdatePartyComponentFlags();
      component?.SetMobilePartyInternal(mobileParty);
      if (delegateFunction != null)
        delegateFunction(mobileParty);
      mobileParty.PartyComponent?.Initialize(mobileParty);
      BOFCampaign.Current.CampaignObjectManager.AddMobileParty(mobileParty);
      CampaignEventDispatcher.Instance.OnMobilePartyCreated(mobileParty);
      return mobileParty;
    }

    public CaravanPartyComponent CaravanPartyComponent => this._partyComponent as CaravanPartyComponent;

    public WarPartyComponent WarPartyComponent => this._partyComponent as WarPartyComponent;

    public BanditPartyComponent BanditPartyComponent => this._partyComponent as BanditPartyComponent;

    public CommonAreaPartyComponent CommonAreaPartyComponent => this._partyComponent as CommonAreaPartyComponent;

    public LordPartyComponent LordPartyComponent => this._partyComponent as LordPartyComponent;

    public PartyComponent PartyComponent
    {
      get => this._partyComponent;
      set
      {
        if (this._partyComponent == value)
          return;
        if (this._partyComponent != null)
          this._partyComponent.Finish();
        this._partyComponent = value;
        this.UpdatePartyComponentFlags();
        if (this._partyComponent == null)
          return;
        this._partyComponent.Initialize(this);
      }
    }

    //[CachedData]
    public bool IsLordParty { get; private set; }

    private void UpdatePartyComponentFlags()
    {
      this.IsLordParty = this._partyComponent is LordPartyComponent;
      this.IsVillager = this._partyComponent is VillagerPartyComponent;
      this.IsMilitia = this._partyComponent is MilitiaPartyComponent;
      this.IsCaravan = this._partyComponent is CaravanPartyComponent;
      this.IsGarrison = this._partyComponent is GarrisonPartyComponent;
      this.IsCommonAreaParty = this._partyComponent is CommonAreaPartyComponent;
      this.IsCustomParty = this._partyComponent is CustomPartyComponent;
      this.IsBandit = this._partyComponent is BanditPartyComponent;
    }

    //[CachedData]
    public bool IsVillager { get; private set; }

    //[CachedData]
    public bool IsMilitia { get; private set; }

    //[CachedData]
    public bool IsCaravan { get; private set; }

    //[CachedData]
    public bool IsGarrison { get; private set; }

    //[CachedData]
    public bool IsCommonAreaParty { get; private set; }

    //[CachedData]
    public bool IsCustomParty { get; private set; }

    //[CachedData]
    public bool IsBandit { get; private set; }

    public bool IsBanditBossParty => this.IsBandit && this.BanditPartyComponent.IsBossParty;

    public enum PartyObjective
    {
      Neutral,
      Defensive,
      Aggressive,
      NumberOfPartyObjectives,
    }

    internal struct TickLocalVariables
    {
      internal bool isArmyMember;
      internal bool hasMapEvent;
      internal float nextMoveDistance;
      internal Vec2 currentPosition;
      internal Vec2 lastCurrentPosition;
      internal Vec2 nextPosition;
      internal PathFaceRecord nextPathFaceRecord;
    }
  }
}
