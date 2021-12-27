using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BOF.CampaignSystem.CampaignSystem
{
    public class MapEventParty
    {
        //[SaveableField(2)]
        private FlattenedTroopRoster _roster;

        //[SaveableField(3)]
        private int _contributionToBattle = 1;

        //[SaveableField(4)]
        private PartyBase _alternativePartyToReceiveLootObsolete;

        //[SaveableField(5)]
        private float _strengthAtStart = 1f;

        //[SaveableField(9)]
        private int _healthyManCountAtStart = 1;

        //[SaveableField(6)]
        private TroopRoster _casualtiesInBattleObsolete;

        //[SaveableField(7)]
        private TroopRoster _woundedInBattle = TroopRoster.CreateDummyTroopRoster();

        //[SaveableField(8)]
        private TroopRoster _diedInBattle = TroopRoster.CreateDummyTroopRoster();

        // //[SaveableProperty(1)]
        public PartyBase Party { get; private set; }

        public float StrengthAtStart => this._strengthAtStart;

        public int HealthyManCountAtStart => this._healthyManCountAtStart;

        public TroopRoster DiedInBattle => this._diedInBattle;

        public TroopRoster WoundedInBattle => this._woundedInBattle;

        public int ContributionToBattle => this._contributionToBattle;

        public void ResetContributionToBattleToStrength() =>
            this._contributionToBattle = (int)MathF.Sqrt(this.Party.TotalStrength);

        public FlattenedTroopRoster Troops => this._roster;

        public MapEventParty(PartyBase party)
        {
            this.Party = party;
            this.Update();
            this._strengthAtStart = party.TotalStrength;
            this._healthyManCountAtStart = party.NumberOfHealthyMembers;
        }

        public void Update()
        {
            if (this._roster == null)
                this._roster = new FlattenedTroopRoster(this.Party.MemberRoster.TotalManCount);
            this._roster.Clear();
            foreach (TroopRosterElement troopRosterElement in this.Party.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character.IsHero)
                {
                    if (!this._woundedInBattle.Contains(troopRosterElement.Character) &&
                        !this._diedInBattle.Contains(troopRosterElement.Character))
                        this._roster.Add(troopRosterElement.Character,
                            troopRosterElement.Character.HeroObject.IsWounded, troopRosterElement.Xp);
                }
                else
                    this._roster.Add(troopRosterElement.Character, troopRosterElement.Number,
                        troopRosterElement.WoundedNumber);
            }
        }

        public bool IsNpcParty => this.Party != PartyBase.MainParty;

        public TroopRoster RosterToReceiveLootMembers => !this.IsNpcParty
            ? PlayerEncounter.Current.RosterToReceiveLootMembers
            : this.Party.MemberRoster;

        public TroopRoster RosterToReceiveLootPrisoners => !this.IsNpcParty
            ? PlayerEncounter.Current.RosterToReceiveLootPrisoners
            : this.Party.PrisonRoster;

        public ItemRoster RosterToReceiveLootItems =>
            !this.IsNpcParty ? PlayerEncounter.Current.RosterToReceiveLootItems : this.Party.ItemRoster;

        // [LoadInitializationCallback]
        // private void OnLoad(MetaData metaData)
        // {
        // TroopRoster troopRoster1 = this._diedInBattle;
        // if ((object) troopRoster1 == null)
        // troopRoster1 = TroopRoster.CreateDummyTroopRoster();
        // this._diedInBattle = troopRoster1;
        // TroopRoster troopRoster2 = this._woundedInBattle;
        // if ((object) troopRoster2 == null)
        // troopRoster2 = TroopRoster.CreateDummyTroopRoster();
        // this._woundedInBattle = troopRoster2;
        // }

        public void OnGameInitialized()
        {
            TroopRoster troopRoster1 = this._diedInBattle;
            if ((object)troopRoster1 == null)
                troopRoster1 = TroopRoster.CreateDummyTroopRoster();
            this._diedInBattle = troopRoster1;
            TroopRoster troopRoster2 = this._woundedInBattle;
            if ((object)troopRoster2 == null)
                troopRoster2 = TroopRoster.CreateDummyTroopRoster();
            this._woundedInBattle = troopRoster2;
            if (this._casualtiesInBattleObsolete != (TroopRoster)null)
            {
                foreach (TroopRosterElement troopRosterElement in this._casualtiesInBattleObsolete.GetTroopRoster())
                {
                    if (troopRosterElement.WoundedNumber > 0)
                        this.WoundedInBattle.AddToCounts(troopRosterElement.Character, troopRosterElement.WoundedNumber,
                            woundedCount: troopRosterElement.WoundedNumber);
                    if (troopRosterElement.Number > troopRosterElement.WoundedNumber)
                        this.DiedInBattle.AddToCounts(troopRosterElement.Character,
                            troopRosterElement.Number - troopRosterElement.WoundedNumber);
                }

                this._casualtiesInBattleObsolete = (TroopRoster)null;
            }

            if (this._alternativePartyToReceiveLootObsolete == null)
                return;
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Current.RosterToReceiveLootMembers =
                    this._alternativePartyToReceiveLootObsolete.MemberRoster;
                PlayerEncounter.Current.RosterToReceiveLootPrisoners =
                    this._alternativePartyToReceiveLootObsolete.PrisonRoster;
                PlayerEncounter.Current.RosterToReceiveLootItems =
                    this._alternativePartyToReceiveLootObsolete.ItemRoster;
            }
            else
                Debug.FailedAssert("Player Encounter null",
                    "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\MapEventParty.cs",
                    nameof(OnGameInitialized), 189);

            this._alternativePartyToReceiveLootObsolete = (PartyBase)null;
        }

        //[SaveableProperty(7)]
        public float GainedRenown { get; set; }

        //[SaveableProperty(8)]
        public float GainedInfluence { get; set; }

        //[SaveableProperty(9)]
        public float MoraleChange { get; set; }

        //[SaveableProperty(10)]
        public int PlunderedGold { get; set; }

        //[SaveableProperty(11)]
        public int GoldLost { get; set; }

        public void OnTroopKilled(UniqueTroopDescriptor troopSeed)
        {
            FlattenedTroopRosterElement troopRosterElement = this._roster[troopSeed];
            CharacterObject troop = troopRosterElement.Troop;
            this.Party.MemberRoster.AddTroopTempXp(troop, -troopRosterElement.XpGained);
            if (!troop.IsHero && this.Party.IsActive)
                this.Party.MemberRoster.RemoveTroop(troop, troopSeed: troopSeed);
            this._roster.OnTroopKilled(troopSeed);
            this.DiedInBattle.AddToCounts(this._roster[troopSeed].Troop, 1);
            ++this._contributionToBattle;
        }

        public void OnTroopWounded(UniqueTroopDescriptor troopSeed)
        {
            this.Party.MemberRoster.WoundTroop(this._roster[troopSeed].Troop, troopSeed: troopSeed);
            this._roster.OnTroopWounded(troopSeed);
            this.WoundedInBattle.AddToCounts(this._roster[troopSeed].Troop, 1, woundedCount: 1);
        }

        public void OnTroopRouted(UniqueTroopDescriptor troopSeed)
        {
        }

        public CharacterObject GetTroop(UniqueTroopDescriptor troopSeed) => this._roster[troopSeed].Troop;

        public void OnTroopScoreHit(
            UniqueTroopDescriptor attackerTroopDesc,
            CharacterObject attackedTroop,
            int damage,
            bool isFatal,
            bool isTeamKill,
            WeaponComponentData attackerWeapon,
            bool isSimulatedHit)
        {
            CharacterObject troop = this._roster[attackerTroopDesc].Troop;
            if (isTeamKill)
                return;
            int xpAmount;
            Campaign.Current.Models.CombatXpModel.GetXpFromHit(troop, (CharacterObject)null, attackedTroop, this.Party,
                damage, isFatal,
                isSimulatedHit ? CombatXpModel.MissionTypeEnum.SimulationBattle : CombatXpModel.MissionTypeEnum.Battle,
                out xpAmount);
            int num = xpAmount + MBRandom.RoundRandomized((float)xpAmount);
            if (!troop.IsHero)
            {
                if (num > 0)
                {
                    int gainedXp = this._roster.OnTroopGainXp(attackerTroopDesc, num);
                    this.Party.MemberRoster.AddTroopTempXp(troop, gainedXp);
                }
            }
            else
                CampaignEventDispatcher.Instance.OnHeroCombatHit(troop, attackedTroop, this.Party, attackerWeapon,
                    isFatal, num);

            this._contributionToBattle += num;
        }

        public void CommitXpGain()
        {
            if (this.Party.MobileParty == null)
                return;
            int num = 0;
            foreach (FlattenedTroopRosterElement troopRosterElement in this._roster)
            {
                CharacterObject troop = troopRosterElement.Troop;
                bool flag = Campaign.Current.Models.PartyTroopUpgradeModel.CanTroopGainXp(this.Party, troop);
                if (((troopRosterElement.IsKilled ? 0 : (troopRosterElement.XpGained > 0 ? 1 : 0)) & (flag ? 1 : 0)) !=
                    0)
                {
                    int xpGainFromBattles =
                        Campaign.Current.Models.PartyTrainingModel.CalculateXpGainFromBattles(troopRosterElement,
                            this.Party);
                    int sharedXp =
                        Campaign.Current.Models.PartyTrainingModel.GenerateSharedXp(troop, xpGainFromBattles,
                            this.Party.MobileParty);
                    if (sharedXp > 0)
                    {
                        num += sharedXp;
                        xpGainFromBattles -= sharedXp;
                    }

                    if (!troop.IsHero)
                        this.Party.MemberRoster.AddXpToTroop(xpGainFromBattles, troop);
                }
            }

            MobilePartyHelper.PartyAddSharedXp(this.Party.MobileParty, (float)num);
        }

        public void CommitSkillXpGains(MapEventSide otherSide)
        {
            MobileParty mobileParty = otherSide.LeaderParty.MobileParty;
            if (this.Party.LeaderHero == null || mobileParty == null ||
                otherSide.MissionSide != otherSide.MapEvent.DefeatedSide ||
                otherSide.OtherSide.LeaderParty != this.Party || !mobileParty.IsVillager && !mobileParty.IsCaravan)
                return;
            int troopCountBeforeBattle = otherSide.Parties
                .FirstOrDefault<MapEventParty>((Func<MapEventParty, bool>)(x => x.Party == otherSide.LeaderParty))
                .Troops.Count<FlattenedTroopRosterElement>();
            SkillLevelingManager.OnAssaultingVillagersAndCaravans(this.Party.MobileParty, mobileParty,
                troopCountBeforeBattle);
        }
    }
}