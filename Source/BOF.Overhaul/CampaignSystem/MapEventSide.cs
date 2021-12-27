using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace BOF.CampaignSystem.CampaignSystem
{
  public class MapEventSide
  {
    //[CachedData]
    private PriorityQueue<int, KeyValuePair<UniqueTroopDescriptor, MapEventParty>> _readyTroopsPriorityQueue;
    //[CachedData]
    private Dictionary<UniqueTroopDescriptor, MapEventParty> _allocatedTroops;
    //[SaveableField(30)]
    private List<MapEventParty> _battleParties;
    //[SaveableField(9)]
    public float StrengthRatio = 1f;
    //[SaveableField(10)]
    public float RenownValue;
    //[SaveableField(11)]
    public float InfluenceValue;
    //[SaveableField(14)]
    public int Casualties;
    //[SaveableField(16)]
    private readonly MapEvent _mapEvent;
    //[CachedData]
    private List<UniqueTroopDescriptor> _simulationTroopList;
    //[SaveableField(130)]
    private IFaction _mapFaction;
    //[SaveableField(23)]
    private int _selectedSimulationTroopIndex;
    //[SaveableField(24)]
    private UniqueTroopDescriptor _selectedSimulationTroopDescriptor;
    //[SaveableField(25)]
    private CharacterObject _selectedSimulationTroop;
    private TroopUpgradeTracker _troopUpgradeTracker;
    //[SaveableField(26)]
    private bool IsSurrendered;
    //[SaveableField(27)]
    private List<MobileParty> _nearbyPartiesAddedToPlayerMapEvent = new List<MobileParty>();

    //[SaveableProperty(4)]
    public PartyBase LeaderParty { get; set; }

    public MBReadOnlyList<MapEventParty> Parties { get; private set; }

    //[SaveableProperty(7)]
    public BattleSideEnum MissionSide { get; private set; }

    private IBattleObserver BattleObserver => this._mapEvent.BattleObserver;

    public int TroopCount => this.RecalculateMemberCountOfSide();

    public int CountTroops(Func<FlattenedTroopRosterElement, bool> pred)
    {
      int num = 0;
      foreach (MapEventParty battleParty in this._battleParties)
      {
        foreach (FlattenedTroopRosterElement troop in battleParty.Troops)
        {
          if (pred(troop))
            ++num;
        }
      }
      return num;
    }

    public int NumRemainingSimulationTroops
    {
      get
      {
        List<UniqueTroopDescriptor> simulationTroopList = this._simulationTroopList;
        return simulationTroopList == null ? 0 : __nonvirtual (simulationTroopList.Count);
      }
    }

    //[SaveableProperty(15)]
    public float CasualtyStrength { get; private set; }

    public MapEvent MapEvent => this._mapEvent;

    public MapEventSide OtherSide => this._mapEvent.GetMapEventSide(this.MissionSide == BattleSideEnum.Defender ? BattleSideEnum.Attacker : BattleSideEnum.Defender);

    public IFaction MapFaction => this._mapFaction ?? this.LeaderParty.MapFaction;

    public MapEventSide(MapEvent mapEvent, BattleSideEnum missionSide, PartyBase leaderParty)
    {
      this._mapEvent = mapEvent;
      this.LeaderParty = leaderParty;
      this.MissionSide = missionSide;
      this._mapFaction = leaderParty.MapFaction;
      this._battleParties = new List<MapEventParty>();
      this.Parties = new MBReadOnlyList<MapEventParty>(this._battleParties);
    }

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    {
      if (this._nearbyPartiesAddedToPlayerMapEvent == null)
        this._nearbyPartiesAddedToPlayerMapEvent = new List<MobileParty>();
      if (this._battleParties == null)
      {
        this._battleParties = new List<MapEventParty>();
        if (objectLoadData.GetDataValueBySaveId(5) is Dictionary<PartyBase, MapEventParty> dataValueBySaveId2)
        {
          foreach (MapEventParty mapEventParty in dataValueBySaveId2.Values)
            this._battleParties.Add(mapEventParty);
        }
      }
      this.Parties = new MBReadOnlyList<MapEventParty>(this._battleParties);
    }

    public void OnGameInitialized()
    {
      foreach (MapEventParty party in this.Parties)
        party.OnGameInitialized();
    }

    public void AddPartyInternal(PartyBase party)
    {
      this._battleParties.Add(new MapEventParty(party));
      this._mapEvent.AddInvolvedPartyInternal(party, this.MissionSide);
    }

    public void RemovePartyInternal(PartyBase party)
    {
      this._battleParties.RemoveAt(this._battleParties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (p => p.Party == party)));
      this._mapEvent.RemoveInvolvedPartyInternal(party);
    }

    private static bool TroopCanJoinBattle(FlattenedTroopRosterElement rosterElement) => !rosterElement.IsWounded && !rosterElement.IsRouted && !rosterElement.IsKilled;

    public int RecalculateMemberCountOfSide()
    {
      int num = 0;
      foreach (MapEventParty party in this.Parties)
        num += party.Party.NumberOfHealthyMembers;
      return num;
    }

    public float RecalculateStrengthOfSide()
    {
      float num = 0.0f;
      foreach (MapEventParty party in this.Parties)
        num += party.Party.TotalStrength;
      return num;
    }

    public void DistributeLootAmongWinners(LootCollector lootCollector)
    {
      int totalContribution = this.CalculateTotalContribution();
      lootCollector.MakeFreedHeroesEscape(lootCollector.LootedPrisoners, this.MapEvent.IsPlayerMapEvent && this.MapEvent.PlayerSide == this.MapEvent.WinningSide);
      bool flag = this.MapEvent.IsSiegeAssault || this.MapEvent.IsSallyOut;
      if (flag)
      {
        int indexQ = this._battleParties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (x => x.Party == PartyBase.MainParty));
        if (indexQ != -1)
        {
          MapEventParty battleParty = this._battleParties[indexQ];
          int giveShareToParty = this.CalculateContributionAndGiveShareToParty(lootCollector, battleParty, totalContribution);
          totalContribution -= giveShareToParty;
        }
        for (int index = lootCollector.LootedMembers.Count - 1; index >= 0; --index)
        {
          TroopRosterElement elementCopyAtIndex = lootCollector.LootedMembers.GetElementCopyAtIndex(index);
          Hero heroObject = elementCopyAtIndex.Character.HeroObject;
          if (heroObject != null)
          {
            lootCollector.LootedMembers.RemoveTroop(elementCopyAtIndex.Character);
            TakePrisonerAction.Apply(this.MapEvent.MapEventSettlement.Party, heroObject);
          }
        }
        this.MapEvent.MapEventSettlement.Party.PrisonRoster.Add(lootCollector.LootedMembers);
        lootCollector.LootedMembers.Clear();
      }
      if ((double) totalContribution > 1.0000000116861E-07)
      {
        MapEventParty[] array = new MapEventParty[this._battleParties.Count];
        this._battleParties.CopyTo(array);
        for (int index = 0; index < array.Length; ++index)
        {
          MapEventParty partyRec = array[index];
          if (!flag || partyRec.Party != PartyBase.MainParty)
          {
            int giveShareToParty = this.CalculateContributionAndGiveShareToParty(lootCollector, partyRec, totalContribution);
            totalContribution -= giveShareToParty;
          }
        }
      }
      lootCollector.MakeRemainingPrisonerHeroesEscape();
    }

    private int CalculateContributionAndGiveShareToParty(
      LootCollector lootCollector,
      MapEventParty partyRec,
      int totalContribution)
    {
      if (partyRec.Party.MemberRoster.Count <= 0)
        return 0;
      float lootAmount = (float) partyRec.ContributionToBattle / (float) totalContribution;
      lootCollector.GiveShareOfLootToParty(partyRec.RosterToReceiveLootMembers, partyRec.RosterToReceiveLootPrisoners, partyRec.RosterToReceiveLootItems, partyRec.Party, lootAmount, this._mapEvent);
      return partyRec.ContributionToBattle;
    }

    public bool IsMainPartyAmongParties() => this.Parties.AnyQ<MapEventParty>((Func<MapEventParty, bool>) (party => party.Party == PartyBase.MainParty));

    public float GetPlayerPartyContributionRate()
    {
      int totalContribution = this.CalculateTotalContribution();
      if (totalContribution == 0)
        return 0.0f;
      int num = 0;
      foreach (MapEventParty battleParty in this._battleParties)
      {
        if (battleParty.Party == PartyBase.MainParty)
        {
          num = battleParty.ContributionToBattle;
          break;
        }
      }
      return (float) num / (float) totalContribution;
    }

    public int CalculateTotalContribution()
    {
      int num = 0;
      foreach (MapEventParty battleParty in this._battleParties)
      {
        if (battleParty.Party.MemberRoster.Count > 0)
          num += battleParty.ContributionToBattle;
      }
      return num;
    }

    public void CalculateRenownAndValorValue(float[] strengthOfSide)
    {
      int missionSide = (int) this.MissionSide;
      int oppositeSide = (int) this.MissionSide.GetOppositeSide();
      float x = 1f;
      float num1 = 1f;
      if (this._mapEvent.IsSiegeAssault)
      {
        float settlementAdvantage = Campaign.Current.Models.CombatSimulationModel.GetSettlementAdvantage(this._mapEvent.MapEventSettlement);
        if (this.MissionSide == BattleSideEnum.Defender)
          num1 = settlementAdvantage;
        else
          x = settlementAdvantage;
      }
      float num2 = this._mapEvent.IsSiegeAssault ? 0.7f : (this._mapEvent.IsSallyOut || this._mapEvent.IsRaid || this._mapEvent.MapEventSettlement != null ? 0.6f : 0.5f);
      this.StrengthRatio = (float) (((double) strengthOfSide[oppositeSide] * (double) MathF.Sqrt(x) + 10.0) / ((double) strengthOfSide[missionSide] * (double) num1 + 10.0));
      this.StrengthRatio = (double) this.StrengthRatio > 10.0 ? 10f : this.StrengthRatio;
      if ((double) strengthOfSide[missionSide] <= 0.0)
        return;
      this.RenownValue = (float) ((double) MathF.Pow(strengthOfSide[oppositeSide] * MathF.Sqrt(x), 0.75f) * (double) MathF.Pow(this.StrengthRatio, 0.45f) * (double) num2 * 0.75);
      this.InfluenceValue = (float) ((double) MathF.Pow(strengthOfSide[oppositeSide] * MathF.Sqrt(x), 0.75f) * (double) MathF.Pow(this.StrengthRatio, 0.15f) * 0.600000023841858);
      if ((double) this.RenownValue > 50.0)
        this.RenownValue = 50f;
      if ((double) this.InfluenceValue <= 50.0)
        return;
      this.InfluenceValue = 50f;
    }

    public void CommitSkillXpGains()
    {
      foreach (MapEventParty battleParty in this._battleParties)
        battleParty.CommitSkillXpGains(this.OtherSide);
    }

    public void CommitXpGains()
    {
      foreach (MapEventParty battleParty in this._battleParties)
        battleParty.CommitXpGain();
    }

    public virtual void DistributeRenown(
      MapEventResultExplainer resultExplainers = null,
      bool forScoreboard = false)
    {
      int totalContribution = this.CalculateTotalContribution();
      float renownValue = this.RenownValue;
      float influenceValue = this.InfluenceValue;
      List<MobileParty> mobilePartyList1 = new List<MobileParty>();
      List<MobileParty> mobilePartyList2 = new List<MobileParty>();
      foreach (MapEventParty battleParty in this._battleParties)
      {
        PartyBase party = battleParty.Party;
        if (party.IsMobile && party.MobileParty.IsVillager)
          mobilePartyList1.Add(party.MobileParty);
        if (party.IsMobile && party.MobileParty.IsCaravan)
          mobilePartyList2.Add(party.MobileParty);
      }
      foreach (MapEventParty battleParty in this._battleParties)
      {
        PartyBase party = battleParty.Party;
        if (totalContribution > 0)
        {
          float contributionShare = (float) battleParty.ContributionToBattle / (float) totalContribution;
          ExplainedNumber explainedNumber1 = new ExplainedNumber(includeDescriptions: true);
          ExplainedNumber explainedNumber2 = new ExplainedNumber(includeDescriptions: true);
          ExplainedNumber explainedNumber3 = new ExplainedNumber(includeDescriptions: true);
          explainedNumber3 = Campaign.Current.Models.BattleRewardModel.CalculateMoraleGainVictory(party, renownValue, contributionShare);
          battleParty.MoraleChange = explainedNumber3.ResultNumber;
          if (resultExplainers != null)
            resultExplainers.MoraleExplainedNumber = explainedNumber3;
          if (party.LeaderHero != null)
          {
            foreach (MobileParty mobileParty in mobilePartyList1)
            {
              if (mobileParty.HomeSettlement.OwnerClan != party.LeaderHero.Clan && !mobileParty.HomeSettlement.OwnerClan.IsEliminated && !party.LeaderHero.Clan.IsEliminated)
              {
                int relationChange1 = MBRandom.RoundRandomized(4f * contributionShare);
                if (relationChange1 > 0)
                  ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mobileParty.HomeSettlement.OwnerClan.Leader, party.LeaderHero.Clan.Leader, relationChange1);
                int relationChange2 = MBRandom.RoundRandomized(2f * contributionShare);
                foreach (Hero notable in mobileParty.HomeSettlement.Notables)
                  ChangeRelationAction.ApplyRelationChangeBetweenHeroes(notable, party.LeaderHero.Clan.Leader, relationChange2);
              }
            }
            foreach (MobileParty mobileParty in mobilePartyList2)
            {
              if (mobileParty.HomeSettlement != null && mobileParty.HomeSettlement.OwnerClan != null && party.LeaderHero != null && mobileParty.HomeSettlement.OwnerClan.Leader.Clan != party.LeaderHero.Clan && mobileParty.Party.Owner != null && mobileParty.Party.Owner != Hero.MainHero && mobileParty.Party.Owner.IsAlive && party.LeaderHero.Clan.Leader != null && party.LeaderHero.Clan.Leader.IsAlive && !mobileParty.IsCurrentlyUsedByAQuest)
              {
                int relationChange = MBRandom.RoundRandomized(6f * contributionShare);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mobileParty.Party.Owner, party.LeaderHero.Clan.Leader, relationChange);
              }
            }
            if (this.MapEvent.IsRaid && this.MissionSide == BattleSideEnum.Defender && this == this.MapEvent.Winner)
              ChangeRelationAction.ApplyRelationChangeBetweenHeroes(this.MapEvent.MapEventSettlement.Notables.GetRandomElement<Hero>(), party.LeaderHero, 5);
          }
          if (party.LeaderHero != null)
          {
            explainedNumber1 = Campaign.Current.Models.BattleRewardModel.CalculateRenownGain(party, renownValue, contributionShare);
            explainedNumber2 = Campaign.Current.Models.BattleRewardModel.CalculateInfluenceGain(party, influenceValue, contributionShare);
            battleParty.GainedRenown = explainedNumber1.ResultNumber;
            battleParty.GainedInfluence = explainedNumber2.ResultNumber;
            if (resultExplainers != null)
            {
              resultExplainers.InfluenceExplainedNumber = explainedNumber2;
              resultExplainers.RenownExplainedNumber = explainedNumber1;
            }
          }
        }
      }
    }

    public void ApplyRewardsAndChanges()
    {
      foreach (MapEventParty battleParty in this._battleParties)
      {
        PartyBase party = battleParty.Party;
        Hero hero = party == PartyBase.MainParty ? Hero.MainHero : party.LeaderHero;
        if (party.MobileParty != null)
          party.MobileParty.RecentEventsMorale += battleParty.MoraleChange;
        if (hero != null)
        {
          if ((double) battleParty.GainedRenown > 1.0 / 1000.0)
            GainRenownAction.Apply(hero, battleParty.GainedRenown, true);
          if ((double) battleParty.GainedInfluence > 1.0 / 1000.0)
            GainKingdomInfluenceAction.ApplyForBattle(hero, battleParty.GainedInfluence);
        }
        if (hero != null)
        {
          if ((double) battleParty.PlunderedGold > 1.0 / 1000.0)
          {
            if (hero == Hero.MainHero)
            {
              MBTextManager.SetTextVariable("GOLD", battleParty.PlunderedGold);
              InformationManager.AddQuickInformation(GameTexts.FindText("str_plunder_gain_message"));
            }
            GiveGoldAction.ApplyBetweenCharacters((Hero) null, hero, battleParty.PlunderedGold, true);
          }
          if ((double) battleParty.GoldLost > 1.0 / 1000.0)
            GiveGoldAction.ApplyBetweenCharacters(hero, (Hero) null, battleParty.GoldLost, true);
        }
        else if (party.IsMobile && party.MobileParty.IsPartyTradeActive)
        {
          party.MobileParty.PartyTradeGold -= battleParty.GoldLost;
          party.MobileParty.PartyTradeGold += battleParty.PlunderedGold;
        }
      }
    }

    public virtual void CalculatePlunderedGoldShare(
      float totalPlunderedGold,
      MapEventResultExplainer resultExplainers = null)
    {
      int totalContribution = this.CalculateTotalContribution();
      foreach (MapEventParty battleParty in this._battleParties)
      {
        if (totalContribution > 0)
        {
          double num1 = (double) battleParty.ContributionToBattle / (double) totalContribution;
          totalContribution -= battleParty.ContributionToBattle;
          double num2 = (double) totalPlunderedGold;
          int num3 = (int) (num1 * num2);
          totalPlunderedGold -= (float) num3;
          battleParty.PlunderedGold = num3;
        }
      }
    }

    public void UpdatePartiesMoveState()
    {
      foreach (MapEventParty party in this.Parties)
      {
        if (party.Party.IsMobile && party.Party.MobileParty.IsActive && party.Party.MobileParty.CurrentSettlement == null && (this._mapEvent.IsRaid && (double) this._mapEvent.MapEventSettlement.SettlementHitPoints <= 0.0 || this._mapEvent.IsSiegeAssault) && party.Party.MobileParty.Army != null && party.Party.MobileParty.Army.AiBehaviorObject == this._mapEvent.MapEventSettlement)
          party.Party.MobileParty.Army.AIBehavior = Army.AIBehaviorFlags.Unassigned;
      }
    }

    public void HandleMapEventEnd()
    {
      while (this.Parties.Count > 0)
        this.HandleMapEventEndForPartyInternal((this.Parties.FirstOrDefault<MapEventParty>((Func<MapEventParty, bool>) (x => !x.Party.IsMobile || x.Party.MobileParty.Army == null || x.Party.MobileParty.Army.LeaderParty != x.Party.MobileParty)) ?? this.Parties[this.Parties.Count - 1]).Party);
    }

    public void HandleMapEventEndForPartyInternal(PartyBase party)
    {
      IEnumerable<TroopRosterElement> troopRosterElements = party.MemberRoster.GetTroopRoster().WhereQ<TroopRosterElement>((Func<TroopRosterElement, bool>) (x => x.Character.IsHero && x.Character.HeroObject.IsAlive && x.Character.HeroObject.DeathMark == KillCharacterAction.KillCharacterActionDetail.DiedInBattle));
      PartyBase leaderParty = this._mapEvent.GetLeaderParty(party.OpponentSide);
      bool flag = this._mapEvent.IsWinnerSide(party.Side);
      bool attackersRanAway = this._mapEvent.AttackersRanAway;
      party.MapEventSide = (MapEventSide) null;
      foreach (TroopRosterElement troopRosterElement in troopRosterElements)
        KillCharacterAction.ApplyByBattle(troopRosterElement.Character.HeroObject, this.OtherSide.LeaderParty.LeaderHero);
      if (party.IsMobile && (party.NumberOfAllMembers == 0 || !flag && !attackersRanAway && (party.NumberOfHealthyMembers == 0 || this._mapEvent.BattleState != BattleState.None && party.MobileParty.IsMilitia) && (party.MobileParty.Army == null || party.MobileParty.Army.LeaderParty.Party.NumberOfHealthyMembers == 0)) && party != PartyBase.MainParty && party.IsActive && (!party.MobileParty.IsDisbanding || party.MemberRoster.Count == 0))
        DestroyPartyAction.Apply(leaderParty, party.MobileParty);
      if (!party.IsMobile || !party.MobileParty.IsActive || party.MobileParty.CurrentSettlement != null)
        return;
      party.Visuals?.SetMapIconAsDirty();
    }

    public void AddHeroDamage(Hero character, int damage) => character.HitPoints -= damage;

    public void AllocateTroops(
      ref List<UniqueTroopDescriptor> troopsList,
      int number = -1,
      bool includePlayer = false)
    {
      if (troopsList == null)
        troopsList = new List<UniqueTroopDescriptor>();
      else
        troopsList.Clear();
      int num = number >= 0 ? number : 100000000;
      for (int index = 0; index < num && this._readyTroopsPriorityQueue.Count != 0; ++index)
      {
        KeyValuePair<int, KeyValuePair<UniqueTroopDescriptor, MapEventParty>> keyValuePair1 = this._readyTroopsPriorityQueue.Dequeue();
        KeyValuePair<UniqueTroopDescriptor, MapEventParty> keyValuePair2 = keyValuePair1.Value;
        UniqueTroopDescriptor key = keyValuePair2.Key;
        keyValuePair2 = keyValuePair1.Value;
        MapEventParty mapEventParty = keyValuePair2.Value;
        troopsList.Add(key);
        this._allocatedTroops.Add(key, mapEventParty);
        if (this.BattleObserver != null)
        {
          IBattleObserver battleObserver = this.BattleObserver;
          int missionSide = (int) this.MissionSide;
          PartyBase party1 = mapEventParty.Party;
          FlattenedTroopRosterElement troop1 = mapEventParty.Troops[key];
          CharacterObject troop2 = troop1.Troop;
          battleObserver.TroopNumberChanged((BattleSideEnum) missionSide, (IBattleCombatant) party1, (BasicCharacterObject) troop2, 1);
          if (this._troopUpgradeTracker == null)
            this._troopUpgradeTracker = new TroopUpgradeTracker();
          TroopUpgradeTracker troopUpgradeTracker = this._troopUpgradeTracker;
          PartyBase party2 = mapEventParty.Party;
          troop1 = mapEventParty.Troops[key];
          CharacterObject troop3 = troop1.Troop;
          troopUpgradeTracker.AddTrackedTroop(party2, troop3);
        }
      }
    }

    public CharacterObject GetAllocatedTroop(UniqueTroopDescriptor troopDesc0) => this._allocatedTroops[troopDesc0].Troops[troopDesc0].Troop;

    public PartyBase GetAllocatedTroopParty(UniqueTroopDescriptor troopDescriptor) => this._allocatedTroops[troopDescriptor].Party;

    public void OnTroopWounded(UniqueTroopDescriptor troopDesc1)
    {
      MapEventParty allocatedTroop = this._allocatedTroops[troopDesc1];
      allocatedTroop.OnTroopWounded(troopDesc1);
      this.CasualtyStrength += Campaign.Current.Models.MilitaryPowerModel.GetTroopPowerBasedOnContext(allocatedTroop.GetTroop(troopDesc1), this._mapEvent.EventType, this.MissionSide, this.MapEvent.IsPlayerMapEvent && PlayerEncounter.Current != null && PlayerEncounter.Current.BattleSimulation != null);
      ++this.Casualties;
    }

    public void OnTroopKilled(UniqueTroopDescriptor troopDesc1)
    {
      MapEventParty allocatedTroop = this._allocatedTroops[troopDesc1];
      allocatedTroop.OnTroopKilled(troopDesc1);
      this.CasualtyStrength += Campaign.Current.Models.MilitaryPowerModel.GetTroopPowerBasedOnContext(allocatedTroop.GetTroop(troopDesc1), this._mapEvent.EventType, this.MissionSide, this.MapEvent.IsPlayerMapEvent && PlayerEncounter.Current != null && PlayerEncounter.Current.BattleSimulation != null);
      ++this.Casualties;
    }

    public void OnTroopRouted(UniqueTroopDescriptor troopDesc1)
    {
      MapEventParty allocatedTroop = this._allocatedTroops[troopDesc1];
      allocatedTroop.OnTroopRouted(troopDesc1);
      this.CasualtyStrength += Campaign.Current.Models.MilitaryPowerModel.GetTroopPowerBasedOnContext(allocatedTroop.GetTroop(troopDesc1), this._mapEvent.EventType, this.MissionSide, this.MapEvent.IsPlayerMapEvent && PlayerEncounter.Current != null && PlayerEncounter.Current.BattleSimulation != null) * 0.1f;
    }

    public void OnTroopScoreHit(
      UniqueTroopDescriptor troopDesc1,
      CharacterObject attackedTroop,
      int damage,
      bool isFatal,
      bool isTeamKill,
      WeaponComponentData attackerWeapon,
      bool isSimulatedHit)
    {
      this._allocatedTroops[troopDesc1].OnTroopScoreHit(troopDesc1, attackedTroop, damage, isFatal, isTeamKill, attackerWeapon, isSimulatedHit);
    }

    private void MakeReady(bool includeHumanPlayers, FlattenedTroopRoster priorTroops = null)
    {
      if (this._readyTroopsPriorityQueue == null || this._allocatedTroops == null)
      {
        this._readyTroopsPriorityQueue = new PriorityQueue<int, KeyValuePair<UniqueTroopDescriptor, MapEventParty>>();
        this._allocatedTroops = new Dictionary<UniqueTroopDescriptor, MapEventParty>();
      }
      else
      {
        this._readyTroopsPriorityQueue.Clear();
        this._allocatedTroops.Clear();
      }
      int sizeOfSide = 0;
      foreach (MapEventParty battleParty in this._battleParties)
        sizeOfSide += battleParty.Party.NumberOfHealthyMembers;
      foreach (MapEventParty battleParty in this._battleParties)
        this.MakeReadyParty(battleParty, priorTroops, includeHumanPlayers, sizeOfSide);
    }

    private void MakeReadyParty(
      MapEventParty battleParty,
      FlattenedTroopRoster priorityTroops,
      bool includePlayers,
      int sizeOfSide)
    {
      battleParty.Update();
      int ofHealthyMembers = battleParty.Party.NumberOfHealthyMembers;
      foreach (FlattenedTroopRosterElement troop in battleParty.Troops)
      {
        if (MapEventSide.TroopCanJoinBattle(troop))
        {
          int num1 = 1;
          if (troop.Troop.IsHero && (includePlayers || !troop.Troop.HeroObject.IsHumanPlayerCharacter))
          {
            int priority = num1 * 150;
            if (priorityTroops != null)
            {
              UniqueTroopDescriptor indexOfCharacter = priorityTroops.FindIndexOfCharacter(troop.Troop);
              if (indexOfCharacter.IsValid)
              {
                priority *= 100;
                priorityTroops.Remove(indexOfCharacter);
              }
            }
            else if (troop.Troop.HeroObject.IsHumanPlayerCharacter)
              priority *= 10;
            this._readyTroopsPriorityQueue.Enqueue(priority, new KeyValuePair<UniqueTroopDescriptor, MapEventParty>(troop.Descriptor, battleParty));
          }
          else if (!troop.Troop.IsHero)
          {
            int num2 = 0;
            int num3 = 0;
            for (int index = 0; index < battleParty.Party.MemberRoster.Count; ++index)
            {
              TroopRosterElement elementCopyAtIndex = battleParty.Party.MemberRoster.GetElementCopyAtIndex(index);
              if (!elementCopyAtIndex.Character.IsHero)
              {
                if (elementCopyAtIndex.Character == troop.Troop)
                {
                  num2 = index - num3;
                  break;
                }
              }
              else
                ++num3;
            }
            int num4 = (int) (100.0 / (double) MathF.Pow(1.2f, (float) num2));
            if (num4 < 10)
              num4 = 10;
            int num5 = ofHealthyMembers / sizeOfSide * 100;
            if (num5 < 10)
              num5 = 10;
            int num6 = 0;
            if (priorityTroops != null)
            {
              UniqueTroopDescriptor indexOfCharacter = priorityTroops.FindIndexOfCharacter(troop.Troop);
              if (indexOfCharacter.IsValid)
              {
                num6 = 20000;
                priorityTroops.Remove(indexOfCharacter);
              }
            }
            this._readyTroopsPriorityQueue.Enqueue(num6 + MBRandom.RandomInt((int) ((double) num4 * 0.5 + (double) num5 * 0.5)), new KeyValuePair<UniqueTroopDescriptor, MapEventParty>(troop.Descriptor, battleParty));
          }
        }
      }
    }

    public void MakeReadyForSimulation()
    {
      this.MakeReady(false);
      this.AllocateTroops(ref this._simulationTroopList);
    }

    public void MakeReadyForMission(FlattenedTroopRoster priorTroops) => this.MakeReady(true, priorTroops);

    public void EndSimulation()
    {
      this._simulationTroopList.Clear();
      this._readyTroopsPriorityQueue.Clear();
      this._allocatedTroops.Clear();
    }

    public void ResetContributionToBattleToStrength()
    {
      foreach (MapEventParty battleParty in this._battleParties)
        battleParty.ResetContributionToBattleToStrength();
    }

    public void CollectAll(LootCollector lootCollector, out bool playerCaptured)
    {
      playerCaptured = false;
      bool flag1 = false;
      ExplainedNumber bonuses = new ExplainedNumber(1f);
      float num1 = 0.0f;
      foreach (MapEventParty party in this.OtherSide.Parties)
      {
        if (party != null)
        {
          bool? nullable = party.Party?.MobileParty?.HasPerk(DefaultPerks.Roguery.KnowHow);
          bool flag2 = true;
          if (nullable.GetValueOrDefault() == flag2 & nullable.HasValue)
            flag1 = true;
        }
        if (party?.Party?.LeaderHero != null && party.Party.LeaderHero.GetPerkValue(DefaultPerks.Roguery.RogueExtraordinaire) && (double) num1 < (double) party.Party.LeaderHero.GetSkillValue(DefaultSkills.Roguery))
        {
          num1 = (float) party.Party.LeaderHero.GetSkillValue(DefaultSkills.Roguery);
          PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Roguery.RogueExtraordinaire, party.Party.LeaderHero.CharacterObject, DefaultSkills.Roguery, true, ref bonuses, 200);
        }
      }
      foreach (MapEventParty battleParty in this._battleParties)
      {
        PartyBase party = battleParty.Party;
        if (!this._mapEvent.IsAlleyFight)
          MapEventSide.CaptureWoundedTroops(lootCollector, party, this.IsSurrendered, ref playerCaptured);
        lootCollector.LootedPrisoners.Add(party.PrisonRoster);
        bool flag3 = false;
        for (int index = party.PrisonRoster.Count - 1; index >= 0; --index)
        {
          TroopRosterElement troopRosterElement = party.PrisonRoster.data[index];
          if (!troopRosterElement.Character.IsHero)
          {
            party.PrisonRoster.RemoveTroop(troopRosterElement.Character, troopRosterElement.Number);
            flag3 = true;
          }
        }
        if (flag3)
          party.PrisonRoster.RemoveZeroCounts();
        float num2 = 0.5f * bonuses.ResultNumber;
        if (party.IsMobile)
        {
          if (flag1 && (party.MobileParty.IsCaravan || party.MobileParty.IsVillager))
            num2 *= 1f + DefaultPerks.Roguery.KnowHow.PrimaryBonus;
        }
        else if (party.IsSettlement)
        {
          Settlement settlement = party.Settlement;
          num2 = !settlement.IsTown ? (!settlement.IsVillage ? 1f : ((double) settlement.SettlementHitPoints > 0.0 ? 0.0f : 1f)) : 0.0f;
        }
        float num3 = 1.0 > (double) num2 ? num2 : 1f;
        if (party == PartyBase.MainParty)
        {
          Settlement nearestSettlement = SettlementHelper.FindNearestTown();
          IOrderedEnumerable<ItemRosterElement> orderedEnumerable = party.ItemRoster.Where<ItemRosterElement>((Func<ItemRosterElement, bool>) (x => x.EquipmentElement.Item.IsMountable)).OrderByDescending<ItemRosterElement, int>((Func<ItemRosterElement, int>) (x => nearestSettlement.Town.GetItemPrice(x.EquipmentElement, (MobileParty) null, false)));
          ItemRoster itemRoster = new ItemRoster();
          int num4 = 3;
          int num5 = 0;
          foreach (ItemRosterElement itemRosterElement in (IEnumerable<ItemRosterElement>) orderedEnumerable)
          {
            for (int index = 0; index < itemRosterElement.Amount && num5 < num4; ++index)
            {
              itemRoster.AddToCounts(itemRosterElement.EquipmentElement, 1);
              ++num5;
            }
            if (num5 == num4)
              break;
          }
          ItemRoster source = new ItemRoster(party.ItemRoster);
          foreach (ItemRosterElement itemRosterElement in source)
          {
            if (!itemRosterElement.EquipmentElement.Item.NotMerchandise && !itemRosterElement.EquipmentElement.IsQuestItem && !itemRosterElement.EquipmentElement.Item.IsBannerItem)
            {
              int number = MBRandom.RoundRandomized((float) itemRosterElement.Amount * num3);
              lootCollector.LootedItems.AddToCounts(itemRosterElement.EquipmentElement, number);
              party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -itemRosterElement.Amount);
            }
          }
          foreach (ItemRosterElement itemRosterElement in itemRoster)
            party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, itemRosterElement.Amount);
          float num6 = MBMath.ClampFloat((float) party.InventoryCapacity - party.ItemRoster.TotalWeight, 0.0f, (float) short.MaxValue);
          float num7 = 0.0f;
          using (IEnumerator<ItemRosterElement> enumerator = source.Where<ItemRosterElement>((Func<ItemRosterElement, bool>) (x => !x.EquipmentElement.Item.NotMerchandise)).OrderByDescending<ItemRosterElement, int>((Func<ItemRosterElement, int>) (x => nearestSettlement.Town.GetItemPrice(x.EquipmentElement, (MobileParty) null, false))).GetEnumerator())
          {
label_58:
            while (enumerator.MoveNext())
            {
              ItemRosterElement current = enumerator.Current;
              float equipmentElementWeight = current.EquipmentElement.GetEquipmentElementWeight();
              int num8 = 0;
              while (true)
              {
                if (num8 < current.Amount && (double) num7 + (double) equipmentElementWeight < (double) num6)
                {
                  if (MBRandom.RandomInt(0, 2) == 0)
                  {
                    party.ItemRoster.AddToCounts(current.EquipmentElement, 1);
                    num7 += equipmentElementWeight;
                  }
                  ++num8;
                }
                else
                  goto label_58;
              }
            }
          }
        }
        else
        {
          foreach (ItemRosterElement itemRosterElement in new ItemRoster(party.ItemRoster))
          {
            if (!itemRosterElement.EquipmentElement.Item.NotMerchandise && !itemRosterElement.EquipmentElement.IsQuestItem)
            {
              int number = MBRandom.RoundRandomized((float) ((double) itemRosterElement.Amount * (double) num3 * (itemRosterElement.EquipmentElement.Item.IsMountable ? 0.330000013113022 : 1.0)));
              if (number > 0)
              {
                lootCollector.LootedItems.AddToCounts(itemRosterElement.EquipmentElement, number);
                party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement, -number);
              }
            }
          }
        }
        lootCollector.CasualtiesInBattle.Add(battleParty.DiedInBattle);
        lootCollector.CasualtiesInBattle.Add(battleParty.WoundedInBattle);
        MapEventSide.OnPartyDefeated(party);
      }
    }

    private static void OnPartyDefeated(PartyBase defeatedParty)
    {
      if (!defeatedParty.IsMobile)
        return;
      defeatedParty.MobileParty.RecentEventsMorale += Campaign.Current.Models.PartyMoraleModel.GetDefeatMoraleChange(defeatedParty);
      if (defeatedParty.NumberOfHealthyMembers <= 0)
        return;
      Vec2 pointAroundPosition = MobilePartyHelper.FindReachablePointAroundPosition(defeatedParty, defeatedParty.MobileParty.Position2D, 4f, 3f);
      defeatedParty.MobileParty.Position2D = pointAroundPosition;
      defeatedParty.MobileParty.ForceDefaultBehaviorUpdate();
    }

    private static void CaptureWoundedTroops(
      LootCollector lootCollector,
      PartyBase defeatedParty,
      bool isSurrender,
      ref bool playerCaptured)
    {
      MapEventSide.CaptureRegularTroops(lootCollector, defeatedParty, isSurrender);
      if (defeatedParty == PartyBase.MainParty)
      {
        bool playerCaptured1;
        MapEventSide.CaptureWoundedHeroesForMainParty(lootCollector, defeatedParty, isSurrender, out playerCaptured1);
        MobileParty.MainParty.MemberRoster.Clear();
        if (playerCaptured1)
          playerCaptured = true;
      }
      else if (defeatedParty.LeaderHero != null)
        MapEventSide.CaptureWoundedHeroes(lootCollector, defeatedParty, isSurrender);
      defeatedParty.MemberRoster.RemoveZeroCounts();
    }

    private static void CaptureWoundedHeroesForMainParty(
      LootCollector lootCollector,
      PartyBase defeatedParty,
      bool isSurrender,
      out bool playerCaptured)
    {
      playerCaptured = false;
      bool flag = false;
      if (defeatedParty != PartyBase.MainParty)
      {
        foreach (TroopRosterElement troopRosterElement in defeatedParty.MemberRoster.GetTroopRoster())
        {
          if (troopRosterElement.Character != null && troopRosterElement.Character.IsHero && !troopRosterElement.Character.HeroObject.IsWounded)
            flag = true;
        }
      }
      if (!(!flag | isSurrender))
        return;
      playerCaptured = true;
      for (int index = 0; index < defeatedParty.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = defeatedParty.MemberRoster.GetElementCopyAtIndex(index);
        if (elementCopyAtIndex.Character.IsHero)
        {
          if (elementCopyAtIndex.Character.HeroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
          {
            defeatedParty.MemberRoster.AddToCountsAtIndex(index, -1, removeDepleted: false);
            if (elementCopyAtIndex.Character.HeroObject != Hero.MainHero && (double) MBRandom.RandomFloat < 0.5)
            {
              MakeHeroFugitiveAction.Apply(elementCopyAtIndex.Character.HeroObject);
              ScatterCompanionAction.ApplyInPrison(elementCopyAtIndex.Character.HeroObject);
            }
            else if (!elementCopyAtIndex.Character.HeroObject.IsDead)
              lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, 1);
            if (defeatedParty.LeaderHero == elementCopyAtIndex.Character.HeroObject && defeatedParty.IsMobile)
              defeatedParty.MobileParty.RemovePartyLeader();
          }
        }
        else if (elementCopyAtIndex.Number > 0)
        {
          defeatedParty.MemberRoster.AddToCountsAtIndex(index, -elementCopyAtIndex.Number, removeDepleted: false);
          lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number);
        }
      }
    }

    private static void CaptureRegularTroops(
      LootCollector lootCollector,
      PartyBase defeatedParty,
      bool isSurrender)
    {
      for (int index = 0; index < defeatedParty.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = defeatedParty.MemberRoster.GetElementCopyAtIndex(index);
        if (!elementCopyAtIndex.Character.IsHero && (elementCopyAtIndex.WoundedNumber > 0 || isSurrender && elementCopyAtIndex.Number > 0))
        {
          int count = isSurrender ? elementCopyAtIndex.Number : elementCopyAtIndex.WoundedNumber;
          lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, count);
          defeatedParty.MemberRoster.AddToCountsAtIndex(index, -count, -elementCopyAtIndex.WoundedNumber, removeDepleted: false);
        }
      }
    }

    private static void CaptureWoundedHeroes(
      LootCollector lootCollector,
      PartyBase defeatedParty,
      bool isSurrender)
    {
      if (!(defeatedParty.LeaderHero.IsWounded | isSurrender))
        return;
      for (int index = 0; index < defeatedParty.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = defeatedParty.MemberRoster.GetElementCopyAtIndex(index);
        if (elementCopyAtIndex.Character.IsHero)
        {
          if (elementCopyAtIndex.Character.HeroObject.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
          {
            lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, 1);
            if (defeatedParty.LeaderHero == elementCopyAtIndex.Character.HeroObject && defeatedParty.IsMobile)
              defeatedParty.MobileParty.RemovePartyLeader();
            defeatedParty.MemberRoster.AddToCountsAtIndex(index, -1, removeDepleted: false);
          }
        }
        else if (elementCopyAtIndex.Number > 0)
        {
          lootCollector.LootedMembers.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number);
          defeatedParty.MemberRoster.AddToCountsAtIndex(index, -elementCopyAtIndex.Number, removeDepleted: false);
        }
      }
    }

    public void UpgradeTroops()
    {
      foreach (MapEventParty battleParty in this._battleParties)
      {
        if (battleParty.Party != MobileParty.MainParty.Party)
          Campaign.Current.PartyUpgrader.UpgradeReadyTroops(battleParty.Party);
      }
    }

    public ItemRoster ItemRosterForPlayerLootShare(PartyBase playerParty) => this._battleParties[this._battleParties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (p => p.Party == playerParty))].RosterToReceiveLootItems;

    public TroopRoster MemberRosterForPlayerLootShare(PartyBase playerParty) => this._battleParties[this._battleParties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (p => p.Party == playerParty))].RosterToReceiveLootMembers;

    public TroopRoster PrisonerRosterForPlayerLootShare(PartyBase playerParty) => this._battleParties[this._battleParties.FindIndexQ<MapEventParty>((Func<MapEventParty, bool>) (p => p.Party == playerParty))].RosterToReceiveLootPrisoners;

    public void Clear() => this._battleParties.Clear();

    public UniqueTroopDescriptor SelectRandomSimulationTroop()
    {
      this._selectedSimulationTroopIndex = MBRandom.RandomInt(this.NumRemainingSimulationTroops);
      this._selectedSimulationTroopDescriptor = this._simulationTroopList[this._selectedSimulationTroopIndex];
      this._selectedSimulationTroop = this.GetAllocatedTroop(this._selectedSimulationTroopDescriptor);
      return this._selectedSimulationTroopDescriptor;
    }

    private void RemoveSelectedTroopFromSimulationList()
    {
      this._simulationTroopList[this._selectedSimulationTroopIndex] = this._simulationTroopList[this._simulationTroopList.Count - 1];
      this._simulationTroopList.RemoveAt(this._simulationTroopList.Count - 1);
      this._selectedSimulationTroopIndex = -1;
      this._selectedSimulationTroopDescriptor = UniqueTroopDescriptor.Invalid;
      this._selectedSimulationTroop = (CharacterObject) null;
    }

    public bool ApplySimulationDamageToSelectedTroop(
      int damage,
      DamageTypes damageType,
      out MapEvent.SimulationTroopState troopState,
      PartyBase strikerParty)
    {
      troopState = MapEvent.SimulationTroopState.Alive;
      bool flag = false;
      if (this._selectedSimulationTroop.IsHero)
      {
        this.AddHeroDamage(this._selectedSimulationTroop.HeroObject, damage);
        if (this._selectedSimulationTroop.HeroObject.IsWounded)
        {
          if ((double) MBRandom.RandomFloat > (double) Campaign.Current.Models.PartyHealingModel.GetSurvivalChance(this._selectedSimulationTroop.HeroObject.PartyBelongedTo?.Party ?? (PartyBase) null, this._selectedSimulationTroop, damageType, strikerParty) && this._selectedSimulationTroop.HeroObject.CanDie(KillCharacterAction.KillCharacterActionDetail.DiedInBattle))
          {
            this.OnTroopKilled(this._selectedSimulationTroopDescriptor);
            troopState = MapEvent.SimulationTroopState.Killed;
            this.BattleObserver?.TroopNumberChanged(this.MissionSide, (IBattleCombatant) this.GetAllocatedTroopParty(this._selectedSimulationTroopDescriptor), (BasicCharacterObject) this._selectedSimulationTroop, -1, 1);
            KillCharacterAction.ApplyByBattle(this._selectedSimulationTroop.HeroObject, (Hero) null, false);
          }
          else
          {
            this.OnTroopWounded(this._selectedSimulationTroopDescriptor);
            troopState = MapEvent.SimulationTroopState.Wounded;
            this.BattleObserver?.TroopNumberChanged(this.MissionSide, (IBattleCombatant) this.GetAllocatedTroopParty(this._selectedSimulationTroopDescriptor), (BasicCharacterObject) this._selectedSimulationTroop, -1, numberWounded: 1);
          }
          flag = true;
        }
      }
      else if (MBRandom.RandomInt(this._selectedSimulationTroop.MaxHitPoints()) < damage)
      {
        PartyBase party = this._allocatedTroops[this._selectedSimulationTroopDescriptor].Party;
        if ((double) MBRandom.RandomFloat < (double) Campaign.Current.Models.PartyHealingModel.GetSurvivalChance(party, this._selectedSimulationTroop, damageType, strikerParty))
        {
          this.OnTroopWounded(this._selectedSimulationTroopDescriptor);
          troopState = MapEvent.SimulationTroopState.Wounded;
          this.BattleObserver?.TroopNumberChanged(this.MissionSide, (IBattleCombatant) this.GetAllocatedTroopParty(this._selectedSimulationTroopDescriptor), (BasicCharacterObject) this._selectedSimulationTroop, -1, numberWounded: 1);
          SkillLevelingManager.OnSurgeryApplied(party.MobileParty, 1f);
        }
        else
        {
          this.OnTroopKilled(this._selectedSimulationTroopDescriptor);
          troopState = MapEvent.SimulationTroopState.Killed;
          this.BattleObserver?.TroopNumberChanged(this.MissionSide, (IBattleCombatant) this.GetAllocatedTroopParty(this._selectedSimulationTroopDescriptor), (BasicCharacterObject) this._selectedSimulationTroop, -1, 1);
          SkillLevelingManager.OnSurgeryApplied(party.MobileParty, 0.5f);
        }
        flag = true;
      }
      if (flag)
        this.RemoveSelectedTroopFromSimulationList();
      return flag;
    }

    public void ApplySimulatedHitRewardToSelectedTroop(
      CharacterObject strikerTroop,
      CharacterObject attackedTroop,
      int damage,
      bool isFinishingStrike)
    {
      EquipmentElement equipmentElement = strikerTroop.FirstBattleEquipment[EquipmentIndex.WeaponItemBeginSlot];
      this.OnTroopScoreHit(this._selectedSimulationTroopDescriptor, attackedTroop, damage, isFinishingStrike, false, equipmentElement.Item?.PrimaryWeapon, true);
      PartyBase party = this._allocatedTroops[this._selectedSimulationTroopDescriptor].Party;
      if (isFinishingStrike && (!attackedTroop.IsHero || !attackedTroop.HeroObject.IsDead))
        SkillLevelingManager.OnSimulationCombatKill(this._selectedSimulationTroop, attackedTroop, party, this.LeaderParty);
      if (this.BattleObserver == null)
        return;
      if (isFinishingStrike)
        this.BattleObserver.TroopNumberChanged(this.MissionSide, (IBattleCombatant) party, (BasicCharacterObject) this._selectedSimulationTroop, killCount: 1);
      if (this._selectedSimulationTroop.IsHero)
      {
        foreach (SkillObject skill in this._troopUpgradeTracker.CheckSkillUpgrades(this._selectedSimulationTroop.HeroObject).ToList<SkillObject>())
          this.BattleObserver.HeroSkillIncreased(this.MissionSide, (IBattleCombatant) party, (BasicCharacterObject) this._selectedSimulationTroop, skill);
      }
      else
      {
        int numberReadyToUpgrade = this._troopUpgradeTracker.CheckUpgradedCount(party, this._selectedSimulationTroop);
        if (numberReadyToUpgrade == 0)
          return;
        this.BattleObserver.TroopNumberChanged(this.MissionSide, (IBattleCombatant) party, (BasicCharacterObject) this._selectedSimulationTroop, numberReadyToUpgrade: numberReadyToUpgrade);
      }
    }

    public void Surrender()
    {
      MapEventSide.SurrenderParty(this.LeaderParty);
      this.IsSurrendered = true;
    }

    private static void SurrenderParty(PartyBase party)
    {
      for (int index = 0; index < party.MemberRoster.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(index);
        if (!elementCopyAtIndex.Character.IsHero)
          party.MemberRoster.AddToCountsAtIndex(index, 0, elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber);
      }
    }

    public void AddNearbyPartyToPlayerMapEvent(MobileParty party)
    {
      if (party.MapEventSide == this)
        return;
      party.MapEventSide = this;
      this._nearbyPartiesAddedToPlayerMapEvent.Add(party);
    }

    public void RemoveNearbyPartiesFromPlayerMapEvent()
    {
      foreach (MobileParty mobileParty in this._nearbyPartiesAddedToPlayerMapEvent)
        mobileParty.MapEventSide = (MapEventSide) null;
      this._nearbyPartiesAddedToPlayerMapEvent.Clear();
    }
  }
}