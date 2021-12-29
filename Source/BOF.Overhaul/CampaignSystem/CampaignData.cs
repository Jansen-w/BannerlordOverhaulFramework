using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace BOF.Overhaul.CampaignSystem
{
    public static class CampaignData
    {
        public const string MainHeroTag = "main_hero";
        public const string PlayerTag = "spawnpoint_player";
        public const string PlayerConversationTag = "sp_player_conversation";
        public const string PlayerOutsideTag = "spawnpoint_player_outside";
        public const string PlayerNearTownMainGate = "sp_outside_near_town_main_gate";
        public const string PlayerPrisonBreakTag = "sp_prison_break";
        public const string PlayerAmbushTag = "ambush_spot";
        public const string TournamentMasterTag = "spawnpoint_tournamentmaster";
        public const string PlayerNearArenaMasterTag = "sp_player_near_arena_master";
        public const string ArcherySetTag = "tournament_archery";
        public const string FightSetTag = "tournament_fight";
        public const string JoustingSetTag = "tournament_jousting";
        public const string TavernWenchTag = "sp_tavern_wench";
        public const string TavernKeeperTag = "spawnpoint_tavernkeeper";
        public const string MercenaryTag = "spawnpoint_mercenary";
        public const string MusicianTag = "musician";
        public const string DesertWarHorseSpawnpointTag = "desert_war_horse";
        public const string SteppeChargerSpawnpointTag = "steppe_charger";
        public const string WarHorseSpawnpointTag = "war_horse";
        public const string HorseChargerSpawnpointTag = "charger";
        public const string DesertHorseSpawnpointTag = "desert_horse";
        public const string HunterHorseSpawnpointTag = "hunter";
        public const string SheepSpawnpointTag = "sp_sheep";
        public const string CowSpawnpointTag = "sp_cow";
        public const string HogSpawnpointTag = "sp_hog";
        public const string GooseSpawnpointTag = "sp_goose";
        public const string ChickenSpawnpointTag = "sp_chicken";
        public const string CatSpawnpointTag = "sp_cat";
        public const string DogSpawnpointTag = "sp_dog";
        public const string HorseSpawnpointTag = "sp_horse";
        public const string SpawnpointNPCTag = "sp_npc";
        public const string GuardTag = "sp_guard";
        public const string GuardWithSpearTag = "sp_guard_with_spear";
        public const string GuardPatrolTag = "sp_guard_patrol";
        public const string PrisonGuard = "sp_prison_guard";
        public const string GuardLordsHallGateTag = "sp_guard_castle";
        public const string UnarmedGuardTag = "sp_guard_unarmed";
        public const string TraderTag = "sp_merchant";
        public const string HorseTraderTag = "sp_horse_merchant";
        public const string ArmorerTag = "sp_armorer";
        public const string BarberTag = "sp_barber";
        public const string WeaponSmithTag = "sp_weaponsmith";
        public const string BlacksmithTag = "sp_blacksmith";
        public const string DancerTag = "npc_dancer";
        public const string BeggarTag = "npc_beggar";
        public const string GovernorTag = "sp_governor";
        public const string CleanerTag = "spawnpoint_cleaner";
        public const string WorkshopSellerTag = "sp_shop_seller";
        public const string NpcCommonTag = "npc_common";
        public const string NpcCommonLimitedTag = "npc_common_limited";
        public const string ElderTag = "spawnpoint_elder";
        public const string IdleTag = "npc_idle";
        public const string GamblerTag = "gambler_npc";
        public const string ReservedTag = "reserved";
        public const string HiddenSpawnPointTag = "sp_common_hidden";
        public const string PassageTag = "npc_passage";
        public const string DisableAtNightTag = "disable_at_night";
        public const string EnableAtNightTag = "enable_at_night";
        public const string KingTag = "sp_king";
        public const string ThroneTag = "sp_throne";
        public const string SpawnPointCleanerTag = "spawnpoint_cleaner";
        public const string NpcAmbushTag = "spawnpoint_thug";
        public const string NpcSneakTag = "spawnpoint_npc_sneak";
        public const string NavigationMeshDeactivatorTag = "navigation_mesh_deactivator";
        public const string BattleSetTag = "battle_set";
        public const string CommonAreaMarkerTag = "common_area_marker";
        public const string WorkshopAreaMarkerTag = "workshop_area_marker";
        public const string WorkshopSignTag = "shop_sign";
        public const string PrisonerTag = "sp_prisoner";
        public const string PrisonerGuardTag = "sp_prison_guard";
        public const string NotableTag = "sp_notable";
        public const string NotableGangLeaderTag = "sp_notable_gangleader";
        public const string NotableRuralNotableTag = "sp_notable_rural_notable";
        public const string NotablePreacherTag = "sp_notable_preacher";
        public const string NotableArtisanTag = "sp_notable_artisan";
        public const string NotableMerchantTag = "sp_notable_merchant";
        public const string GangLeaderBodyGuard = "sp_gangleader_bodyguard";
        public const string MerchantNotary = "sp_merchant_notary";
        public const string ArtisanNotary = "sp_artisan_notary";
        public const string PreacherNotary = "sp_preacher_notary";
        public const string RuralNotableNotary = "sp_rural_notable_notary";
        public const string PrisonBreakPrisonerTag = "sp_prison_break_prisoner";
        public const string Level1Tag = "level_1";
        public const string Level2Tag = "level_2";
        public const string Level3Tag = "level_3";
        public const string CivilianTag = "civilian";
        public const string PrisonBreakLevelTag = "prison_break";
        public const string SiegeTag = "siege";
        public const string RaidTag = "raid";
        public const string BurnedTag = "burned";
        public const string SallyOutTag = "sally_out";
        public const string PlayerStealthTag = "sp_player_stealth";
        public const string Shop1Tag = "shop_1";
        public const string Shop2Tag = "shop_2";
        public const string Shop3Tag = "shop_3";
        public const string Shop4Tag = "shop_4";
        public const string CultureEmpire = "empire";
        public const string CultureSturgia = "sturgia";
        public const string CultureAserai = "aserai";
        public const string CultureVlandia = "vlandia";
        public const string CultureBattania = "battania";
        public const string CultureKhuzait = "khuzait";
        public const string CultureNord = "nord";
        public const string CultureDarshi = "darshi";
        public const string CultureVakken = "vakken";
        public const string CultureNeutral = "neutral_culture";
        public const string CultureForestHideout = "forest_bandits";
        public const string CultureSeaHideout = "sea_raiders";
        public const string CultureMountainHideout = "mountain_bandits";
        public const string CultureDesertHideout = "desert_bandits";
        public const string CultureSteppeHideout = "steppe_bandits";
        public const string Looters = "looters";
        public const string LocationCenter = "center";
        public const string LocationArena = "arena";
        public const string LocationPrison = "prison";
        public const string LocationLordsHall = "lordshall";
        public const string LocationTavern = "tavern";
        public const string LocationVillageCenter = "village_center";
        public const string LocationHouse1 = "house_1";
        public const string LocationHouse2 = "house_2";
        public const string LocationHouse3 = "house_3";
        public const string LameHorseModifier = "lame_horse";
        public const int MinClanNameLength = 1;
        public const int MaxClanNameLength = 50;
        public const int CampaignStartYear = 1084;
        private static Clan _neutralFaction;

        public static readonly CampaignTime CampaignStartTime =
            CampaignTime.Years(1084f) + CampaignTime.Weeks(3f) + CampaignTime.Hours(9f);

        public static uint NeutralColor1 => NeutralFaction.Color;

        public static uint NeutralColor2 => NeutralFaction.Color2;

        public static Clan NeutralFaction
        {
            get
            {
                if (_neutralFaction != null)
                    return _neutralFaction;
                foreach (Clan clan in Clan.All)
                {
                    if (clan.StringId == "neutral")
                    {
                        _neutralFaction = clan;
                        return clan;
                    }
                }

                Debug.FailedAssert("false",
                    "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignData.cs",
                    nameof(NeutralFaction), 200);
                return null;
            }
            private set => _neutralFaction = value;
        }

        public static void OnGameEnd() => NeutralFaction = null;
    }
}