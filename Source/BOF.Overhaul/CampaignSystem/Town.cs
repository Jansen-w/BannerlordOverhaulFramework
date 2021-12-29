using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace BOF.Overhaul.CampaignSystem
{
  public class Town : Fief
  {
    private const int InitialTownGold = 20000;
    private const int VeryHighProsperityThreshold = 10000;
    private const int HighProsperityThreshold = 5000;
    private const int MidProsperityThreshold = 2000;
    private const int LowProsperityThreshold = 1000;
    //[SaveableField(1000)]
    private int _wallLevel;
    private bool _isCastle;
    //[SaveableField(1016)]
    public bool GarrisonAutoRecruitmentIsEnabled = true;
    //[SaveableField(1040)]
    private Clan _ownerClan;
    //[SaveableField(1015)]
    private float _security;
    //[SaveableField(1014)]
    private float _loyalty;
    //[SaveableField(1006)]
    public List<Building> Buildings;
    //[SaveableField(1007)]
    public Queue<Building> BuildingsInProgress;
    //[SaveableField(1008)]
    public int BoostBuildingProcess;
    //[SaveableField(1009)]
    private TownMarketData _marketData;
    //[SaveableField(1010)]
    private int _tradeTax;
    //[SaveableField(1011)]
    public bool InRebelliousState;
    //[SaveableField(1012)]
    private Hero _governor;
    //[SaveableField(1013)]
    private Town.SellLog[] _soldItems = new Town.SellLog[0];
    
    public CultureObject Culture => this.Owner.Settlement.Culture;

    public float ProsperityChange => BOFCampaign.Current.Models.SettlementProsperityModel.CalculateProsperityChange(this).ResultNumber;

    public ExplainedNumber ProsperityChangeExplanation => BOFCampaign.Current.Models.SettlementProsperityModel.CalculateProsperityChange(this, true);

    public int GarrisonChange => (int) BOFCampaign.Current.Models.SettlementGarrisonModel.CalculateGarrisonChange(this.Owner.Settlement).ResultNumber;

    public int GarrisonChangeAutoRecruitment => (int) BOFCampaign.Current.Models.SettlementGarrisonModel.CalculateGarrisonChangeAutoRecruitment(this.Owner.Settlement).ResultNumber;

    public ExplainedNumber GarrisonChangeExplanation => BOFCampaign.Current.Models.SettlementGarrisonModel.CalculateGarrisonChange(this.Owner.Settlement, true);

    public float FoodChange => BOFCampaign.Current.Models.SettlementFoodModel.CalculateTownFoodStocksChange(this).ResultNumber;

    public ExplainedNumber FoodChangeExplanation => BOFCampaign.Current.Models.SettlementFoodModel.CalculateTownFoodStocksChange(this, true);

    public float LoyaltyChange => BOFCampaign.Current.Models.SettlementLoyaltyModel.CalculateLoyaltyChange(this).ResultNumber;

    public ExplainedNumber LoyaltyChangeExplanation => BOFCampaign.Current.Models.SettlementLoyaltyModel.CalculateLoyaltyChange(this, true);

    public float SecurityChange => BOFCampaign.Current.Models.SettlementSecurityModel.CalculateSecurityChange(this).ResultNumber;

    public ExplainedNumber SecurityChangeExplanation => BOFCampaign.Current.Models.SettlementSecurityModel.CalculateSecurityChange(this, true);

    public float MilitiaChange => BOFCampaign.Current.Models.SettlementMilitiaModel.CalculateMilitiaChange(this.Owner.Settlement).ResultNumber;

    public ExplainedNumber MilitiaChangeExplanation => BOFCampaign.Current.Models.SettlementMilitiaModel.CalculateMilitiaChange(this.Owner.Settlement, true);

    public float Construction => BOFCampaign.Current.Models.BuildingConstructionModel.CalculateDailyConstructionPower(this).ResultNumber;

    public ExplainedNumber ConstructionExplanation => BOFCampaign.Current.Models.BuildingConstructionModel.CalculateDailyConstructionPower(this, true);

    public Clan OwnerClan
    {
      get
      {
        if (this.Settlement._ownerClanDepricated != null && this._ownerClan == null)
        {
          this._ownerClan = this.Settlement._ownerClanDepricated;
          this.Settlement._ownerClanDepricated = (Clan) null;
        }
        return this._ownerClan;
      }
      set
      {
        if (this._ownerClan == value)
          return;
        this.ChangeClanInternal(value);
      }
    }

    public float Security
    {
      get => this._security;
      set
      {
        this._security = value;
        if ((double) this._security < 0.0)
        {
          this._security = 0.0f;
        }
        else
        {
          if ((double) this._security <= 100.0)
            return;
          this._security = 100f;
        }
      }
    }

    public float Loyalty
    {
      get => this._loyalty;
      set
      {
        this._loyalty = value;
        if ((double) this._loyalty < 0.0)
        {
          this._loyalty = 0.0f;
        }
        else
        {
          if ((double) this._loyalty <= 100.0)
            return;
          this._loyalty = 100f;
        }
      }
    }

    public int FoodStocksUpperLimit() => (int) ((double) (BOFCampaign.Current.Models.SettlementFoodModel.FoodStocksUpperLimit + (this.IsCastle ? BOFCampaign.Current.Models.SettlementFoodModel.CastleFoodStockUpperLimitBonus : 0)) + (double) this.GetEffectOfBuildings(BuildingEffectEnum.Foodstock));

    //[SaveableProperty(1005)]
    public Workshop[] Workshops { get; protected set; }

    public Building CurrentBuilding => !this.BuildingsInProgress.IsEmpty<Building>() ? this.BuildingsInProgress.Peek() : this.CurrentDefaultBuilding;

    public Building CurrentDefaultBuilding => this.Buildings.Find((Predicate<Building>) (k => k.IsCurrentlyDefault));

    public TownMarketData MarketData => this._marketData;

    public int TradeTaxAccumulated
    {
      get => this._tradeTax;
      set => this._tradeTax = value;
    }

    public Hero Governor
    {
      get => this._governor;
      set
      {
        if (this._governor == value)
          return;
        if (this._governor != null)
          this._governor.GovernorOf = (Town) null;
        this._governor = value;
        if (this._governor == null)
          return;
        this._governor.GovernorOf = this;
      }
    }

    public Town()
    {
      this.Buildings = new List<Building>();
      this.BuildingsInProgress = new Queue<Building>();
      this.Workshops = new Workshop[0];
      this._marketData = new TownMarketData(this);
    }

    public static IEnumerable<Town> AllFiefs
    {
      get
      {
        foreach (Town allTown in (IEnumerable<Town>) BOFCampaign.Current.AllTowns)
          yield return allTown;
        foreach (Town allCastle in (IEnumerable<Town>) BOFCampaign.Current.AllCastles)
          yield return allCastle;
      }
    }

    public static IReadOnlyList<Town> AllTowns => BOFCampaign.Current.AllTowns;

    public static IReadOnlyList<Town> AllCastles => BOFCampaign.Current.AllCastles;

    public override bool IsTown => !this._isCastle;

    public override bool IsCastle => this._isCastle;

    public override void OnInit()
    {
      this.Loyalty = this.Owner.Random.GetValue(1337, 30f, 70f);
      this.Security = this.Owner.Random.GetValue(1001, 40f, 60f);
      this.TradeTaxAccumulated = this.IsTown ? 1000 + MBRandom.RandomInt(1000) : 0;
      this.ChangeGold(20000);
      this.Buildings.Add(new Building(this.IsTown ? DefaultBuildingTypes.Fortifications : DefaultBuildingTypes.Wall, this, currentLevel: this._wallLevel));
    }

    public override void OnStart() => this.Owner.Settlement.Town = this;

    public void InitializeWorkshops(int count)
    {
      if (count <= 0)
        return;
      this.Workshops = new Workshop[count];
      for (int index = 0; index < count; ++index)
        this.Workshops[index] = new Workshop(this.Owner.Settlement, "workshop_" + (object) index);
    }

    protected override void PreAfterLoad() => this._ownerClan?.AddFiefInternal(this);

    protected override void AfterLoad()
    {
      foreach (Workshop workshop in this.Workshops)
        workshop.AfterLoad();
      if (double.IsNaN((double) this._security))
        this.Security = this.Owner.Random.GetValue(1001, 40f, 60f);
      bool flag = false;
      for (int index = this.Buildings.Count - 1; index >= 0; --index)
      {
        Building building = this.Buildings[index];
        if (building.BuildingType == null || !building.BuildingType.IsReady)
        {
          this.Buildings.RemoveAt(index);
          flag = true;
        }
      }
      if (!flag)
      {
        foreach (Building building in this.BuildingsInProgress)
        {
          if (building.BuildingType == null || !building.BuildingType.IsReady)
          {
            flag = true;
            break;
          }
        }
      }
      if (!flag)
        return;
      this.BuildingsInProgress.Clear();
    }

    public IReadOnlyCollection<Town.SellLog> SoldItems => (IReadOnlyCollection<Town.SellLog>) this._soldItems;

    public IFaction MapFaction => this.OwnerClan?.MapFaction ?? (IFaction) null;

    public bool IsUnderSiege => this.Settlement.IsUnderSiege;

    [CachedData]
    public MBReadOnlyList<Village> Villages => this.Settlement.BoundVillages;

    //[SaveableProperty(1030)]
    public Clan LastCapturedBy { get; set; }

    private void ChangeClanInternal(Clan value)
    {
      if (this._ownerClan != null)
        this.RemoveOwnerClan();
      this._ownerClan = value;
      if (this._ownerClan != null)
        this.SetNewOwnerClan();
      this.ConsiderSiegesAndMapEventsInternal((IFaction) this._ownerClan);
    }

    public float GetEffectOfBuildings(BuildingEffectEnum buildingEffect)
    {
      float num = 0.0f;
      foreach (Building building in this.Buildings)
        num += building.GetBuildingEffectAmount(buildingEffect);
      return num;
    }

    public void ConsiderSiegesAndMapEventsInternal(IFaction factionToConsiderAgainst)
    {
      this.GarrisonParty?.ConsiderMapEventsAndSiegesInternal(factionToConsiderAgainst);
      foreach (Village boundVillage in this.Settlement.BoundVillages)
        boundVillage.VillagerPartyComponent?.MobileParty.ConsiderMapEventsAndSiegesInternal(factionToConsiderAgainst);
    }

    private void SetNewOwnerClan()
    {
      this._ownerClan.AddFiefInternal(this);
      foreach (Village boundVillage in this.Settlement.BoundVillages)
      {
        boundVillage.Settlement.Party.Visuals?.SetMapIconAsDirty();
        if (boundVillage.VillagerPartyComponent != null)
          boundVillage.VillagerPartyComponent.MobileParty.Party.Visuals?.SetMapIconAsDirty();
      }
    }

    private void RemoveOwnerClan() => this._ownerClan.RemoveFiefInternal(this);

    public bool HasTournament => this.IsTown && BOFCampaign.Current.TournamentManager.GetTournamentGame(this) != null;

    public void DailyTick()
    {
      this.Loyalty += this.LoyaltyChange;
      this.Security += this.SecurityChange;
      this.FoodStocks += this.FoodChange;
      if ((double) this.FoodStocks < 0.0)
      {
        this.FoodStocks = 0.0f;
        this.Owner.RemainingFoodPercentage = -100;
      }
      else
        this.Owner.RemainingFoodPercentage = 0;
      if ((double) this.FoodStocks > (double) this.FoodStocksUpperLimit())
        this.FoodStocks = (float) this.FoodStocksUpperLimit();
      if (!this.CurrentBuilding.BuildingType.IsDefaultProject)
        this.TickCurrentBuilding();
      else if (this.Governor != null && this.Governor.GetPerkValue(DefaultPerks.Charm.Virile) && (double) MBRandom.RandomFloat < 0.100000001490116)
      {
        Hero randomElement = this.Settlement.Notables.GetRandomElement<Hero>();
        if (randomElement != null)
          ChangeRelationAction.ApplyRelationChangeBetweenHeroes(this.Governor.Clan.Leader, randomElement, MathF.Round(DefaultPerks.Charm.Virile.SecondaryBonus), false);
      }
      if (this.Governor != null)
      {
        if (this.Governor.GetPerkValue(DefaultPerks.Roguery.WhiteLies) && (double) MBRandom.RandomFloat < 0.0199999995529652)
        {
          Hero randomElement = this.Settlement.Notables.GetRandomElement<Hero>();
          if (randomElement != null)
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(this.Governor, randomElement, MathF.Round(DefaultPerks.Roguery.WhiteLies.SecondaryBonus));
        }
        if (this.Governor.GetPerkValue(DefaultPerks.Roguery.Scarface) && (double) MBRandom.RandomFloat < 0.0500000007450581)
        {
          Hero elementWithPredicate = this.Settlement.Notables.GetRandomElementWithPredicate<Hero>((Func<Hero, bool>) (x => x.IsGangLeader));
          if (elementWithPredicate != null)
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(this.Governor, elementWithPredicate, MathF.Round(DefaultPerks.Roguery.Scarface.SecondaryBonus));
        }
      }
      this.Owner.Settlement.Prosperity += this.ProsperityChange;
      if ((double) this.Owner.Settlement.Prosperity < 0.0)
        this.Owner.Settlement.Prosperity = 0.0f;
      this.HandleMilitiaAndGarrisonOfSettlementDaily();
      this.RepairWallsOfSettlementDaily();
    }

    private void HandleMilitiaAndGarrisonOfSettlementDaily()
    {
      this.Owner.Settlement.Militia += this.MilitiaChange;
      if (this.GarrisonChange >= 1 && this.GarrisonParty == null)
        this.Owner.Settlement.AddGarrisonParty();
      if (this.GarrisonParty == null || !this.GarrisonParty.IsActive || this.GarrisonParty.MapEvent != null || this.GarrisonParty.CurrentSettlement == null)
        return;
      int dailyTroopXpBonus = Campaign.Current.Models.DailyTroopXpBonusModel.CalculateDailyTroopXpBonus(this);
      float xpBonusMultiplier = Campaign.Current.Models.DailyTroopXpBonusModel.CalculateGarrisonXpBonusMultiplier(this);
      if (dailyTroopXpBonus > 0)
      {
        foreach (TroopRosterElement troopRosterElement in this.GarrisonParty.MemberRoster.GetTroopRoster())
          this.GarrisonParty.MemberRoster.AddXpToTroop(MathF.Round((float) dailyTroopXpBonus * xpBonusMultiplier * (float) troopRosterElement.Number), troopRosterElement.Character);
      }
      this.DailyGarrisonAdjustment();
      Campaign.Current.PartyUpgrader.UpgradeReadyTroops(this.GarrisonParty.Party);
    }

    private void RepairWallsOfSettlementDaily()
    {
      Settlement settlement = this.Owner.Settlement;
      float maxWallHitPoints = settlement.MaxWallHitPoints;
      if (!settlement.SettlementWallSectionHitPointsRatioList.Any<float>((Func<float, bool>) (health => (double) health < 1.0)) || settlement.IsUnderSiege)
        return;
      float num1 = maxWallHitPoints * 0.02f;
      float effectOfBuildings = this.GetEffectOfBuildings(BuildingEffectEnum.WallRepairSpeed);
      if ((double) effectOfBuildings > 0.0)
        num1 += (float) ((double) num1 * (double) effectOfBuildings * 0.00999999977648258);
      float b = num1 / settlement.MaxHitPointsOfOneWallSection;
      for (int index = 0; index < settlement.SettlementWallSectionHitPointsRatioList.Count; ++index)
      {
        float sectionHitPointsRatio = settlement.SettlementWallSectionHitPointsRatioList[index];
        float num2 = MathF.Min(1f - sectionHitPointsRatio, b);
        settlement.SetWallSectionHitPointsRatioAtIndex(index, sectionHitPointsRatio + num2);
        b -= num2;
        if ((double) b <= 0.0)
          break;
      }
    }

    private void DesertOneTroopFromGarrison()
    {
      if (this.GarrisonParty.MemberRoster.TotalManCount <= 0)
        return;
      int num = (int) ((double) MBRandom.RandomFloat * (double) this.GarrisonParty.MemberRoster.TotalManCount);
      for (int index = 0; index < this.GarrisonParty.MemberRoster.Count; ++index)
      {
        num -= this.GarrisonParty.MemberRoster.GetElementNumber(index);
        if (num < 0)
        {
          TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
          MobilePartyHelper.DesertTroopsFromParty(this.GarrisonParty, index, 1, 0, ref dummyTroopRoster);
          break;
        }
      }
    }

    private void DailyGarrisonAdjustment()
    {
      int garrisonChange = this.GarrisonParty.CurrentSettlement.Town.GarrisonChange;
      int num1 = this.GarrisonAutoRecruitmentIsEnabled ? this.GarrisonParty.CurrentSettlement.Town.GarrisonChangeAutoRecruitment : 0;
      int num2 = garrisonChange - num1;
      int partySizeLimit = this.GarrisonParty.Party.PartySizeLimit;
      if (num2 > 0 && num2 > partySizeLimit - this.GarrisonParty.Party.NumberOfAllMembers)
        num2 = partySizeLimit - this.GarrisonParty.Party.NumberOfAllMembers;
      if (num2 < 0)
      {
        for (int index = 0; index < MathF.Abs(num2); ++index)
          this.DesertOneTroopFromGarrison();
      }
      else if (num2 > 0)
        this.GarrisonParty.MemberRoster.AddToCounts(this.GarrisonParty.MapFaction.BasicTroop, num2);
      if (num1 > 0)
      {
        int num3 = SettlementHelper.NumberOfVolunteersCanBeRecruitedForGarrison(this.GarrisonParty.CurrentSettlement);
        Hero leader = this.GarrisonParty.CurrentSettlement.OwnerClan.Leader;
        if (num3 > 0)
        {
          float num4 = MBRandom.RandomFloat * (float) num3;
          foreach (Hero notable in this.GarrisonParty.CurrentSettlement.Notables)
          {
            if ((double) num4 > 0.0)
            {
              int num5 = HeroHelper.MaximumIndexHeroCanRecruitFromHero(leader, notable);
              for (int index = 0; index < num5; ++index)
              {
                if (notable.VolunteerTypes[index] != null)
                {
                  --num4;
                  if ((double) num4 <= 0.0)
                  {
                    this.GarrisonParty.MemberRoster.AddToCounts(notable.VolunteerTypes[index], 1);
                    leader.Clan.AutoRecruitmentExpenses += Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(notable.VolunteerTypes[index], leader);
                    notable.VolunteerTypes[index] = (CharacterObject) null;
                    break;
                  }
                }
              }
            }
            else
              break;
          }
          if ((double) num4 > 0.0)
          {
            foreach (Village boundVillage in this.GarrisonParty.CurrentSettlement.BoundVillages)
            {
              if ((double) num4 > 0.0)
              {
                if (boundVillage.VillageState == Village.VillageStates.Normal)
                {
                  foreach (Hero notable in boundVillage.Settlement.Notables)
                  {
                    if ((double) num4 > 0.0)
                    {
                      int num6 = HeroHelper.MaximumIndexHeroCanRecruitFromHero(leader, notable);
                      for (int index = 0; index < num6; ++index)
                      {
                        if (notable.VolunteerTypes[index] != null)
                        {
                          --num4;
                          if ((double) num4 <= 0.0)
                          {
                            this.GarrisonParty.MemberRoster.AddToCounts(notable.VolunteerTypes[index], 1);
                            leader.Clan.AutoRecruitmentExpenses += Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(notable.VolunteerTypes[index], leader);
                            notable.VolunteerTypes[index] = (CharacterObject) null;
                            break;
                          }
                        }
                      }
                    }
                    else
                      break;
                  }
                }
              }
              else
                break;
            }
          }
        }
      }
      if (this.GarrisonParty.Party.NumberOfAllMembers <= partySizeLimit)
        return;
      int num7 = MBRandom.RoundRandomized((float) (this.GarrisonParty.Party.NumberOfAllMembers - partySizeLimit) * 0.2f);
      for (int index = 0; index < num7; ++index)
        this.DesertOneTroopFromGarrison();
    }

    public int GetWallLevel()
    {
      int num = 0;
      foreach (Building building in this.Buildings)
      {
        if (building.BuildingType == DefaultBuildingTypes.Fortifications && this.IsTown)
        {
          num = building.CurrentLevel;
          break;
        }
        if (building.BuildingType == DefaultBuildingTypes.Wall && this.IsCastle)
        {
          num = building.CurrentLevel;
          break;
        }
      }
      return num;
    }

    public override string ToString() => this.Name.ToString();

    public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    {
      if (!this.IsInitialized)
        this._wallLevel = node.Attributes["level"] != null ? int.Parse(node.Attributes["level"].Value) : 0;
      this._isCastle = node.Attributes["is_castle"] != null && bool.Parse(node.Attributes["is_castle"].Value);
      this.BackgroundCropPosition = float.Parse(node.Attributes["background_crop_position"].Value);
      this.BackgroundMeshName = node.Attributes["background_mesh"].Value;
      this.WaitMeshName = node.Attributes["wait_mesh"].Value;
      base.Deserialize(objectManager, node);
    }

    public void SetSoldItems(IEnumerable<Town.SellLog> logList) => this._soldItems = logList.ToArray<Town.SellLog>();

    public override int GetItemPrice(ItemObject item, MobileParty tradingParty = null, bool isSelling = false) => this.MarketData.GetPrice(item, tradingParty, isSelling);

    public override int GetItemPrice(
      EquipmentElement itemRosterElement,
      MobileParty tradingParty = null,
      bool isSelling = false)
    {
      return this.MarketData.GetPrice(itemRosterElement, tradingParty, isSelling);
    }

    private static float GetItemProductionPerDayAtVillagesOfTown(Settlement town, ItemObject item)
    {
      float num = 0.0f;
      foreach (Settlement settlement in Campaign.Current.Settlements)
      {
        if (settlement.IsVillage && settlement.GetComponent<SettlementComponent>().TradeBound == town)
        {
          float productionAmount = Campaign.Current.Models.VillageProductionCalculatorModel.CalculateDailyProductionAmount(settlement.Village, item);
          num += productionAmount;
        }
      }
      return num;
    }

    private float GetItemProductionPerDayAtArea(ItemObject item, float radius)
    {
      float num1 = 0.0f;
      foreach (Settlement settlement in Campaign.Current.Settlements)
      {
        if (settlement.IsTown)
        {
          float num2 = settlement.Position2D.Distance(this.Owner.Settlement.Position2D);
          if ((double) num2 < (double) radius)
          {
            double num3 = (double) num2 / (double) radius;
            num1 += Town.GetItemProductionPerDayAtVillagesOfTown(settlement, item);
          }
        }
      }
      return num1;
    }

    public override SettlementComponent.ProsperityLevel GetProsperityLevel()
    {
      if ((double) this.Owner.Settlement.Prosperity >= 5000.0)
        return SettlementComponent.ProsperityLevel.High;
      return (double) this.Owner.Settlement.Prosperity >= 2000.0 ? SettlementComponent.ProsperityLevel.Mid : SettlementComponent.ProsperityLevel.Low;
    }

    private void TickCurrentBuilding()
    {
      if (this.BuildingsInProgress.Peek().CurrentLevel == 3)
        this.BuildingsInProgress.Dequeue();
      if (this.Owner.Settlement.IsUnderSiege || this.BuildingsInProgress.IsEmpty<Building>())
        return;
      BuildingConstructionModel constructionModel = Campaign.Current.Models.BuildingConstructionModel;
      Building building = this.BuildingsInProgress.Peek();
      building.BuildingProgress += this.Construction;
      int num = this.IsCastle ? constructionModel.CastleBoostCost : constructionModel.TownBoostCost;
      if (this.BoostBuildingProcess > 0)
      {
        this.BoostBuildingProcess -= num;
        if (this.BoostBuildingProcess < 0)
          this.BoostBuildingProcess = 0;
      }
      if ((double) building.GetConstructionCost() > (double) building.BuildingProgress)
        return;
      if (building.CurrentLevel < 3)
        building.LevelUp();
      if (building.CurrentLevel == 3)
        building.BuildingProgress = (float) building.GetConstructionCost();
      this.BuildingsInProgress.Dequeue();
    }

    protected override void OnInventoryUpdated(ItemRosterElement item, int count) => this.MarketData.OnTownInventoryUpdated(item, count);

    public float GetItemCategoryPriceIndex(ItemCategory itemCategory, bool isSellingToTown = false) => this.MarketData.GetPriceFactor(itemCategory, isSellingToTown);

    public struct SellLog
    {
      public static void AutoGeneratedStaticCollectObjectsSellLog(
        object o,
        List<object> collectedObjects)
      {
        ((Town.SellLog) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
      }

      private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects) => collectedObjects.Add((object) this.Category);

      public static object AutoGeneratedGetMemberValueCategory(object o) => (object) ((Town.SellLog) o).Category;

      public static object AutoGeneratedGetMemberValueNumber(object o) => (object) ((Town.SellLog) o).Number;

      //[SaveableProperty(200)]
      public ItemCategory Category { get; private set; }

      //[SaveableProperty(201)]
      public int Number { get; private set; }

      public SellLog(ItemCategory category, int count)
      {
        this.Category = category;
        this.Number = count;
      }
    }
  }
}