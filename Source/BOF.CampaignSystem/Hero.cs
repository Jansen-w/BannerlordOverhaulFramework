using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem
{
    public class Hero : MBObjectBase, ITrackableCampaignObject, ITrackableBase
    {
        public const int RelationLimit = 100;

        public const int RelationNeutralLimit = 10;

        //[SaveableField(120)]
        public int LastTimeStampForActivity;

        public const int MaximumNumberOfVolunteers = 6;

        //[SaveableField(130)]
        public CharacterObject[] VolunteerTypes;

        //[SaveableField(160)]
        private float _passedTimeAtHomeSettlement;

        //[SaveableField(170)]
        private CharacterObject _characterObject;

        //[SaveableField(180)]
        private TextObject _firstName;

        //[SaveableField(181)]
        private TextObject _name;

        //[SaveableField(201)]
        public string HairTags = "";

        //[SaveableField(202)]
        public string BeardTags = "";

        //[SaveableField(203)]
        public string TattooTags = "";

        //[SaveableField(230)]
        public bool Detected;

        //[SaveableField(260)]
        private Hero.CharacterStates _heroState;

        //[SaveableField(270)]
        private CharacterTraits _heroTraits;

        //[SaveableField(280)]
        private CharacterPerks _heroPerks;

        //[SaveableField(290)]
        private CharacterSkills _heroSkills;

        private int[] _obsoleteCharAttributeValues;

        //[SaveableField(301)]
        private CharacterAttributes _characterAttributes;

        //[SaveableField(360)]
        public float LastVisitTimeOfHomeSettlement;

        //[SaveableField(370)]
        public int Level;

        public const int HeroWoundedHealthLevel = 20;

        //[SaveableField(380)]
        private Clan _companionOf;

        [CachedData] private Hero.HeroLastSeenInformation _cachedLastSeenInformation;

        //[SaveableField(410)]
        private Hero.HeroLastSeenInformation _lastSeenInformationKnownToPlayer;

        public bool IsMercenary;

        //[SaveableField(420)]
        public int SpcDaysInLocation;

        //[SaveableField(430)]
        private int _health;

        //[SaveableField(441)]
        private float _defaultAge;

        //[SaveableField(440)]
        private TaleWorlds.CampaignSystem.CampaignTime _birthDay;

        //[SaveableField(450)]
        private TaleWorlds.CampaignSystem.CampaignTime _deathDay;

        //[SaveableField(460)]
        private float _power;

        //[SaveableField(500)]
        private Clan _clan;

        //[SaveableField(510)]
        private Clan _supporterOf;

        //[SaveableField(520)]
        private Town _governorOf;

        //[SaveableField(530)]
        private List<Workshop> _ownedWorkshops = new List<Workshop>();

        //[SaveableField(551)]
        public CultureObject Culture;

        [XmlIgnore]
        //[SaveableField(560)]
        private TaleWorlds.CampaignSystem.MobileParty _partyBelongedTo;

        //[SaveableField(580)]
        private Settlement _stayingInSettlement;

        //[SaveableField(590)]
        public List<ItemObject> SpecialItems;

        //[SaveableField(600)]
        private float _controversy;

        //[SaveableField(610)]
        private bool _hasMet;

        //[SaveableField(630)]
        private Settlement _bornSettlement;

        [CachedData] private Settlement _homeSettlement;

        //[SaveableField(650)]
        private int _gold;

        //[SaveableField(660)]
        public int RandomValue;

        //[SaveableField(670)]
        public int RandomValueDeterministic;

        //[SaveableField(680)]
        public int RandomValueRarelyChanging;

        //[SaveableField(700)]
        private Hero _father;

        //[SaveableField(710)]
        private Hero _mother;

        //[SaveableField(720)]
        private readonly List<Hero> _exSpouses;

        public MBReadOnlyList<Hero> ExSpouses;

        //[SaveableField(730)]
        private Hero _spouse;

        //[SaveableField(740)]
        private readonly List<Hero> _children = new List<Hero>();

        //[SaveableField(760)]
        public bool IsPregnant;

        //[SaveableField(770)]
        private readonly HeroDeveloper _heroDeveloper;

        //[SaveableProperty(100)]
        internal StaticBodyProperties StaticBodyProperties { get; set; }

        //[SaveableProperty(111)]
        public float Weight { get; set; }

        //[SaveableProperty(112)]
        public float Build { get; set; }

        public BodyProperties BodyProperties =>
            new BodyProperties(new DynamicBodyProperties(this.Age, this.Weight, this.Build), this.StaticBodyProperties);

        public float PassedTimeAtHomeSettlement
        {
            get => this._passedTimeAtHomeSettlement;
            set => this._passedTimeAtHomeSettlement = value;
        }

        public bool CanHaveRecruits =>
            TaleWorlds.CampaignSystem.Campaign.Current.Models.VolunteerProductionModel.CanHaveRecruits(this);

        public CharacterObject CharacterObject => this._characterObject;

        public void SetCharacterObject(CharacterObject characterObject)
        {
            this._characterObject = characterObject;
            this.SetInitialValuesFromCharacter(this._characterObject);
        }

        public TextObject FirstName => this._firstName;

        public TextObject Name => this._name;

        public void SetName(TextObject fullName, TextObject firstName)
        {
            this._name = fullName;
            this._firstName = firstName;
            if (this.PartyBelongedTo == null || this.PartyBelongedTo.LeaderHero != this)
                return;
            this.PartyBelongedTo.PartyComponent.ClearCachedName();
        }

        //[SaveableProperty(190)]
        public TextObject EncyclopediaText { get; set; }

        public string EncyclopediaLink =>
            TaleWorlds.CampaignSystem.Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(Hero)) + "-" +
            this.StringId ?? "";

        public TextObject EncyclopediaLinkWithName =>
            HyperlinkTexts.GetHeroHyperlinkText(this.EncyclopediaLink, this.Name);

        //[SaveableProperty(200)]
        public bool IsFemale { get; private set; }

        public void UpdatePlayerGender(bool isFemale) => this.IsFemale = isFemale;

        //[SaveableProperty(210)]
        public Equipment BattleEquipment { get; private set; }

        //[SaveableProperty(220)]
        public Equipment CivilianEquipment { get; private set; }

        //[SaveableProperty(240)]
        public TaleWorlds.CampaignSystem.CampaignTime CaptivityStartTime { get; set; }

        public Hero.CharacterStates HeroState
        {
            get => this._heroState;
            private set => this.ChangeState(value);
        }

        public CharacterTraits GetHeroTraits() => this._heroTraits;

        //[SaveableProperty(310)]
        public bool IsNoble { get; set; }

        //[SaveableProperty(320)]
        public bool IsMinorFactionHero { get; set; }

        public IssueBase Issue { get; private set; }

        public void OnIssueCreatedForHero(IssueBase issue) => this.Issue = issue;

        public void OnIssueDeactivatedForHero() => this.Issue = (IssueBase)null;

        public override string ToString() => this.Name.ToString();

        public bool CanBeCompanion => this.IsWanderer && this.CompanionOf == null;

        public bool Noncombatant => this.GetSkillValue(DefaultSkills.OneHanded) < 50 &&
                                    this.GetSkillValue(DefaultSkills.TwoHanded) < 50 &&
                                    this.GetSkillValue(DefaultSkills.Polearm) < 50 &&
                                    this.GetSkillValue(DefaultSkills.Throwing) < 50 &&
                                    this.GetSkillValue(DefaultSkills.Crossbow) < 50 &&
                                    this.GetSkillValue(DefaultSkills.Bow) < 50;

        public Clan CompanionOf
        {
            get => this._companionOf;
            set
            {
                if (value == this._companionOf)
                    return;
                this._homeSettlement = (Settlement)null;
                if (this._companionOf != null)
                    this._companionOf.RemoveCompanion(this);
                this._companionOf = value;
                if (this._companionOf == null)
                    return;
                this._companionOf.AddCompanion(this);
            }
        }

        public IEnumerable<Hero> CompanionsInParty
        {
            get
            {
                if (this.PartyBelongedTo != null && this.Clan != null)
                {
                    foreach (Hero companion in this.Clan.Companions)
                    {
                        if (companion.PartyBelongedTo == this.PartyBelongedTo)
                            yield return companion;
                    }
                }
            }
        }

        //[SaveableProperty(780)]
        public Occupation Occupation { get; internal set; }

        //[SaveableProperty(390)]
        public CharacterObject Template { get; set; }

        public bool IsDead => this.HeroState == Hero.CharacterStates.Dead;

        public bool IsFugitive => this.HeroState == Hero.CharacterStates.Fugitive;

        public bool IsPrisoner => this.HeroState == Hero.CharacterStates.Prisoner;

        public bool IsReleased => this.HeroState == Hero.CharacterStates.Released;

        public bool IsActive => this.HeroState == Hero.CharacterStates.Active;

        public bool IsNotSpawned => this.HeroState == Hero.CharacterStates.NotSpawned;

        public bool IsDisabled => this.HeroState == Hero.CharacterStates.Disabled;

        public bool IsAlive => !this.IsDead;

        //[SaveableProperty(400)]
        public KillCharacterAction.KillCharacterActionDetail DeathMark { get; private set; }

        //[SaveableProperty(401)]
        public Hero DeathMarkKillerHero { get; private set; }

        public Settlement LastSeenPlace => this._lastSeenInformationKnownToPlayer.LastSeenPlace;

        public TaleWorlds.CampaignSystem.CampaignTime LastSeenTime =>
            this._lastSeenInformationKnownToPlayer.LastSeenDate;

        public bool LastSeenInSettlement => !this._lastSeenInformationKnownToPlayer.IsNearbySettlement;

        public void CacheLastSeenInformation(Settlement settlement, bool isNearbySettlement = false)
        {
            if (settlement.IsHideout)
                return;
            this._cachedLastSeenInformation.LastSeenPlace = settlement;
            this._cachedLastSeenInformation.LastSeenDate = TaleWorlds.CampaignSystem.CampaignTime.Now;
            this._cachedLastSeenInformation.IsNearbySettlement = isNearbySettlement;
        }

        public void ClearLastSeenInformation()
        {
            this._cachedLastSeenInformation.LastSeenPlace = (Settlement)null;
            this._cachedLastSeenInformation.IsNearbySettlement = false;
            this.SyncLastSeenInformation();
        }

        public void SyncLastSeenInformation() =>
            this._lastSeenInformationKnownToPlayer = this._cachedLastSeenInformation;

        public void SetNewOccupation(Occupation occupation) => this.Occupation = occupation;

        public bool IsWanderer => this.Occupation == Occupation.Wanderer;

        public bool IsTemplate => this.CharacterObject.IsTemplate;

        public bool IsWounded => this.HitPoints <= 20;

        public bool IsPlayerCompanion => this.CompanionOf == Clan.PlayerClan;

        public bool IsMerchant => this.Occupation == Occupation.Merchant;

        public bool IsPreacher => this.Occupation == Occupation.Preacher;

        public bool IsHeadman => this.Occupation == Occupation.Headman;

        public bool IsGangLeader => this.Occupation == Occupation.GangLeader;

        public bool IsArtisan => this.Occupation == Occupation.Artisan;

        public bool IsRuralNotable => this.Occupation == Occupation.RuralNotable;

        public bool IsUrbanNotable => this.Occupation == Occupation.Merchant || this.Occupation == Occupation.Artisan ||
                                      this.Occupation == Occupation.GangLeader;

        public bool IsSpecial => this.Occupation == Occupation.Special;

        public bool IsRebel => this.Clan != null && this.Clan.IsRebelClan;

        public bool IsCommander => this.GetTraitLevel(DefaultTraits.Commander) > 0;

        public bool IsPartyLeader => this.PartyBelongedTo != null && this.PartyBelongedTo.LeaderHero == this;

        public bool IsNotable => this.IsArtisan || this.IsGangLeader || this.IsPreacher || this.IsMerchant ||
                                 this.IsRuralNotable || this.IsHeadman;

        public bool AwaitingTrial => this.IsPrisoner;

        public int MaxHitPoints => this.CharacterObject.MaxHitPoints();

        public int HitPoints
        {
            get => this._health;
            set
            {
                if (this._health == value)
                    return;
                int health = this._health;
                this._health = value;
                if (this._health < 0)
                    this._health = 1;
                else if (this._health > this.CharacterObject.MaxHitPoints())
                    this._health = this.CharacterObject.MaxHitPoints();
                if (health <= 20 != this._health <= 20)
                {
                    if (this.PartyBelongedTo != null)
                        this.PartyBelongedTo.MemberRoster.OnHeroHealthStatusChanged(this);
                    if (this.PartyBelongedToAsPrisoner != null)
                        this.PartyBelongedToAsPrisoner.PrisonRoster.OnHeroHealthStatusChanged(this);
                }

                if (health <= 20 || !this.IsWounded)
                    return;
                CampaignEventDispatcher.Instance.OnHeroWounded(this);
            }
        }

        public TaleWorlds.CampaignSystem.CampaignTime BirthDay => CampaignOptions.IsLifeDeathCycleDisabled
            ? TaleWorlds.CampaignSystem.CampaignTime.YearsFromNow(-this._defaultAge)
            : this._birthDay;

        public TaleWorlds.CampaignSystem.CampaignTime DeathDay
        {
            get => CampaignOptions.IsLifeDeathCycleDisabled
                ? TaleWorlds.CampaignSystem.CampaignTime.YearsFromNow(-this._defaultAge) +
                  TaleWorlds.CampaignSystem.CampaignTime.Years(this._defaultAge)
                : this._deathDay;
            set => this._deathDay = value;
        }

        public float Age
        {
            get
            {
                if (CampaignOptions.IsLifeDeathCycleDisabled)
                    return this._defaultAge;
                return this.IsAlive
                    ? this._birthDay.ElapsedYearsUntilNow
                    : (float)(this.DeathDay - this._birthDay).ToYears;
            }
        }

        public void SetBirthDay(TaleWorlds.CampaignSystem.CampaignTime birthday)
        {
            this._birthDay = birthday;
            this._defaultAge = birthday.IsNow ? 1f / 1000f : this._birthDay.ElapsedYearsUntilNow;
        }

        public bool IsChild => (double)this.Age <
                               (double)TaleWorlds.CampaignSystem.Campaign.Current.Models.AgeModel.HeroComesOfAge;

        public float Power => this._power;

        public void AddPower(float value) => this._power += value;

        public Banner ClanBanner => this.Clan?.Banner;

        //[SaveableProperty(481)]
        public long LastExaminedLogEntryID { get; set; }

        //[SaveableProperty(490)]
        internal TaleWorlds.CampaignSystem.CampaignTime LastCommentTime { get; set; }

        public Clan Clan
        {
            get => this.CompanionOf ?? this._clan;
            set
            {
                if (this._clan == value)
                    return;
                this._homeSettlement = (Settlement)null;
                if (this._clan != null)
                    this._clan.RemoveHeroInternal(this);
                Clan clan = this._clan;
                this._clan = value;
                if (this._clan != null)
                    this._clan.AddHeroInternal(this);
                CampaignEventDispatcher.Instance.OnHeroChangedClan(this, clan);
            }
        }

        public Clan SupporterOf
        {
            get => this._supporterOf;
            set
            {
                if (this._supporterOf == value)
                    return;
                if (this._supporterOf != null)
                    this._supporterOf.RemoveSupporterInternal(this);
                this._supporterOf = value;
                if (this._supporterOf == null)
                    return;
                this._supporterOf.AddSupporterInternal(this);
            }
        }

        public Town GovernorOf
        {
            get => this._governorOf;
            set
            {
                if (value == this._governorOf)
                    return;
                this._governorOf = value;
            }
        }

        public IFaction MapFaction
        {
            get
            {
                if (this.Clan != null)
                    return (IFaction)this.Clan.Kingdom ?? (IFaction)this.Clan;
                if (this.IsSpecial)
                    return (IFaction)null;
                if (this.HomeSettlement != null)
                    return this.HomeSettlement.MapFaction;
                return this.PartyBelongedTo != null ? this.PartyBelongedTo.MapFaction : (IFaction)null;
            }
        }

        public List<CommonAreaPartyComponent> OwnedCommonAreas { get; private set; }

        public bool IsFactionLeader => this.MapFaction != null && this.MapFaction.Leader == this;

        public List<CaravanPartyComponent> OwnedCaravans { get; private set; }

        public TaleWorlds.CampaignSystem.MobileParty PartyBelongedTo
        {
            get => this._partyBelongedTo;
            private set => this.SetPartyBelongedTo(value);
        }

        //[SaveableProperty(570)]
        public PartyBase PartyBelongedToAsPrisoner { get; private set; }

        public Settlement StayingInSettlement
        {
            get => this._stayingInSettlement;
            set
            {
                if (this._stayingInSettlement == value)
                    return;
                if (this._stayingInSettlement != null)
                {
                    this._stayingInSettlement.RemoveHeroWithoutParty(this);
                    this._stayingInSettlement = (Settlement)null;
                }

                value?.AddHeroWithoutParty(this);
                this._stayingInSettlement = value;
            }
        }

        public float Controversy
        {
            get => this._controversy;
            set
            {
                if ((double)value < 0.0)
                    value = 0.0f;
                else if ((double)value > 100.0)
                    value = 100f;
                this._controversy = value;
            }
        }

        public bool IsHumanPlayerCharacter => this == Hero.MainHero;

        public bool HasMet
        {
            get => this._hasMet;
            set
            {
                if (this._hasMet != value)
                {
                    this._hasMet = value;
                    this.Detected = true;
                    CampaignEventDispatcher.Instance.OnPlayerMetCharacter(this);
                }

                if (!value)
                    return;
                HeroHelper.SetLastSeenLocation(this, true);
            }
        }

        //[SaveableProperty(620)]
        public CampaignTime LastMeetingTimeWithPlayer { get; set; }

        public Settlement BornSettlement
        {
            get => this._bornSettlement;
            set
            {
                this._bornSettlement = value;
                this._homeSettlement = (Settlement)null;
            }
        }

        public Settlement HomeSettlement
        {
            get
            {
                if (this._homeSettlement == null)
                    this.UpdateHomeSettlement();
                return this._homeSettlement;
            }
        }

        public void UpdateHomeSettlement()
        {
            if (this.GovernorOf != null)
                this._homeSettlement = this.GovernorOf.Owner.Settlement;
            else if (this.Spouse != null && this.Spouse.GovernorOf != null)
            {
                this._homeSettlement = this.Spouse.GovernorOf.Owner.Settlement;
            }
            else
            {
                foreach (Hero child in this.Children)
                {
                    if (child.GovernorOf != null && child.Clan == this.Clan)
                    {
                        this._homeSettlement = child.GovernorOf.Owner.Settlement;
                        return;
                    }
                }

                if (this.Father != null && this.Father.GovernorOf != null && this.Father.Clan == this.Clan)
                    this._homeSettlement = this.Father.GovernorOf.Owner.Settlement;
                else if (this.Mother != null && this.Mother.GovernorOf != null && this.Mother.Clan == this.Clan)
                {
                    this._homeSettlement = this.Mother.GovernorOf.Owner.Settlement;
                }
                else
                {
                    foreach (Hero sibling in this.Siblings)
                    {
                        if (sibling.GovernorOf != null && sibling.Clan == this.Clan)
                        {
                            this._homeSettlement = sibling.GovernorOf.Owner.Settlement;
                            return;
                        }
                    }

                    if (this.Clan != null && !this.Clan.IsNeutralClan)
                        this._homeSettlement = this.Clan.HomeSettlement;
                    else if (this.CompanionOf != null &&
                             TaleWorlds.CampaignSystem.CampaignData.NeutralFaction != this.CompanionOf)
                        this._homeSettlement = this.CompanionOf.HomeSettlement;
                    else if (this.IsNotable && this.CurrentSettlement != null)
                        this._homeSettlement = this.CurrentSettlement;
                    else
                        this._homeSettlement = this._bornSettlement;
                }
            }
        }

        public Settlement CurrentSettlement
        {
            get
            {
                Settlement settlement = (Settlement)null;
                if (this.PartyBelongedTo != null)
                    settlement = this.PartyBelongedTo.CurrentSettlement;
                else if (this.PartyBelongedToAsPrisoner != null)
                    settlement = this.PartyBelongedToAsPrisoner.IsSettlement
                        ? this.PartyBelongedToAsPrisoner.Settlement
                        : (this.PartyBelongedToAsPrisoner.IsMobile
                            ? this.PartyBelongedToAsPrisoner.MobileParty.CurrentSettlement
                            : (Settlement)null);
                else if (this.StayingInSettlement != null)
                    settlement = this.StayingInSettlement;
                return settlement;
            }
        }

        public int Gold
        {
            get => this._gold;
            set => this._gold = MathF.Max(0, value);
        }

        public Hero.FactionRank Rank
        {
            get
            {
                if (this.Clan == null || this.Occupation != Occupation.Lord)
                    return Hero.FactionRank.None;
                return this != this.Clan.Leader ? Hero.FactionRank.Vassal : Hero.FactionRank.Leader;
            }
        }

        //[SaveableProperty(690)]
        public float ProbabilityOfDeath { get; set; }

        //[SaveableProperty(790)]
        public EquipmentElement BannerItem { get; set; }

        public Hero Father
        {
            get => this._father;
            set
            {
                this._father = value;
                if (this._father == null)
                    return;
                this._father._children.Add(this);
            }
        }

        public Hero Mother
        {
            get => this._mother;
            set
            {
                this._mother = value;
                if (this._mother == null)
                    return;
                this._mother._children.Add(this);
            }
        }

        public Hero Spouse
        {
            get => this._spouse;
            set
            {
                if (this._spouse == value)
                    return;
                Hero spouse = this._spouse;
                this._spouse = value;
                if (spouse != null)
                {
                    this._exSpouses.Add(spouse);
                    spouse.Spouse = (Hero)null;
                }

                if (this._spouse == null)
                    return;
                this._spouse.Spouse = this;
            }
        }

        public List<Hero> Children => this._children;

        public IEnumerable<Hero> Siblings
        {
            get
            {
                Hero hero = this;
                if (hero.Father != null)
                {
                    foreach (Hero child in hero.Father._children)
                    {
                        if (child != hero)
                            yield return child;
                    }
                }
                else if (hero.Mother != null)
                {
                    foreach (Hero child in hero.Mother._children)
                    {
                        if (child != hero)
                            yield return child;
                    }
                }
            }
        }

        public HeroDeveloper HeroDeveloper => this._heroDeveloper;

        public int GetSkillValue(SkillObject skill) => this._heroSkills.GetPropertyValue(skill);

        internal void SetSkillValueInternal(SkillObject skill, int value) =>
            this._heroSkills.SetPropertyValue(skill, value);

        public void ClearSkills() => this._heroSkills.ClearAllProperty();

        public void AddSkillXp(SkillObject skill, float xpAmount)
        {
            if (this._heroDeveloper == null)
                return;
            this._heroDeveloper.AddSkillXp(skill, xpAmount);
        }

        public int GetAttributeValue(CharacterAttribute charAttribute) =>
            this._characterAttributes.GetPropertyValue(charAttribute);

        internal void SetAttributeValueInternal(CharacterAttribute charAttribute, int value)
        {
            int num = MBMath.ClampInt(value, 0, 10);
            this._characterAttributes.SetPropertyValue(charAttribute, num);
        }

        public void ClearAttributes() => this._characterAttributes.ClearAllProperty();

        public void SetTraitLevelInternal(TraitObject trait, int value)
        {
            value = MBMath.ClampInt(value, trait.MinValue, trait.MaxValue);
            this._heroTraits.SetPropertyValue(trait, value);
        }

        public int GetTraitLevel(TraitObject trait) => this._heroTraits.GetPropertyValue(trait);

        public void ClearTraits() => this._heroTraits.ClearAllProperty();

        internal void SetPerkValueInternal(PerkObject perk, bool value)
        {
            this._heroPerks.SetPropertyValue(perk, value ? 1 : 0);
            if (!value)
                return;
            CampaignEventDispatcher.Instance.OnPerkOpened(this, perk);
        }

        public bool GetPerkValue(PerkObject perk) => (uint)this._heroPerks.GetPropertyValue(perk) > 0U;

        public void ClearPerks()
        {
            this._heroPerks.ClearAllProperty();
            this.HitPoints = MathF.Min(this.HitPoints, this.MaxHitPoints);
        }

        public static Hero CreateHero(string stringID)
        {
            stringID =
                TaleWorlds.CampaignSystem.Campaign.Current.CampaignObjectManager.FindNextUniqueStringId<Hero>(stringID);
            Hero hero = new Hero(stringID);
            TaleWorlds.CampaignSystem.Campaign.Current.CampaignObjectManager.AddHero(hero);
            return hero;
        }

        public Hero(string stringID)
        {
            this.StringId = stringID;
            this._heroDeveloper = new HeroDeveloper(this);
            this._exSpouses = new List<Hero>();
            this.Init();
        }

        public Hero()
        {
            this._heroDeveloper = new HeroDeveloper(this);
            this._exSpouses = new List<Hero>();
            this.Init();
        }

        public void Init()
        {
            this.ExSpouses = this._exSpouses.GetReadOnlyList<Hero>();
            this.BattleEquipment = (Equipment)null;
            this.CivilianEquipment = (Equipment)null;
            this._gold = 0;
            this.OwnedCaravans = new List<CaravanPartyComponent>();
            this.OwnedCommonAreas = new List<CommonAreaPartyComponent>();
            this.SpecialItems = new List<ItemObject>();
            this.Detected = false;
            this._health = 1;
            this._deathDay = TaleWorlds.CampaignSystem.CampaignTime.Never;
            this.RandomValue = MBRandom.RandomInt();
            this.RandomValueDeterministic = MBRandom.DeterministicRandomInt(100);
            this.RandomValueRarelyChanging = MBRandom.DeterministicRandomInt(100);
            this.HeroState = Hero.CharacterStates.NotSpawned;
            this._heroSkills = new CharacterSkills();
            this._heroTraits = new CharacterTraits();
            this._heroPerks = new CharacterPerks();
            this._characterAttributes = new CharacterAttributes();
            this.VolunteerTypes = new CharacterObject[6];
            this.LastVisitTimeOfHomeSettlement = TaleWorlds.CampaignSystem.Campaign.CurrentTime;
            this.ProbabilityOfDeath = 0.0f;
        }

        [LoadInitializationCallback]
        private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
        {
            this.OwnedCaravans = new List<CaravanPartyComponent>();
            this.OwnedCommonAreas = new List<CommonAreaPartyComponent>();
            this.ExSpouses = this._exSpouses.GetReadOnlyList<Hero>();
            this._cachedLastSeenInformation = this._lastSeenInformationKnownToPlayer;
            if (this._characterAttributes == null)
                this._obsoleteCharAttributeValues = (int[])objectLoadData.GetDataValueBySaveId(300);
            ApplicationVersion applicationVersion = metaData.GetApplicationVersion();
            if (applicationVersion.Major <= 1 && applicationVersion.Minor <= 4 && applicationVersion.Revision <= 2 &&
                applicationVersion.ApplicationVersionType == ApplicationVersionType.EarlyAccess)
            {
                object dataValueBySaveId = objectLoadData.GetDataValueBySaveId(110);
                if (dataValueBySaveId != null)
                {
                    DynamicBodyProperties dynamicBodyProperties = (DynamicBodyProperties)dataValueBySaveId;
                    this.Weight = dynamicBodyProperties.Weight;
                    this.Build = dynamicBodyProperties.Build;
                }
            }

            this.HairTags = this.HairTags ?? "";
            this.BeardTags = this.BeardTags ?? "";
            this.TattooTags = this.TattooTags ?? "";
        }

        protected override void PreAfterLoad()
        {
            if (this._obsoleteCharAttributeValues != null)
            {
                this._characterAttributes = new CharacterAttributes();
                this.SetAttributeValueInternal(DefaultCharacterAttributes.Vigor, this._obsoleteCharAttributeValues[0]);
                this.SetAttributeValueInternal(DefaultCharacterAttributes.Control,
                    this._obsoleteCharAttributeValues[1]);
                this.SetAttributeValueInternal(DefaultCharacterAttributes.Endurance,
                    this._obsoleteCharAttributeValues[2]);
                this.SetAttributeValueInternal(DefaultCharacterAttributes.Cunning,
                    this._obsoleteCharAttributeValues[3]);
                this.SetAttributeValueInternal(DefaultCharacterAttributes.Social, this._obsoleteCharAttributeValues[4]);
                this.SetAttributeValueInternal(DefaultCharacterAttributes.Intelligence,
                    this._obsoleteCharAttributeValues[5]);
                this._obsoleteCharAttributeValues = (int[])null;
            }

            if (this.Occupation == Occupation.NotAssigned)
                this.Occupation = this._characterObject.GetDefaultOccupation();
            TaleWorlds.CampaignSystem.CampaignTime campaignTime;
            if (this._defaultAge.ApproximatelyEqualsTo(0.0f))
            {
                if (!CampaignOptions.IsLifeDeathCycleDisabled)
                {
                    double num;
                    if (!this.IsAlive)
                    {
                        campaignTime = this._deathDay - this._birthDay;
                        num = campaignTime.ToYears;
                    }
                    else
                        num = (double)this._birthDay.ElapsedYearsUntilNow;

                    this._defaultAge = (float)num;
                }
                else if (this.IsAlive)
                {
                    campaignTime = TaleWorlds.CampaignSystem.CampaignTime.Years(1084f) - this._birthDay;
                    this._defaultAge = (float)campaignTime.ToYears;
                }
            }

            if (this.IsAlive && CampaignOptions.IsLifeDeathCycleDisabled && (double)this._defaultAge >=
                (double)TaleWorlds.CampaignSystem.Campaign.Current.Models.AgeModel.MaxAge)
            {
                campaignTime = TaleWorlds.CampaignSystem.CampaignTime.Years(1084f) - this._birthDay;
                this._defaultAge = (float)campaignTime.ToYears;
            }

            if (this._supporterOf != null)
                this._supporterOf.AddSupporterInternal(this);
            if (this._clan != null)
                this._clan.AddHeroInternal(this);
            if (this.CurrentSettlement != null && this.PartyBelongedTo == null &&
                this.PartyBelongedToAsPrisoner == null)
                this.CurrentSettlement.AddHeroWithoutParty(this);
            if (this._companionOf == null)
                return;
            this._companionOf.AddCompanion(this);
        }

        protected override void OnBeforeLoad()
        {
            if (MBSaveLoad.LastLoadedGameVersion <
                ApplicationVersion.FromString("e1.6.0", ApplicationVersionGameType.Singleplayer))
            {
                base.OnBeforeLoad();
            }
            else
            {
                this.OnUnregistered();
                this.IsReady = true;
            }
        }

        protected override void AfterLoad()
        {
            if (this.PartyBelongedTo != null && !this.IsActive)
                this.ChangeState(Hero.CharacterStates.Active);
            if (MBSaveLoad.LastLoadedGameVersion <=
                ApplicationVersion.FromString("e1.6.5", ApplicationVersionGameType.Singleplayer) && this.IsAlive &&
                (this.HeroState == Hero.CharacterStates.NotSpawned && this.LastSeenPlace != null && (double)this.Age >=
                 (double)TaleWorlds.CampaignSystem.Campaign.Current.Models.AgeModel.HeroComesOfAge ||
                 this.Occupation == Occupation.NotAssigned))
                KillCharacterAction.ApplyByRemove(this);
            this.HeroDeveloper.AfterLoadInternal();
            if (this.PartyBelongedToAsPrisoner != null && this.PartyBelongedToAsPrisoner.MobileParty == null &&
                !this.IsNotable && !Settlement.All
                    .Select<Settlement, PartyBase>((Func<Settlement, PartyBase>)(x => x.Party))
                    .Contains<PartyBase>(this.PartyBelongedToAsPrisoner))
            {
                Debug.FailedAssert(this.GetName().ToString() + " is a prisoner of a dummy party",
                    "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Hero.cs", nameof(AfterLoad),
                    1376);
                this.PartyBelongedToAsPrisoner = (PartyBase)null;
                this._stayingInSettlement = (Settlement)null;
                TeleportHeroAction.ApplyForCharacter(this, this.HomeSettlement);
            }

            if (this.PartyBelongedToAsPrisoner != null && !this.IsPrisoner)
            {
                this.PartyBelongedToAsPrisoner.PrisonRoster.AddToCounts(this.CharacterObject, -1);
                this.PartyBelongedToAsPrisoner = (PartyBase)null;
                this._stayingInSettlement = (Settlement)null;
                TeleportHeroAction.ApplyForCharacter(this, this.HomeSettlement);
            }

            if (this.IsNotable && this.PartyBelongedTo == TaleWorlds.CampaignSystem.MobileParty.MainParty)
            {
                TaleWorlds.CampaignSystem.MobileParty.MainParty.MemberRoster.AddToCounts(this.CharacterObject, -1);
                TeleportHeroAction.ApplyForCharacter(this, this.BornSettlement);
            }

            if (this.IsNotable && this.CurrentSettlement != null &&
                (this.CurrentSettlement.IsFortification && (this.IsRuralNotable || this.IsHeadman) ||
                 this.CurrentSettlement.IsVillage && (this.IsGangLeader || this.IsArtisan || this.IsMerchant)))
            {
                Settlement targetSettlement = this.BornSettlement;
                if ((this.IsRuralNotable || this.IsHeadman) && !this.BornSettlement.IsVillage)
                    targetSettlement = SettlementHelper.FindNearestVillage(
                        (Func<Settlement, bool>)(v => v.Culture == this.Culture), (IMapPoint)this.BornSettlement);
                else if ((this.IsGangLeader || this.IsArtisan || this.IsMerchant) && !this.BornSettlement.IsTown)
                    targetSettlement = SettlementHelper.FindNearestTown(
                        (Func<Settlement, bool>)(v => v.Culture == this.Culture), (IMapPoint)this.BornSettlement);
                TeleportHeroAction.ApplyForCharacter(this, targetSettlement);
            }

            if (this.IsWanderer && !this.IsDead)
            {
                bool flag = false;
                foreach (SkillObject skill in Skills.All)
                {
                    if (this.GetSkillValue(skill) > 0)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                    KillCharacterAction.ApplyByRemove(this);
            }

            if (this.CurrentSettlement == null || !this.IsPrisoner || this.PartyBelongedToAsPrisoner != null)
                return;
            this.PartyBelongedToAsPrisoner = this.CurrentSettlement.Party;
        }

        public void ChangeState(Hero.CharacterStates newState)
        {
            Hero.CharacterStates heroState = this._heroState;
            this._heroState = newState;
            TaleWorlds.CampaignSystem.Campaign.Current.CampaignObjectManager.HeroStateChanged(this, heroState);
        }

        public bool IsHealthFull() => this.HitPoints >= this.CharacterObject.MaxHitPoints();

        private void HealByAmountInternal(PartyBase party, int healingAmount, bool addXp = false)
        {
            if (this.IsHealthFull())
                return;
            int healingAmount1 = MathF.Min(healingAmount, this.CharacterObject.MaxHitPoints() - this.HitPoints);
            this.HitPoints += healingAmount1;
            if (!addXp || party == null)
                return;
            SkillLevelingManager.OnHeroHealedWhileWaiting(party.MobileParty, healingAmount1);
        }

        public void Heal(PartyBase party, int healAmount, bool addXp = false)
        {
            int effectedHealingAmount =
                TaleWorlds.CampaignSystem.Campaign.Current.Models.PartyHealingModel.GetHeroesEffectedHealingAmount(this,
                    (float)healAmount);
            this.HealByAmountInternal(party, effectedHealingAmount, addXp);
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            this.SetCharacterObject(MBObjectManager.Instance.GetObject<CharacterObject>(this.StringId));
            this.IsFemale = this.CharacterObject.IsFemale;
            this.StaticBodyProperties = this.CharacterObject.GetBodyPropertiesMin(false).StaticProperties;
            DynamicBodyProperties dynamicProperties = this.CharacterObject.GetBodyPropertiesMin(true).DynamicProperties;
            if (dynamicProperties == DynamicBodyProperties.Invalid)
                dynamicProperties = DynamicBodyProperties.Default;
            this.Weight = dynamicProperties.Weight;
            this.Build = dynamicProperties.Build;
            this.CharacterObject.HeroObject = this;
            XmlAttribute attribute1 = node.Attributes["alive"];
            this._heroState = attribute1 != null && attribute1.Value == "false"
                ? Hero.CharacterStates.Dead
                : Hero.CharacterStates.NotSpawned;
            if (this.IsDead)
                this.DeathDay = this._birthDay + TaleWorlds.CampaignSystem.CampaignTime.Years(this._defaultAge);
            XmlNode attribute2 = (XmlNode)node.Attributes["is_noble"];
            if (attribute2 != null)
                this.IsNoble = Convert.ToBoolean(attribute2.InnerText);
            this.Father = objectManager.ReadObjectReferenceFromXml("father", typeof(Hero), node) as Hero;
            this.Mother = objectManager.ReadObjectReferenceFromXml("mother", typeof(Hero), node) as Hero;
            if (this.Spouse == null)
                this.Spouse = objectManager.ReadObjectReferenceFromXml("spouse", typeof(Hero), node) as Hero;
            Clan clan = objectManager.ReadObjectReferenceFromXml("faction", typeof(Clan), node) as Clan;
            if (clan.StringId != "neutral")
                this.Clan = clan;
            this.EncyclopediaText = node.Attributes["text"] != null
                ? new TextObject(node.Attributes["text"].Value)
                : TextObject.Empty;
        }

        public bool CanLeadParty()
        {
            bool result = true;
            CampaignEventDispatcher.Instance.CanHeroLeadParty(this, ref result);
            return result;
        }

        public static string SetHeroEncyclopediaTextAndLinks(Hero o)
        {
            StringHelpers.SetCharacterProperties("LORD", o.CharacterObject);
            MBTextManager.SetTextVariable("TITLE", HeroHelper.GetTitleInIndefiniteCase(o), false);
            MBTextManager.SetTextVariable("REPUTATION", CharacterHelper.GetReputationDescription(o.CharacterObject),
                false);
            MBTextManager.SetTextVariable("FACTION_NAME",
                GameTexts.FindText("str_neutral_term_for_culture",
                    o.MapFaction.IsMinorFaction ? o.Culture.StringId : o.MapFaction.Culture.StringId), false);
            if (o.MapFaction.Culture.StringId == "empire")
                MBTextManager.SetTextVariable("FACTION_NAME", "{=empirefaction}Empire", false);
            MBTextManager.SetTextVariable("CLAN_NAME", o.Clan.Name, false);
            if (o.Clan.IsMinorFaction || o.Clan.IsRebelClan)
            {
                if (o.Clan == Hero.MainHero.Clan)
                    MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=REWGj2ge}a rising new clan", false);
                else if (o.Clan.IsSect)
                    MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=IlRC9Drl}a religious sect", false);
                else if (o.Clan.IsClanTypeMercenary)
                    MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=5cH6ssDI}a mercenary company", false);
                else if (o.Clan.IsNomad)
                    MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=nt1ra97u}a nomadic clan", false);
                else if (o.Clan.IsMafia)
                    MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=EmBEupR5}a secret society", false);
                else
                    MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=KZxKVby0}an organization", false);
                return o == Hero.MainHero && o.GetTraitLevel(DefaultTraits.Mercy) == 0 &&
                       o.GetTraitLevel(DefaultTraits.Honor) == 0 && o.GetTraitLevel(DefaultTraits.Generosity) == 0 &&
                       o.GetTraitLevel(DefaultTraits.Valor) == 0 && o.GetTraitLevel(DefaultTraits.Calculating) == 0
                    ? new TextObject(
                            "{=FHjM62IY}{LORD.FIRSTNAME} is a member of the {CLAN_NAME}, a rising new clan. {?LORD.GENDER}She{?}He{\\?} is still making {?LORD.GENDER}her{?}his{\\?} reputation.")
                        .ToString()
                    : new TextObject(
                            "{=9Obe3S6L}{LORD.FIRSTNAME} is a member of the {CLAN_NAME}, {CLAN_DESCRIPTION} from the lands of the {FACTION_NAME}. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}.")
                        .ToString();
            }

            List<Kingdom> list = TaleWorlds.CampaignSystem.Campaign.Current.Kingdoms
                .Where<Kingdom>((Func<Kingdom, bool>)(x => x.Culture == o.MapFaction.Culture)).ToList<Kingdom>();
            if (list.Count > 1)
                MBTextManager.SetTextVariable("RULER", o.MapFaction.Leader.Name, false);
            MBTextManager.SetTextVariable("CLAN_DESCRIPTION", "{=KzSeg8ks}a noble family", false);
            if (list.Count == 1)
                return o.Clan.Leader == o
                    ? new TextObject(
                            "{=6d4ZTvGv}{LORD.NAME} is {TITLE} of the {FACTION_NAME} and head of the {CLAN_NAME}, {CLAN_DESCRIPTION} of the realm. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}")
                        .ToString()
                    : new TextObject(
                            "{=o5AUljbW}{LORD.NAME} is a member of the {CLAN_NAME}, {CLAN_DESCRIPTION} of the {FACTION_NAME}. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}")
                        .ToString();
            if (list.Count <= 1)
                return new TextObject("{=!}Placeholder text").ToString();
            return o.Clan.Leader == o
                ? new TextObject(
                        "{=JuPUG5wX}{LORD.NAME} is {TITLE} of the {FACTION_NAME} and head of the {CLAN_NAME}, {CLAN_DESCRIPTION} that is backing {RULER} in the civil war. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}")
                    .ToString()
                : new TextObject(
                        "{=0bPb5btR}{LORD.NAME} is a member of the {CLAN_NAME}, {CLAN_DESCRIPTION} of the {FACTION_NAME} that is backing {RULER} in the civil war. {?LORD.GENDER}She{?}He{\\?} has the reputation of being {REPUTATION}")
                    .ToString();
        }

        public bool CanHeroEquipmentBeChanged()
        {
            bool result = true;
            CampaignEventDispatcher.Instance.CanHeroEquipmentBeChanged(this, ref result);
            return result;
        }

        public bool CanMarry()
        {
            if (!TaleWorlds.CampaignSystem.Campaign.Current.Models.MarriageModel.IsSuitableForMarriage(this))
                return false;
            bool result = true;
            CampaignEventDispatcher.Instance.CanHeroMarry(this, ref result);
            return result;
        }

        private void SetPartyBelongedTo(TaleWorlds.CampaignSystem.MobileParty party)
        {
            if (this._partyBelongedTo != party && this._partyBelongedTo != null &&
                this._partyBelongedTo.LeaderHero == this)
                this._partyBelongedTo.PartyComponent.ChangePartyLeader((Hero)null);
            this._partyBelongedTo = party;
        }

        public bool CanBeGovernorOrHavePartyRole()
        {
            if (this.IsPrisoner)
                return false;
            bool result = true;
            CampaignEventDispatcher.Instance.CanBeGovernorOrHavePartyRole(this, ref result);
            return result;
        }

        public bool CanDie(
            KillCharacterAction.KillCharacterActionDetail causeOfDeath)
        {
            if (CampaignOptions.IsLifeDeathCycleDisabled)
                return false;
            bool result = true;
            CampaignEventDispatcher.Instance.CanHeroDie(this, causeOfDeath, ref result);
            return result;
        }

        public bool CanBecomePrisoner()
        {
            if (this != Hero.MainHero)
                return true;
            bool result = true;
            CampaignEventDispatcher.Instance.CanHeroBecomePrisoner(this, ref result);
            return result;
        }

        public bool CanMoveToSettlement()
        {
            bool result = true;
            CampaignEventDispatcher.Instance.CanMoveToSettlement(this, ref result);
            return result;
        }

        public bool CanHaveQuestsOrIssues()
        {
            if (this.Issue != null)
                return false;
            bool result = true;
            CampaignEventDispatcher.Instance.CanHaveQuestsOrIssues(this, ref result);
            return result;
        }

        public string AssignVoice()
        {
            double age = (double)this.CharacterObject.Age;
            this.GetTraitLevel(DefaultTraits.Mercy);
            int traitLevel1 = this.GetTraitLevel(DefaultTraits.Generosity);
            this.GetTraitLevel(DefaultTraits.Valor);
            int traitLevel2 = this.GetTraitLevel(DefaultTraits.Calculating);
            int traitLevel3 = this.GetTraitLevel(DefaultTraits.Honor);
            int traitLevel4 = this.GetTraitLevel(DefaultTraits.Politician);
            int traitLevel5 = this.GetTraitLevel(DefaultTraits.Commander);
            string str1 = (string)null;
            if (this.CharacterObject.Culture.StringId == "empire" ||
                this.CharacterObject.Culture.StringId == "vlandia" && (this.IsNoble || this.IsMerchant))
                str1 = "upperwest";
            else if (this.CharacterObject.Culture.StringId == "empire" ||
                     this.CharacterObject.Culture.StringId == "vlandia")
                str1 = "lowerwest";
            else if (this.CharacterObject.Culture.StringId == "sturgia" ||
                     this.CharacterObject.Culture.StringId == "battania")
                str1 = "north";
            else if (this.CharacterObject.Culture.StringId == "aserai" ||
                     this.CharacterObject.Culture.StringId == "khuzait")
                str1 = "east";
            string str2 = "earnest";
            if (traitLevel4 < 3 && traitLevel1 < 0)
                str2 = "curt";
            else if (traitLevel4 < 3 && traitLevel5 < 5)
                str2 = "softspoken";
            else if (traitLevel2 - traitLevel3 > -1)
                str2 = "ironic";
            return str2 + "_" + str1;
        }

        public void AddInfluenceWithKingdom(float additionalInfluence)
        {
            float randomFloat = MBRandom.RandomFloat;
            this.Clan.Influence += (float)((int)additionalInfluence +
                                           ((double)randomFloat < (double)additionalInfluence -
                                               (double)MathF.Floor(additionalInfluence)
                                                   ? 1
                                                   : 0));
        }

        public float GetRelationWithPlayer() => (float)Hero.MainHero.GetRelation(this);

        public float GetUnmodifiedClanLeaderRelationshipWithPlayer() => (float)Hero.MainHero.GetBaseHeroRelation(this);

        public void SetTextVariables()
        {
            MBTextManager.SetTextVariable("SALUTATION_BY_PLAYER",
                !CharacterObject.OneToOneConversationCharacter.IsFemale
                    ? GameTexts.FindText("str_my_lord")
                    : GameTexts.FindText("str_my_lady"), false);
            if (!TextObject.IsNullOrEmpty(this.FirstName))
                MBTextManager.SetTextVariable("FIRST_NAME", this.FirstName, false);
            else
                MBTextManager.SetTextVariable("FIRST_NAME", this.Name, false);
            MBTextManager.SetTextVariable("GENDER", this.IsFemale ? 1 : 0);
        }

        public void SetPersonalRelation(Hero otherHero, int value)
        {
            value = MBMath.ClampInt(value, -100, 100);
            CharacterRelationManager.SetHeroRelation(this, otherHero, value);
        }

        public int GetRelation(Hero otherHero) => otherHero == this
            ? 0
            : TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetEffectiveRelation(this, otherHero);

        public int GetBaseHeroRelation(Hero otherHero) =>
            TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetBaseRelation(this, otherHero);

        public bool IsEnemy(Hero otherHero) => CharacterRelationManager.GetHeroRelation(this, otherHero) < -10;

        public bool IsFriend(Hero otherHero) => CharacterRelationManager.GetHeroRelation(this, otherHero) > 10;

        public bool IsNeutral(Hero otherHero) => !this.IsFriend(otherHero) && !this.IsEnemy(otherHero);

        public void ModifyHair(int hair, int beard, int tattoo)
        {
            BodyProperties bodyProperties = this.BodyProperties;
            FaceGen.SetHair(ref bodyProperties, hair, beard, tattoo);
            this.StaticBodyProperties = bodyProperties.StaticProperties;
        }

        public void ModifyPlayersFamilyAppearance(StaticBodyProperties staticBodyProperties) =>
            this.StaticBodyProperties = staticBodyProperties;

        public IReadOnlyList<Workshop> OwnedWorkshops => (IReadOnlyList<Workshop>)this._ownedWorkshops.AsReadOnly();

        public void AddOwnedWorkshop(Workshop workshop)
        {
            if (this._ownedWorkshops.Contains(workshop))
                return;
            this._ownedWorkshops.Add(workshop);
        }

        public void RemoveOwnedWorkshop(Workshop workshop)
        {
            if (!this._ownedWorkshops.Contains(workshop))
                return;
            this._ownedWorkshops.Remove(workshop);
        }

        public static Hero FindFirst(Func<Hero, bool> predicate) => TaleWorlds.CampaignSystem.Campaign.Current
            .Characters.FirstOrDefault<CharacterObject>(
                (Func<CharacterObject, bool>)(x => x.IsHero && predicate(x.HeroObject)))?.HeroObject;

        public static IEnumerable<Hero> FindAll(Func<Hero, bool> predicate) => TaleWorlds.CampaignSystem.Campaign
            .Current.Characters
            .Where<CharacterObject>((Func<CharacterObject, bool>)(x => x.IsHero && predicate(x.HeroObject)))
            .Select<CharacterObject, Hero>((Func<CharacterObject, Hero>)(x => x.HeroObject));

        public static MBReadOnlyList<Hero> AllAliveHeroes => TaleWorlds.CampaignSystem.Campaign.Current.AliveHeroes;

        public static MBReadOnlyList<Hero> DeadOrDisabledHeroes =>
            TaleWorlds.CampaignSystem.Campaign.Current.DeadOrDisabledHeroes;

        public static Hero MainHero => CharacterObject.PlayerCharacter.HeroObject;

        public static Hero OneToOneConversationHero =>
            TaleWorlds.CampaignSystem.Campaign.Current.ConversationManager.OneToOneConversationHero;

        public static IEnumerable<Hero> ConversationHeroes =>
            TaleWorlds.CampaignSystem.Campaign.Current.ConversationManager.ConversationHeroes;

        public static bool IsMainHeroIll => TaleWorlds.CampaignSystem.Campaign.Current.MainHeroIllDays != -1;

        public void MakeWounded(
            Hero killerHero = null,
            KillCharacterAction.KillCharacterActionDetail deathMarkDetail =
                KillCharacterAction.KillCharacterActionDetail.None)
        {
            this.DeathMark = deathMarkDetail;
            this.DeathMarkKillerHero = killerHero;
            this.HitPoints = 1;
        }

        public void AddDeathMark(
            Hero killerHero = null,
            KillCharacterAction.KillCharacterActionDetail deathMarkDetail =
                KillCharacterAction.KillCharacterActionDetail.None)
        {
            this.DeathMark = deathMarkDetail;
            this.DeathMarkKillerHero = killerHero;
        }

        internal void OnAddedToParty(TaleWorlds.CampaignSystem.MobileParty mobileParty)
        {
            this.PartyBelongedTo = mobileParty;
            this.StayingInSettlement = (Settlement)null;
        }

        internal void OnRemovedFromParty(TaleWorlds.CampaignSystem.MobileParty mobileParty) =>
            this.PartyBelongedTo = (TaleWorlds.CampaignSystem.MobileParty)null;

        internal void OnAddedToPartyAsPrisoner(PartyBase party)
        {
            this.PartyBelongedToAsPrisoner = party;
            this.PartyBelongedTo = (TaleWorlds.CampaignSystem.MobileParty)null;
        }

        internal void OnRemovedFromPartyAsPrisoner(PartyBase party) => this.PartyBelongedToAsPrisoner = (PartyBase)null;

        TextObject ITrackableBase.GetName() => this.Name;

        public Vec3 GetPosition()
        {
            Vec3 vec3 = Vec3.Zero;
            if (this.CurrentSettlement != null)
                vec3 = this.CurrentSettlement.GetLogicalPosition();
            else if (this.IsPrisoner && this.PartyBelongedToAsPrisoner != null)
                vec3 = this.PartyBelongedToAsPrisoner.IsSettlement
                    ? this.PartyBelongedToAsPrisoner.Settlement.GetLogicalPosition()
                    : this.PartyBelongedToAsPrisoner.MobileParty.GetLogicalPosition();
            else if (this.PartyBelongedTo != null)
                vec3 = this.PartyBelongedTo.GetLogicalPosition();
            return vec3;
        }

        public IMapPoint GetMapPoint()
        {
            if (this.CurrentSettlement != null)
                return (IMapPoint)this.CurrentSettlement;
            if (!this.IsPrisoner || this.PartyBelongedToAsPrisoner == null)
                return (IMapPoint)this.PartyBelongedTo;
            return !this.PartyBelongedToAsPrisoner.IsSettlement
                ? (IMapPoint)this.PartyBelongedToAsPrisoner.MobileParty
                : (IMapPoint)this.PartyBelongedToAsPrisoner.Settlement;
        }

        public float GetTrackDistanceToMainAgent() => this.GetPosition().Distance(Hero.MainHero.GetPosition());

        public bool CheckTracked(BasicCharacterObject basicCharacter) => this.CharacterObject == basicCharacter;

        private void SetInitialValuesFromCharacter(CharacterObject characterObject)
        {
            foreach (SkillObject skill in Skills.All)
                this.SetSkillValueInternal(skill, characterObject.GetSkillValue(skill));
            foreach (TraitObject trait in TraitObject.All)
                this.SetTraitLevelInternal(trait, characterObject.GetTraitLevel(trait));
            this.Level = characterObject.Level;
            this.SetName(characterObject.Name, characterObject.Name);
            this.Culture = characterObject.Culture;
            this.HairTags = characterObject.HairTags;
            this.BeardTags = characterObject.BeardTags;
            this.TattooTags = characterObject.TattooTags;
            this._defaultAge = characterObject.Age;
            this._birthDay = HeroHelper.GetRandomBirthDayForAge(this._defaultAge);
            this.HitPoints = characterObject.MaxHitPoints();
            List<Equipment> list1 = characterObject.AllEquipments
                .Where<Equipment>((Func<Equipment, bool>)(t => !t.IsEmpty() && !t.IsCivilian)).ToList<Equipment>();
            List<Equipment> list2 = characterObject.AllEquipments
                .Where<Equipment>((Func<Equipment, bool>)(t => !t.IsEmpty() && t.IsCivilian)).ToList<Equipment>();
            if (list1.IsEmpty<Equipment>())
                list1 = this.Culture.ChildCharacterTemplates
                    .GetRandomElementWithPredicate<CharacterObject>(
                        (Func<CharacterObject, bool>)(x => x.IsFemale == this.IsFemale)).BattleEquipments
                    .ToList<Equipment>();
            if (list2.IsEmpty<Equipment>())
                list2 = this.Culture.ChildCharacterTemplates
                    .GetRandomElementWithPredicate<CharacterObject>(
                        (Func<CharacterObject, bool>)(x => x.IsFemale == this.IsFemale)).CivilianEquipments
                    .ToList<Equipment>();
            if (!list1.IsEmpty<Equipment>() && !list2.IsEmpty<Equipment>())
            {
                Equipment equipment1 = list1[this.RandomValueDeterministic % list1.Count];
                Equipment equipment2 = list2[this.RandomValueDeterministic % list2.Count];
                this.BattleEquipment = equipment1.Clone();
                this.CivilianEquipment = equipment2.Clone();
            }

            this.IsFemale = characterObject.IsFemale;
            this.Occupation = this.CharacterObject.GetDefaultOccupation();
        }

        public void ResetEquipments()
        {
            this.BattleEquipment = this.Template.FirstBattleEquipment.Clone();
            this.CivilianEquipment = this.Template.FirstCivilianEquipment.Clone();
        }

        public void ChangeHeroGold(int changeAmount) => this.Gold =
            changeAmount <= int.MaxValue - this._gold ? this._gold + changeAmount : int.MaxValue;

        public void CheckInvalidEquipmentsAndReplaceIfNeeded()
        {
            for (int index = 0; index < 12; ++index)
            {
                EquipmentElement equipmentElement = this.BattleEquipment[index];
                if (equipmentElement.Item == DefaultItems.Trash)
                {
                    this.HandleInvalidItem(true, index);
                }
                else
                {
                    equipmentElement = this.BattleEquipment[index];
                    if (equipmentElement.Item != null)
                    {
                        equipmentElement = this.BattleEquipment[index];
                        if (!equipmentElement.Item.IsReady)
                        {
                            MBObjectManager instance1 = MBObjectManager.Instance;
                            equipmentElement = this.BattleEquipment[index];
                            MBGUID id = equipmentElement.Item.Id;
                            MBObjectBase mbObjectBase = instance1.GetObject(id);
                            equipmentElement = this.BattleEquipment[index];
                            ItemObject itemObject1 = equipmentElement.Item;
                            if (mbObjectBase == itemObject1)
                            {
                                MBObjectManager instance2 = MBObjectManager.Instance;
                                equipmentElement = this.BattleEquipment[index];
                                ItemObject itemObject2 = equipmentElement.Item;
                                instance2.UnregisterObject((MBObjectBase)itemObject2);
                            }

                            this.HandleInvalidItem(true, index);
                            this.PartyBelongedTo?.ItemRoster.AddToCounts(DefaultItems.Trash, 1);
                        }

                        equipmentElement = this.BattleEquipment[index];
                        ItemModifier itemModifier = equipmentElement.ItemModifier;
                        if ((itemModifier != null ? (!itemModifier.IsReady ? 1 : 0) : 0) != 0)
                            this.HandleInvalidModifier(true, index);
                    }
                }

                equipmentElement = this.CivilianEquipment[index];
                if (equipmentElement.Item == DefaultItems.Trash)
                {
                    this.HandleInvalidItem(false, index);
                }
                else
                {
                    equipmentElement = this.CivilianEquipment[index];
                    if (equipmentElement.Item != null)
                    {
                        equipmentElement = this.CivilianEquipment[index];
                        if (!equipmentElement.Item.IsReady)
                        {
                            MBObjectManager instance3 = MBObjectManager.Instance;
                            equipmentElement = this.CivilianEquipment[index];
                            MBGUID id = equipmentElement.Item.Id;
                            MBObjectBase mbObjectBase = instance3.GetObject(id);
                            equipmentElement = this.CivilianEquipment[index];
                            ItemObject itemObject3 = equipmentElement.Item;
                            if (mbObjectBase == itemObject3)
                            {
                                MBObjectManager instance4 = MBObjectManager.Instance;
                                equipmentElement = this.CivilianEquipment[index];
                                ItemObject itemObject4 = equipmentElement.Item;
                                instance4.UnregisterObject((MBObjectBase)itemObject4);
                            }

                            this.HandleInvalidItem(false, index);
                            this.PartyBelongedTo?.ItemRoster.AddToCounts(DefaultItems.Trash, 1);
                        }

                        equipmentElement = this.CivilianEquipment[index];
                        ItemModifier itemModifier = equipmentElement.ItemModifier;
                        if ((itemModifier != null ? (!itemModifier.IsReady ? 1 : 0) : 0) != 0)
                            this.HandleInvalidModifier(false, index);
                    }
                }
            }
        }

        private void HandleInvalidItem(bool isBattleEquipment, int i)
        {
            if (this.IsHumanPlayerCharacter)
            {
                if (isBattleEquipment)
                    this.BattleEquipment[i] = EquipmentElement.Invalid;
                else
                    this.CivilianEquipment[i] = EquipmentElement.Invalid;
            }
            else
            {
                List<Equipment> equipmentList = isBattleEquipment
                    ? this.CharacterObject.BattleEquipments.Where<Equipment>((Func<Equipment, bool>)(t => !t.IsEmpty()))
                        .ToList<Equipment>()
                    : this.CharacterObject.CivilianEquipments
                        .Where<Equipment>((Func<Equipment, bool>)(t => !t.IsEmpty())).ToList<Equipment>();
                EquipmentElement invalid = equipmentList[this.RandomValueDeterministic % equipmentList.Count][i];
                if (invalid.IsEmpty || !invalid.Item.IsReady)
                    invalid = EquipmentElement.Invalid;
                if (!isBattleEquipment)
                {
                    EquipmentElement equipmentElement1 = this.CivilianEquipment[i];
                }
                else
                {
                    EquipmentElement equipmentElement2 = this.BattleEquipment[i];
                }

                if (isBattleEquipment)
                    this.BattleEquipment[i] = invalid;
                else
                    this.CivilianEquipment[i] = invalid;
            }
        }

        private void HandleInvalidModifier(bool isBattleEquipment, int i)
        {
            if (isBattleEquipment)
                this.BattleEquipment[i] = new EquipmentElement(this.BattleEquipment[i].Item);
            else
                this.CivilianEquipment[i] = new EquipmentElement(this.CivilianEquipment[i].Item);
        }

        public struct HeroLastSeenInformation
        {
            //[SaveableField(104)]
            public Settlement LastSeenPlace;

            //[SaveableField(105)]
            public TaleWorlds.CampaignSystem.CampaignTime LastSeenDate;

            //[SaveableField(106)]
            public bool IsNearbySettlement;

            public static void AutoGeneratedStaticCollectObjectsHeroLastSeenInformation(
                object o,
                List<object> collectedObjects)
            {
                ((Hero.HeroLastSeenInformation)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
            }

            private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
            {
                collectedObjects.Add((object)this.LastSeenPlace);
                TaleWorlds.CampaignSystem.CampaignTime.AutoGeneratedStaticCollectObjectsCampaignTime(
                    (object)this.LastSeenDate, collectedObjects);
            }

            internal static object AutoGeneratedGetMemberValueLastSeenPlace(object o) =>
                (object)((Hero.HeroLastSeenInformation)o).LastSeenPlace;

            internal static object AutoGeneratedGetMemberValueLastSeenDate(object o) =>
                (object)((Hero.HeroLastSeenInformation)o).LastSeenDate;

            internal static object AutoGeneratedGetMemberValueIsNearbySettlement(object o) =>
                (object)((Hero.HeroLastSeenInformation)o).IsNearbySettlement;
        }

        public enum CharacterStates
        {
            NotSpawned,
            Active,
            Fugitive,
            Prisoner,
            Released,
            Dead,
            Disabled,
        }

        public enum FactionRank
        {
            None = -1, // 0xFFFFFFFF
            Vassal = 0,
            Leader = 1,
        }
    }
}