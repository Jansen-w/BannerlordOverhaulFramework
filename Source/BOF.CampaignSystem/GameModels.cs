using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BOF.CampaignSystem
{
    public sealed class GameModels : GameModelsManager
    {
        public MapVisibilityModel MapVisibilityModel { get; private set; }

        public MapVisibilityListener MapVisibilityListener { get; private set; }

        public PartySpeedModel PartySpeedCalculatingModel { get; private set; }

        public PartyHealingModel PartyHealingModel { get; private set; }

        public PartyTrainingModel PartyTrainingModel { get; private set; }

        public BarterModel BarterModel { get; private set; }

        public PersuasionModel PersuasionModel { get; private set; }

        public CombatSimulationModel CombatSimulationModel { get; private set; }

        public CombatXpModel CombatXpModel { get; private set; }

        public GenericXpModel GenericXpModel { get; private set; }

        public SmithingModel SmithingModel { get; private set; }

        public PartyTradeModel PartyTradeModel { get; private set; }

        public RansomValueCalculationModel RansomValueCalculationModel { get; private set; }

        public RaidModel RaidModel { get; private set; }

        public MobilePartyFoodConsumptionModel MobilePartyFoodConsumptionModel { get; private set; }

        public PartyFoodBuyingModel PartyFoodBuyingModel { get; private set; }

        public PartyImpairmentModel PartyImpairmentModel { get; private set; }

        public PartyMoraleModel PartyMoraleModel { get; private set; }

        public PartyDesertionModel PartyDesertionModel { get; private set; }

        public DiplomacyModel DiplomacyModel { get; private set; }

        public HeroSpawnModel HeroSpawnModel { get; private set; }

        public KingdomCreationModel KingdomCreationModel { get; private set; }

        public KingdomDecisionPermissionModel KingdomDecisionPermissionModel { get; private set; }

        public CharacterDevelopmentModel CharacterDevelopmentModel { get; private set; }

        public TaleWorlds.CampaignSystem.CharacterStatsModel CharacterStatsModel { get; private set; }

        public ItemDiscardModel ItemDiscardModel { get; private set; }

        public ValuationModel ValuationModel { get; private set; }

        public PartySizeLimitModel PartySizeLimitModel { get; private set; }

        public InventoryCapacityModel InventoryCapacityModel { get; private set; }

        public PartyWageModel PartyWageModel { get; private set; }

        public PlayerCaptivityModel PlayerCaptivityModel { get; private set; }

        public VillageProductionCalculatorModel VillageProductionCalculatorModel { get; private set; }

        public VolunteerProductionModel VolunteerProductionModel { get; private set; }

        public RomanceModel RomanceModel { get; private set; }

        public ArmyManagementCalculationModel ArmyManagementCalculationModel { get; private set; }

        public BanditDensityModel BanditDensityModel { get; private set; }

        public LordPartyHourlyPersonalThinkModel LordPartyHourlyPersonalThinkModel { get; private set; }

        public LordPartyHourlyMilitaryThinkModel LordPartyHourlyMilitaryThinkModel { get; private set; }

        public EncounterGameMenuModel EncounterGameMenuModel { get; private set; }

        public BattleRewardModel BattleRewardModel { get; private set; }

        public MapTrackModel MapTrackModel { get; private set; }

        public MapDistanceModel MapDistanceModel { get; private set; }

        public MapWeatherModel MapWeatherModel { get; private set; }

        public TargetScoreCalculatingModel TargetScoreCalculatingModel { get; private set; }

        public TradeItemPriceFactorModel TradeItemPriceFactorModel { get; private set; }

        public SettlementEconomyModel SettlementConsumptionModel { get; private set; }

        public SettlementFoodModel SettlementFoodModel { get; private set; }

        public SettlementValueModel SettlementValueModel { get; private set; }

        public SettlementMilitiaModel SettlementMilitiaModel { get; private set; }

        public SettlementLoyaltyModel SettlementLoyaltyModel { get; private set; }

        public SettlementSecurityModel SettlementSecurityModel { get; private set; }

        public SettlementProsperityModel SettlementProsperityModel { get; private set; }

        public SettlementGarrisonModel SettlementGarrisonModel { get; private set; }

        public ClanTierModel ClanTierModel { get; private set; }

        public VassalRewardsModel VassalRewardsModel { get; private set; }

        public ClanPoliticsModel ClanPoliticsModel { get; private set; }

        public ClanFinanceModel ClanFinanceModel { get; private set; }

        public SettlementTaxModel SettlementTaxModel { get; private set; }

        public HeirSelectionCalculationModel HeirSelectionCalculationModel { get; private set; }

        public HeroDeathProbabilityCalculationModel HeroDeathProbabilityCalculationModel { get; private set; }

        public BuildingConstructionModel BuildingConstructionModel { get; private set; }

        public BuildingEffectModel BuildingEffectModel { get; private set; }

        public WallHitPointCalculationModel WallHitPointCalculationModel { get; private set; }

        public MarriageModel MarriageModel { get; private set; }

        public AgeModel AgeModel { get; private set; }

        public DailyTroopXpBonusModel DailyTroopXpBonusModel { get; private set; }

        public PregnancyModel PregnancyModel { get; private set; }

        public NotablePowerModel NotablePowerModel { get; private set; }

        public MilitaryPowerModel MilitaryPowerModel { get; private set; }

        public PrisonerDonationModel PrisonerDonationModel { get; private set; }

        public NotableSpawnModel NotableSpawnModel { get; private set; }

        public TournamentModel TournamentModel { get; private set; }

        public CrimeModel CrimeModel { get; private set; }

        public DisguiseDetectionModel DisguiseDetectionModel { get; private set; }

        public BribeCalculationModel BribeCalculationModel { get; private set; }

        public TroopSacrificeModel TroopSacrificeModel { get; private set; }

        public SiegeStrategyActionModel SiegeStrategyActionModel { get; private set; }

        public SiegeEventModel SiegeEventModel { get; private set; }

        public SiegeAftermathModel SiegeAftermathModel { get; private set; }

        public SiegeLordsHallFightModel SiegeLordsHallFightModel { get; private set; }

        public CompanionHiringPriceCalculationModel CompanionHiringPriceCalculationModel { get; private set; }

        public BuildingScoreCalculationModel BuildingScoreCalculationModel { get; private set; }

        public SettlementAccessModel SettlementAccessModel { get; private set; }

        public IssueModel IssueModel { get; private set; }

        public PrisonerRecruitmentCalculationModel PrisonerRecruitmentCalculationModel { get; private set; }

        public PartyTroopUpgradeModel PartyTroopUpgradeModel { get; private set; }

        public TavernMercenaryTroopsModel TavernMercenaryTroopsModel { get; private set; }

        public WorkshopModel WorkshopModel { get; private set; }

        public DifficultyModel DifficultyModel { get; private set; }

        public LocationModel LocationModel { get; private set; }

        public PrisonBreakModel PrisonBreakModel { get; private set; }

        public BattleCaptainModel BattleCaptainModel { get; private set; }

        public ExecutionRelationModel ExecutionRelationModel { get; private set; }

        public BannerItemRewardModel BannerItemRewardModel { get; private set; }

        private void GetSpecificGameBehaviors()
        {
            if (TaleWorlds.CampaignSystem.Campaign.Current.GameMode != CampaignGameMode.Campaign &&
                TaleWorlds.CampaignSystem.Campaign.Current.GameMode != CampaignGameMode.Tutorial)
                return;
            this.CharacterDevelopmentModel = this.GetGameModel<CharacterDevelopmentModel>();
            this.CharacterStatsModel = this.GetGameModel<TaleWorlds.CampaignSystem.CharacterStatsModel>();
            this.ItemDiscardModel = this.GetGameModel<ItemDiscardModel>();
            this.ValuationModel = this.GetGameModel<ValuationModel>();
            this.MapVisibilityModel = this.GetGameModel<MapVisibilityModel>();
            this.PlayerCaptivityModel = this.GetGameModel<PlayerCaptivityModel>();
            this.MapVisibilityListener = this.GetGameModel<MapVisibilityListener>();
            this.PartySpeedCalculatingModel = this.GetGameModel<PartySpeedModel>();
            this.PartyHealingModel = this.GetGameModel<PartyHealingModel>();
            this.PartyTrainingModel = this.GetGameModel<PartyTrainingModel>();
            this.PartyTradeModel = this.GetGameModel<PartyTradeModel>();
            this.RansomValueCalculationModel = this.GetGameModel<RansomValueCalculationModel>();
            this.RaidModel = this.GetGameModel<RaidModel>();
            this.CombatSimulationModel = this.GetGameModel<CombatSimulationModel>();
            this.CombatXpModel = this.GetGameModel<CombatXpModel>();
            this.GenericXpModel = this.GetGameModel<GenericXpModel>();
            this.SmithingModel = this.GetGameModel<SmithingModel>();
            this.MobilePartyFoodConsumptionModel = this.GetGameModel<MobilePartyFoodConsumptionModel>();
            this.PartyImpairmentModel = this.GetGameModel<PartyImpairmentModel>();
            this.PartyFoodBuyingModel = this.GetGameModel<PartyFoodBuyingModel>();
            this.PartyMoraleModel = this.GetGameModel<PartyMoraleModel>();
            this.PartyDesertionModel = this.GetGameModel<PartyDesertionModel>();
            this.DiplomacyModel = this.GetGameModel<DiplomacyModel>();
            this.HeroSpawnModel = this.GetGameModel<HeroSpawnModel>();
            this.KingdomCreationModel = this.GetGameModel<KingdomCreationModel>();
            this.KingdomDecisionPermissionModel = this.GetGameModel<KingdomDecisionPermissionModel>();
            this.VillageProductionCalculatorModel = this.GetGameModel<VillageProductionCalculatorModel>();
            this.RomanceModel = this.GetGameModel<RomanceModel>();
            this.VolunteerProductionModel = this.GetGameModel<VolunteerProductionModel>();
            this.ArmyManagementCalculationModel = this.GetGameModel<ArmyManagementCalculationModel>();
            this.BanditDensityModel = this.GetGameModel<BanditDensityModel>();
            this.LordPartyHourlyPersonalThinkModel = this.GetGameModel<LordPartyHourlyPersonalThinkModel>();
            this.LordPartyHourlyMilitaryThinkModel = this.GetGameModel<LordPartyHourlyMilitaryThinkModel>();
            this.EncounterGameMenuModel = this.GetGameModel<EncounterGameMenuModel>();
            this.BattleRewardModel = this.GetGameModel<BattleRewardModel>();
            this.MapTrackModel = this.GetGameModel<MapTrackModel>();
            this.MapDistanceModel = this.GetGameModel<MapDistanceModel>();
            this.MapWeatherModel = this.GetGameModel<MapWeatherModel>();
            this.TargetScoreCalculatingModel = this.GetGameModel<TargetScoreCalculatingModel>();
            this.PartySizeLimitModel = this.GetGameModel<PartySizeLimitModel>();
            this.PartyWageModel = this.GetGameModel<PartyWageModel>();
            this.InventoryCapacityModel = this.GetGameModel<InventoryCapacityModel>();
            this.TradeItemPriceFactorModel = this.GetGameModel<TradeItemPriceFactorModel>();
            this.SettlementValueModel = this.GetGameModel<SettlementValueModel>();
            this.SettlementConsumptionModel = this.GetGameModel<SettlementEconomyModel>();
            this.SettlementMilitiaModel = this.GetGameModel<SettlementMilitiaModel>();
            this.SettlementFoodModel = this.GetGameModel<SettlementFoodModel>();
            this.SettlementLoyaltyModel = this.GetGameModel<SettlementLoyaltyModel>();
            this.SettlementSecurityModel = this.GetGameModel<SettlementSecurityModel>();
            this.SettlementProsperityModel = this.GetGameModel<SettlementProsperityModel>();
            this.SettlementGarrisonModel = this.GetGameModel<SettlementGarrisonModel>();
            this.SettlementTaxModel = this.GetGameModel<SettlementTaxModel>();
            this.BarterModel = this.GetGameModel<BarterModel>();
            this.PersuasionModel = this.GetGameModel<PersuasionModel>();
            this.ClanTierModel = this.GetGameModel<ClanTierModel>();
            this.VassalRewardsModel = this.GetGameModel<VassalRewardsModel>();
            this.ClanPoliticsModel = this.GetGameModel<ClanPoliticsModel>();
            this.ClanFinanceModel = this.GetGameModel<ClanFinanceModel>();
            this.HeirSelectionCalculationModel = this.GetGameModel<HeirSelectionCalculationModel>();
            this.HeroDeathProbabilityCalculationModel = this.GetGameModel<HeroDeathProbabilityCalculationModel>();
            this.BuildingConstructionModel = this.GetGameModel<BuildingConstructionModel>();
            this.BuildingEffectModel = this.GetGameModel<BuildingEffectModel>();
            this.WallHitPointCalculationModel = this.GetGameModel<WallHitPointCalculationModel>();
            this.MarriageModel = this.GetGameModel<MarriageModel>();
            this.AgeModel = this.GetGameModel<AgeModel>();
            this.DailyTroopXpBonusModel = this.GetGameModel<DailyTroopXpBonusModel>();
            this.PregnancyModel = this.GetGameModel<PregnancyModel>();
            this.NotablePowerModel = this.GetGameModel<NotablePowerModel>();
            this.NotableSpawnModel = this.GetGameModel<NotableSpawnModel>();
            this.TournamentModel = this.GetGameModel<TournamentModel>();
            this.SiegeStrategyActionModel = this.GetGameModel<SiegeStrategyActionModel>();
            this.SiegeEventModel = this.GetGameModel<SiegeEventModel>();
            this.SiegeAftermathModel = this.GetGameModel<SiegeAftermathModel>();
            this.SiegeLordsHallFightModel = this.GetGameModel<SiegeLordsHallFightModel>();
            this.CrimeModel = this.GetGameModel<CrimeModel>();
            this.DisguiseDetectionModel = this.GetGameModel<DisguiseDetectionModel>();
            this.BribeCalculationModel = this.GetGameModel<BribeCalculationModel>();
            this.CompanionHiringPriceCalculationModel = this.GetGameModel<CompanionHiringPriceCalculationModel>();
            this.TroopSacrificeModel = this.GetGameModel<TroopSacrificeModel>();
            this.BuildingScoreCalculationModel = this.GetGameModel<BuildingScoreCalculationModel>();
            this.SettlementAccessModel = this.GetGameModel<SettlementAccessModel>();
            this.IssueModel = this.GetGameModel<IssueModel>();
            this.PrisonerRecruitmentCalculationModel = this.GetGameModel<PrisonerRecruitmentCalculationModel>();
            this.PartyTroopUpgradeModel = this.GetGameModel<PartyTroopUpgradeModel>();
            this.TavernMercenaryTroopsModel = this.GetGameModel<TavernMercenaryTroopsModel>();
            this.WorkshopModel = this.GetGameModel<WorkshopModel>();
            this.DifficultyModel = this.GetGameModel<DifficultyModel>();
            this.LocationModel = this.GetGameModel<LocationModel>();
            this.MilitaryPowerModel = this.GetGameModel<MilitaryPowerModel>();
            this.PrisonerDonationModel = this.GetGameModel<PrisonerDonationModel>();
            this.PrisonBreakModel = this.GetGameModel<PrisonBreakModel>();
            this.BattleCaptainModel = this.GetGameModel<BattleCaptainModel>();
            this.ExecutionRelationModel = this.GetGameModel<ExecutionRelationModel>();
            this.BannerItemRewardModel = this.GetGameModel<BannerItemRewardModel>();
        }

        private void MakeGameComponentBindings()
        {
        }

        public GameModels(IEnumerable<GameModel> inputComponents)
            : base(inputComponents)
        {
            this.GetSpecificGameBehaviors();
            this.MakeGameComponentBindings();
        }
    }
}