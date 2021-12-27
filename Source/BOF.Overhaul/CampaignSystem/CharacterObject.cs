// Decompiled with JetBrains decompiler
// Type: TaleWorlds.CampaignSystem.CharacterObject
// Assembly: TaleWorlds.CampaignSystem, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B2BAE1FB-C553-469E-8537-7AB53654D4A1
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem.CampaignSystem
{
    public class CharacterObject : Core.BasicCharacterObject, ICharacterData
    {
        private CharacterRestrictionFlags _characterRestrictionFlags;

        //[SaveableField(101)]
        private Hero _heroObject;

        private string _originCharacterStringId;

        //[SaveableField(103)]
        private CharacterObject _originCharacter;
        private TraitObject _persona;
        private CharacterTraits _characterTraits;
        private CharacterObject _civilianEquipmentTemplate;
        private CharacterObject _battleEquipmentTemplate;
        private Occupation _occupation;
        public const int MaxCharacterTier = 6;

        public override TextObject Name => this.IsHero ? this.HeroObject.Name : base.Name;

        public string EncyclopediaLink => !this.IsHero
            ? TaleWorlds.CampaignSystem.Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(CharacterObject)) +
              "-" + this.StringId
            : this._heroObject.EncyclopediaLink;

        public TextObject EncyclopediaLinkWithName
        {
            get
            {
                if (this.IsHero)
                    return this._heroObject.EncyclopediaLinkWithName;
                return TaleWorlds.CampaignSystem.Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject))
                    .IsValidEncyclopediaItem((object)this)
                    ? HyperlinkTexts.GetUnitHyperlinkText(this.EncyclopediaLink, this.Name)
                    : this.Name;
            }
        }

        public bool HiddenInEncylopedia { get; private set; }

        public override string ToString() => this.Name.ToString();

        public bool IsNotTransferableInPartyScreen =>
            (this._characterRestrictionFlags & CharacterRestrictionFlags.NotTransferableInPartyScreen) ==
            CharacterRestrictionFlags.NotTransferableInPartyScreen;

        public bool IsNotTransferableInHideouts =>
            (this._characterRestrictionFlags & CharacterRestrictionFlags.CanNotGoInHideout) ==
            CharacterRestrictionFlags.CanNotGoInHideout;

        public bool IsOriginalCharacter => this._originCharacter == null && this._originCharacterStringId == null;

        public Hero HeroObject
        {
            get => this._heroObject;
            internal set => this._heroObject = value;
        }

        public override MBReadOnlyList<Equipment> AllEquipments
        {
            get
            {
                if (!this.IsHero)
                    return base.AllEquipments;
                return new List<Equipment>()
                {
                    this.HeroObject.BattleEquipment,
                    this.HeroObject.CivilianEquipment
                }.GetReadOnlyList<Equipment>();
            }
        }

        public override Equipment Equipment => this.IsHero ? this.HeroObject.BattleEquipment : base.Equipment;

        public IEnumerable<Equipment> BattleEquipments
        {
            get
            {
                if (!this.IsHero)
                    return this.AllEquipments.WhereQ<Equipment>((Func<Equipment, bool>)(e => !e.IsCivilian));
                return new List<Equipment>()
                {
                    this.HeroObject.BattleEquipment
                }.AsEnumerable<Equipment>();
            }
        }

        public IEnumerable<Equipment> CivilianEquipments
        {
            get
            {
                if (!this.IsHero)
                    return this.AllEquipments.WhereQ<Equipment>((Func<Equipment, bool>)(e => e.IsCivilian));
                return new List<Equipment>()
                {
                    this.HeroObject.CivilianEquipment
                }.AsEnumerable<Equipment>();
            }
        }

        public Equipment FirstBattleEquipment => this.IsHero
            ? this.HeroObject.BattleEquipment
            : this.AllEquipments.FirstOrDefaultQ<Equipment>((Func<Equipment, bool>)(e => !e.IsCivilian));

        public Equipment FirstCivilianEquipment => this.IsHero
            ? this.HeroObject.CivilianEquipment
            : this.AllEquipments.FirstOrDefaultQ<Equipment>((Func<Equipment, bool>)(e => e.IsCivilian));

        public Equipment RandomBattleEquipment => this.IsHero
            ? this.HeroObject.BattleEquipment
            : this.AllEquipments.GetRandomElementWithPredicate<Equipment>((Func<Equipment, bool>)(e => !e.IsCivilian));

        public Equipment RandomCivilianEquipment => this.IsHero
            ? this.HeroObject.CivilianEquipment
            : this.AllEquipments.GetRandomElementWithPredicate<Equipment>((Func<Equipment, bool>)(e => e.IsCivilian));

        public override int HitPoints => this.IsHero ? this.HeroObject.HitPoints : this.MaxHitPoints();

        public override string HairTags => this.IsHero ? this.HeroObject.HairTags : base.HairTags;

        public override string BeardTags => this.IsHero ? this.HeroObject.BeardTags : base.BeardTags;

        public override string TattooTags => this.IsHero ? this.HeroObject.TattooTags : base.TattooTags;

        public override int MaxHitPoints() =>
            MathF.Round(BOFCampaign.Current.Models.CharacterStatsModel.MaxHitpoints(this).ResultNumber);

        public ExplainedNumber MaxHitPointsExplanation =>
            BOFCampaign.Current.Models.CharacterStatsModel.MaxHitpoints(this, true);

        public override int Level => !this.IsHero ? base.Level : this.HeroObject.Level;

        public CharacterObject() => this.Init();

        // [LoadInitializationCallback]
        // private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
        // {
        // this.Init();
        // if (this._originCharacter != null)
        // return;
        // string dataValueBySaveId = objectLoadData.GetDataValueBySaveId(102) as string;
        // if (!(dataValueBySaveId != this.StringId))
        // return;
        // this._originCharacterStringId = dataValueBySaveId;
        // }

        private void Init()
        {
            this._occupation = Occupation.NotAssigned;
            this._characterTraits = new CharacterTraits();
            this.Level = 1;
            this._characterRestrictionFlags = CharacterRestrictionFlags.None;
        }

        public static CharacterObject CreateFrom(
            CharacterObject character,
            bool ignoreSkillsAndTraits)
        {
            CharacterObject characterObject = MBObjectManager.Instance.CreateObject<CharacterObject>();
            characterObject._originCharacter = character._originCharacter ?? character;
            characterObject.Culture = character.Culture;
            characterObject.DefaultFormationClass = character.DefaultFormationClass;
            characterObject.DefaultFormationGroup = character.DefaultFormationGroup;
            characterObject.BodyPropertyRange = character.BodyPropertyRange;
            if (characterObject.IsHero)
                characterObject.HeroObject.StaticBodyProperties = character.IsHero
                    ? character.HeroObject.StaticBodyProperties
                    : character.GetBodyPropertiesMin(false).StaticProperties;
            characterObject.FormationPositionPreference = character.FormationPositionPreference;
            characterObject.IsFemale = character.IsFemale;
            characterObject.Level = character.Level;
            characterObject._basicName = character.Name;
            characterObject._occupation = character._occupation;
            characterObject._persona = character._persona;
            characterObject.Age = character.Age;
            if (ignoreSkillsAndTraits)
            {
                characterObject.CharacterSkills =
                    MBObjectManager.Instance.CreateObject<MBCharacterSkills>(characterObject.StringId);
                characterObject._characterTraits = new CharacterTraits();
            }
            else
            {
                characterObject.CharacterSkills = character.CharacterSkills;
                characterObject._characterTraits = new CharacterTraits(character._characterTraits);
            }

            characterObject.HairTags = character.HairTags;
            characterObject.BeardTags = character.BeardTags;
            characterObject._civilianEquipmentTemplate = character._civilianEquipmentTemplate;
            characterObject._battleEquipmentTemplate = character._battleEquipmentTemplate;
            characterObject.InitializeEquipmentsOnLoad((BasicCharacterObject)character);
            return characterObject;
        }

        public static CharacterObject PlayerCharacter => Core.Game.Current.PlayerTroop as CharacterObject;

        public static CharacterObject OneToOneConversationCharacter =>
            BOFCampaign.Current.ConversationManager.OneToOneConversationCharacter;

        public static IEnumerable<CharacterObject> ConversationCharacters =>
            BOFCampaign.Current.ConversationManager.ConversationCharacters;

        public override void AfterRegister()
        {
            base.AfterRegister();
            if (this.Equipment != null)
                this.Equipment.SyncEquipments = true;
            if (this.FirstCivilianEquipment == null)
                return;
            this.FirstCivilianEquipment.SyncEquipments = true;
        }

        public CultureObject Culture
        {
            get => this.IsHero ? this.HeroObject.Culture : (CultureObject)base.Culture;
            set
            {
                if (this.IsHero)
                    this.HeroObject.Culture = value;
                else
                    this.Culture = (BasicCultureObject)value;
            }
        }

        public override BodyProperties GetBodyPropertiesMin(bool returnBaseValue = false) =>
            this.IsHero && !returnBaseValue ? this.HeroObject.BodyProperties : base.GetBodyPropertiesMin();

        public override BodyProperties GetBodyPropertiesMax() =>
            this.IsHero ? this.HeroObject.BodyProperties : base.GetBodyPropertiesMax();

        public override bool IsFemale => this.IsHero ? this.HeroObject.IsFemale : base.IsFemale;

        public override void UpdatePlayerCharacterBodyProperties(
            BodyProperties properties,
            bool isFemale)
        {
            if (!this.IsPlayerCharacter || !this.IsHero)
                return;
            this.HeroObject.StaticBodyProperties = properties.StaticProperties;
            this.HeroObject.Weight = properties.Weight;
            this.HeroObject.Build = properties.Build;
            this.HeroObject.UpdatePlayerGender(isFemale);
            CampaignEventDispatcher.Instance.OnPlayerBodyPropertiesChanged();
        }

        public bool IsBasicTroop { get; set; }

        public bool IsTemplate { get; private set; }

        public bool IsChildTemplate { get; private set; }

        public override bool IsPlayerCharacter => CharacterObject.PlayerCharacter == this;

        public override bool IsHero => this._heroObject != null;

        public bool IsRegular => this._heroObject == null;

        public Occupation Occupation => this.IsHero ? this.HeroObject.Occupation : this._occupation;

        public Occupation GetDefaultOccupation() => this._occupation;

        public override float Age
        {
            get
            {
                TaleWorlds.CampaignSystem.Hero heroObject = this.HeroObject;
                return heroObject == null ? base.Age : heroObject.Age;
            }
        }

        public int ConformityNeededToRecruitPrisoner => BOFCampaign.Current.Models.PrisonerRecruitmentCalculationModel
            .GetConformityNeededToRecruitPrisoner(this);

        public CharacterObject[] UpgradeTargets { get; private set; } = new CharacterObject[0];

        public ItemCategory UpgradeRequiresItemFromCategory { get; private set; }

        public bool HasThrowingWeapon()
        {
            for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot;
                index < EquipmentIndex.NumAllWeaponSlots;
                ++index)
            {
                ItemObject itemObject = this.Equipment[index].Item;
                if (itemObject != null && itemObject.Type == ItemObject.ItemTypeEnum.Thrown)
                    return true;
            }

            return false;
        }

        public int GetUpgradeXpCost(PartyBase party, int index)
        {
            CharacterObject upgradeTarget = (CharacterObject)null;
            if (index >= 0 && index < this.UpgradeTargets.Length)
                upgradeTarget = this.UpgradeTargets[index];
            return BOFCampaign.Current.Models.PartyTroopUpgradeModel.GetXpCostForUpgrade(party, this, upgradeTarget);
        }

        public int GetUpgradeGoldCost(PartyBase party, int index) =>
            BOFCampaign.Current.Models.PartyTroopUpgradeModel.GetGoldCostForUpgrade(party, this,
                this.UpgradeTargets[index]);

        public void InitializeHeroCharacterOnAfterLoad()
        {
            if (this._originCharacter == null)
            {
                if (this._originCharacterStringId == null)
                    return;
                this._originCharacter =
                    MBObjectManager.Instance.GetObject<CharacterObject>(this._originCharacterStringId);
            }

            this.InitializeHeroBasicCharacterOnAfterLoad((BasicCharacterObject)this._originCharacter);
            this._occupation = this._originCharacter._occupation;
            this.IsChildTemplate = this._originCharacter.IsChildTemplate;
            this._basicName = this._originCharacter._basicName;
            this.UpgradeTargets = this._originCharacter.UpgradeTargets;
            this.IsBasicTroop = this._originCharacter.IsBasicTroop;
            this.UpgradeRequiresItemFromCategory = this._originCharacter.UpgradeRequiresItemFromCategory;
            this._civilianEquipmentTemplate = this._originCharacter._civilianEquipmentTemplate;
            this._battleEquipmentTemplate = this._originCharacter._battleEquipmentTemplate;
            this._persona = this._originCharacter._persona;
            this.IsReady = true;
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            XmlNode attribute1 = (XmlNode)node.Attributes["occupation"];
            if (attribute1 != null)
                this._occupation = (Occupation)Enum.Parse(typeof(Occupation), attribute1.InnerText);
            XmlNode attribute2 = (XmlNode)node.Attributes["is_template"];
            this.IsTemplate = attribute2 != null && Convert.ToBoolean(attribute2.InnerText);
            XmlNode attribute3 = (XmlNode)node.Attributes["is_child_template"];
            this.IsChildTemplate = attribute3 != null && Convert.ToBoolean(attribute3.InnerText);
            XmlNode attribute4 = (XmlNode)node.Attributes["is_hidden_encyclopedia"];
            this.HiddenInEncylopedia = attribute4 != null && Convert.ToBoolean(attribute4.InnerText);
            List<CharacterObject> characterObjectList = new List<CharacterObject>();
            foreach (XmlNode childNode1 in node.ChildNodes)
            {
                if (childNode1.Name == "Traits")
                    this._characterTraits.Deserialize(objectManager, childNode1);
                else if (childNode1.Name == "upgrade_targets")
                {
                    foreach (XmlNode childNode2 in childNode1.ChildNodes)
                    {
                        if (childNode2.Name == "upgrade_target")
                        {
                            CharacterObject characterObject =
                                objectManager.ReadObjectReferenceFromXml("id", typeof(CharacterObject), childNode2) as
                                    CharacterObject;
                            characterObjectList.Add(characterObject);
                        }
                    }
                }
            }

            this.UpgradeTargets = characterObjectList.ToArray();
            XmlNode attribute5 = (XmlNode)node.Attributes["voice"];
            if (attribute5 != null)
                this._persona = MBObjectManager.Instance.GetObject<TraitObject>(attribute5.Value);
            XmlNode attribute6 = (XmlNode)node.Attributes["is_basic_troop"];
            this.IsBasicTroop = attribute6 != null && Convert.ToBoolean(attribute6.InnerText);
            this.UpgradeRequiresItemFromCategory =
                objectManager.ReadObjectReferenceFromXml<ItemCategory>("upgrade_requires", node);
            XmlNode attribute7 = (XmlNode)node.Attributes["level"];
            this.Level = attribute7 != null ? Convert.ToInt32(attribute7.InnerText) : 1;
            if (node.Attributes["civilianTemplate"] != null)
                this._civilianEquipmentTemplate =
                    objectManager.ReadObjectReferenceFromXml("civilianTemplate", typeof(CharacterObject), node) as
                        CharacterObject;
            if (node.Attributes["battleTemplate"] != null)
                this._battleEquipmentTemplate =
                    objectManager.ReadObjectReferenceFromXml("battleTemplate", typeof(CharacterObject), node) as
                        CharacterObject;
            this._originCharacter = (CharacterObject)null;
        }

        public override float GetPower() =>
            CharacterObject.GetPowerImp(this.IsHero ? this.HeroObject.Level / 4 + 1 : this.Tier, this.IsHero,
                this.IsMounted);

        public override float GetBattlePower() =>
            MathF.Max((float)(1.0 + 0.5 * ((double)this.GetPower() - (double)CharacterObject.GetPowerImp(0))), 1f);

        public override float GetMoraleResistance()
        {
            int num = this.IsHero ? this.HeroObject.Level / 4 + 1 : this.Tier;
            return (float)((this.IsHero ? 1.5 : 1.0) * (0.5 * (double)num + 1.0));
        }

        public void GetSimulationAttackPower(
            out float attackPoints,
            out float defencePoints,
            Equipment equipment = null)
        {
            if (equipment == null)
                equipment = this.Equipment;
            attackPoints = 0.0f;
            defencePoints = 0.0f;
            float a = 0.0f;
            float num1 = 0.0f;
            float num2 = equipment.GetArmArmorSum() + equipment.GetHeadArmorSum() + equipment.GetHumanBodyArmorSum() +
                         equipment.GetLegArmorSum();
            float num3 = num2 * num2 / equipment.GetTotalWeightOfArmor(true);
            defencePoints += (float)((double)num3 * 10.0 + 4000.0);
            for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot;
                index < EquipmentIndex.NumAllWeaponSlots;
                ++index)
            {
                EquipmentElement equipmentElement1 = equipment[index];
                if (!equipmentElement1.IsEmpty)
                {
                    float num4 = equipmentElement1.Item.RelevantSkill == null
                        ? 1f
                        : (float)(0.300000011920929 + (double)this.GetSkillValue(equipmentElement1.Item.RelevantSkill) /
                            300.0 * 0.699999988079071);
                    float b = num4 * equipmentElement1.Item.Effectiveness;
                    if (equipmentElement1.Item.PrimaryWeapon.IsRangedWeapon)
                    {
                        for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                            equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                            ++equipmentIndex)
                        {
                            EquipmentElement equipmentElement2 = equipment[index];
                            if (index != equipmentIndex && !equipmentElement2.IsEmpty &&
                                equipmentElement2.Item.PrimaryWeapon.IsAmmo)
                            {
                                b += num4 * equipmentElement2.Item.Effectiveness;
                                break;
                            }
                        }
                    }

                    if (equipmentElement1.Item.PrimaryWeapon.IsShield)
                        defencePoints += b * 10f;
                    else
                        a = MathF.Max(a, b);
                }
            }

            attackPoints += a;
            for (EquipmentIndex index = EquipmentIndex.ArmorItemEndSlot; index <= EquipmentIndex.HorseHarness; ++index)
            {
                EquipmentElement equipmentElement = equipment[index];
                if (!equipmentElement.IsEmpty)
                    num1 += equipmentElement.Item.Effectiveness;
            }

            float num5 = equipment.Horse.Item == null || equipment.Horse.Item.RelevantSkill == null
                ? 1f
                : (float)(0.300000011920929 + (double)this.GetSkillValue(equipment.Horse.Item.RelevantSkill) / 300.0 *
                    0.699999988079071);
            float num6 = num1 * num5;
            attackPoints += num6 * 2.5f;
            defencePoints += num6 * 5f;
        }

        public override bool IsMounted => this.IsHero ? this.Equipment[10].Item != null : base.IsMounted;

        public override bool IsRanged
        {
            get
            {
                if (this.IsHero)
                {
                    for (int index = 0; index < 4; ++index)
                    {
                        ItemObject itemObject = this.Equipment[index].Item;
                        if (itemObject != null && (itemObject.ItemType == ItemObject.ItemTypeEnum.Bow ||
                                                   itemObject.ItemType == ItemObject.ItemTypeEnum.Crossbow))
                            return true;
                    }
                }

                return base.IsRanged;
            }
        }

        public float GetHeadArmorSum(bool civilianEquipment = false) => !civilianEquipment
            ? this.FirstBattleEquipment.GetHeadArmorSum()
            : this.FirstCivilianEquipment.GetHeadArmorSum();

        public float GetBodyArmorSum(bool civilianEquipment = false) => !civilianEquipment
            ? this.FirstBattleEquipment.GetHumanBodyArmorSum()
            : this.FirstCivilianEquipment.GetHumanBodyArmorSum();

        public float GetLegArmorSum(bool civilianEquipment = false) => !civilianEquipment
            ? this.FirstBattleEquipment.GetLegArmorSum()
            : this.FirstCivilianEquipment.GetLegArmorSum();

        public float GetArmArmorSum(bool civilianEquipment = false) => !civilianEquipment
            ? this.FirstBattleEquipment.GetArmArmorSum()
            : this.FirstCivilianEquipment.GetArmArmorSum();

        public float GetHorseArmorSum(bool civilianEquipment = false) => !civilianEquipment
            ? this.FirstBattleEquipment.GetHorseArmorSum()
            : this.FirstCivilianEquipment.GetHorseArmorSum();

        public override BodyProperties GetBodyProperties(Equipment equipment, int seed = -1)
        {
            if (this.IsHero)
                return this.HeroObject.BodyProperties;
            switch (seed)
            {
                case -2:
                    return this.GetBodyPropertiesMin(false);
                case -1:
                    seed = this.StringId.GetDeterministicHashCode();
                    break;
            }

            return FaceGen.GetRandomBodyProperties(this.IsFemale, this.GetBodyPropertiesMin(false),
                this.GetBodyPropertiesMax(), equipment != null ? (int)equipment.HairCoverType : 0, seed, this.HairTags,
                this.BeardTags, this.TattooTags);
        }

        public int TroopWage => this.IsHero
            ? 2 + this.Level * 2
            : BOFCampaign.Current.Models.PartyWageModel.GetCharacterWage(this.Tier);

        public void SetTransferableInPartyScreen(bool isTransferable)
        {
            if (isTransferable)
                this._characterRestrictionFlags &= ~CharacterRestrictionFlags.NotTransferableInPartyScreen;
            else
                this._characterRestrictionFlags |= CharacterRestrictionFlags.NotTransferableInPartyScreen;
        }

        public void SetTransferableInHideouts(bool isTransferable)
        {
            if (isTransferable)
                this._characterRestrictionFlags &= ~CharacterRestrictionFlags.CanNotGoInHideout;
            else
                this._characterRestrictionFlags |= CharacterRestrictionFlags.CanNotGoInHideout;
        }

        public int Tier => CharacterHelper.GetCharacterTier(this);

        public void ClearAttributes()
        {
            if (!this.IsHero)
                return;
            this.HeroObject.ClearAttributes();
        }

        public int GetTraitLevel(TraitObject trait) => this.IsHero
            ? this.HeroObject.GetTraitLevel(trait)
            : this._characterTraits.GetPropertyValue(trait);

        public bool GetPerkValue(PerkObject perk) => this.IsHero && this.HeroObject.GetPerkValue(perk);

        public override int GetSkillValue(SkillObject skill) =>
            this.IsHero ? this.HeroObject.GetSkillValue(skill) : base.GetSkillValue(skill);

        public TraitObject GetPersona() => this._persona == null ? DefaultTraits.PersonaSoftspoken : this._persona;

        public override int GetMountKeySeed() => !this.IsHero ? MBRandom.RandomInt() : this.HeroObject.RandomValue;

        public override FormationClass GetFormationClass(IBattleCombatant owner)
        {
            if (!this.IsHero || this.Equipment == null)
                return base.GetFormationClass(owner);
            ItemObject itemObject = this.Equipment[EquipmentIndex.ArmorItemEndSlot].Item;
            int num = itemObject == null ? 0 : (itemObject.HasHorseComponent ? 1 : 0);
            bool flag = this.Equipment.HasWeaponOfClass(WeaponClass.Bow) ||
                        this.Equipment.HasWeaponOfClass(WeaponClass.Crossbow);
            return num == 0
                ? (!flag ? FormationClass.Infantry : FormationClass.Ranged)
                : (!flag ? FormationClass.Cavalry : FormationClass.HorseArcher);
        }

        public static CharacterObject Find(string idString) =>
            MBObjectManager.Instance.GetObject<CharacterObject>(idString);

        public static CharacterObject FindFirst(Predicate<CharacterObject> predicate) =>
            CharacterObject.All.FirstOrDefault<CharacterObject>((Func<CharacterObject, bool>)(x => predicate(x)));

        public static IEnumerable<CharacterObject> FindAll(
            Predicate<CharacterObject> predicate)
        {
            return CharacterObject.All.Where<CharacterObject>((Func<CharacterObject, bool>)(x => predicate(x)));
        }

        public static MBReadOnlyList<CharacterObject> All => BOFCampaign.Current.Characters;

        private static float GetPowerImp(int tier, bool isHero = false, bool isMounted = false) =>
            (float)((double)((2 + tier) * (8 + tier)) * 0.0199999995529652 *
                    (isHero ? 1.5 : (isMounted ? 1.20000004768372 : 1.0)));
    }
}