using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem.Core
{
  public sealed class CultureObject : BasicCultureObject
  {
    private List<TextObject> _maleNameList;
    private List<TextObject> _femaleNameList;
    private List<TextObject> _clanNameList;
    private List<FeatObject> _cultureFeats;
    private List<PolicyObject> _defaultPolicyList;

    public CultureTrait[] Traits { get; private set; }

    public bool HasTrait(CultureTrait trait) => ((IEnumerable<CultureTrait>) this.Traits).Contains<CultureTrait>(trait);

    public bool HasFeat(FeatObject feat) => this._cultureFeats.Contains(feat);

    public IEnumerable<FeatObject> GetCulturalFeats(
      Func<FeatObject, bool> predicate = null)
    {
      foreach (FeatObject cultureFeat in this._cultureFeats)
      {
        if (predicate == null || predicate(cultureFeat))
          yield return cultureFeat;
      }
    }

    public string BodyPropertiesValue { get; private set; }

    public CharacterObject BasicTroop { get; private set; }

    public CharacterObject EliteBasicTroop { get; private set; }

    public CharacterObject MeleeMilitiaTroop { get; private set; }

    public CharacterObject MeleeEliteMilitiaTroop { get; private set; }

    public CharacterObject RangedEliteMilitiaTroop { get; private set; }

    public CharacterObject RangedMilitiaTroop { get; private set; }

    public CharacterObject TournamentMaster { get; private set; }

    public CharacterObject Villager { get; private set; }

    public CharacterObject CaravanMaster { get; private set; }

    public CharacterObject ArmedTrader { get; private set; }

    public CharacterObject CaravanGuard { get; private set; }

    public CharacterObject BasicMercenaryTroop { get; private set; }

    public CharacterObject DuelPreset { get; private set; }

    public CharacterObject PrisonGuard { get; private set; }

    public CharacterObject Guard { get; private set; }

    public CharacterObject Steward { get; private set; }

    public CharacterObject Blacksmith { get; private set; }

    public CharacterObject Weaponsmith { get; private set; }

    public CharacterObject Townswoman { get; private set; }

    public CharacterObject TownswomanInfant { get; private set; }

    public CharacterObject TownswomanChild { get; private set; }

    public CharacterObject TownswomanTeenager { get; private set; }

    public CharacterObject VillageWoman { get; private set; }

    public CharacterObject VillagerMaleChild { get; private set; }

    public CharacterObject VillagerMaleTeenager { get; private set; }

    public CharacterObject VillagerFemaleChild { get; private set; }

    public CharacterObject VillagerFemaleTeenager { get; private set; }

    public CharacterObject Townsman { get; private set; }

    public CharacterObject TownsmanInfant { get; private set; }

    public CharacterObject TownsmanChild { get; private set; }

    public CharacterObject TownsmanTeenager { get; private set; }

    public CharacterObject RansomBroker { get; private set; }

    public CharacterObject GangleaderBodyguard { get; private set; }

    public CharacterObject MerchantNotary { get; private set; }

    public CharacterObject ArtisanNotary { get; private set; }

    public CharacterObject PreacherNotary { get; private set; }

    public CharacterObject RuralNotableNotary { get; private set; }

    public CharacterObject ShopWorker { get; private set; }

    public CharacterObject Tavernkeeper { get; private set; }

    public CharacterObject TavernGamehost { get; private set; }

    public CharacterObject Musician { get; private set; }

    public CharacterObject TavernWench { get; private set; }

    public CharacterObject Armorer { get; private set; }

    public CharacterObject HorseMerchant { get; private set; }

    public CharacterObject Barber { get; private set; }

    public CharacterObject Merchant { get; private set; }

    public CharacterObject Beggar { get; private set; }

    public CharacterObject FemaleBeggar { get; private set; }

    public CharacterObject FemaleDancer { get; private set; }

    public CharacterObject MilitiaArcher { get; private set; }

    public CharacterObject MilitiaVeteranArcher { get; private set; }

    public CharacterObject MilitiaSpearman { get; private set; }

    public CharacterObject MilitiaVeteranSpearman { get; private set; }

    public CharacterObject GearPracticeDummy { get; private set; }

    public CharacterObject WeaponPracticeStage1 { get; private set; }

    public CharacterObject WeaponPracticeStage2 { get; private set; }

    public CharacterObject WeaponPracticeStage3 { get; private set; }

    public CharacterObject GearDummy { get; private set; }

    public CharacterObject BanditChief { get; private set; }

    public CharacterObject BanditRaider { get; private set; }

    public CharacterObject BanditBandit { get; private set; }

    public CharacterObject BanditBoss { get; private set; }

    public TextObject EncyclopediaText { get; private set; }

    public PartyTemplateObject DefaultPartyTemplate { get; private set; }

    public PartyTemplateObject VillagerPartyTemplate { get; private set; }

    public PartyTemplateObject MilitiaPartyTemplate { get; private set; }

    public PartyTemplateObject RebelsPartyTemplate { get; private set; }

    public PartyTemplateObject CaravanPartyTemplate { get; private set; }

    public PartyTemplateObject EliteCaravanPartyTemplate { get; private set; }

    public PartyTemplateObject BanditBossPartyTemplate { get; private set; }

    public PartyTemplateObject VassalRewardTroopsPartyTemplate { get; private set; }

    public IReadOnlyList<ItemObject> VassalRewardItems { get; private set; }

    public IReadOnlyList<TextObject> MaleNameList { get; private set; }

    public IReadOnlyList<TextObject> FemaleNameList { get; private set; }

    public IReadOnlyList<TextObject> ClanNameList { get; private set; }

    public IReadOnlyList<FeatObject> CultureFeats { get; private set; }

    public IReadOnlyList<PolicyObject> DefaultPolicyList { get; private set; }

    public IReadOnlyList<int> PossibleClanBannerIconsIDs { get; private set; }

    public IReadOnlyList<CharacterObject> ChildCharacterTemplates { get; private set; }

    public IReadOnlyList<CharacterObject> NotableAndWandererTemplates { get; private set; }

    public IReadOnlyList<CharacterObject> RebelliousHeroTemplates { get; private set; }

    public IReadOnlyList<CharacterObject> LordTemplates { get; private set; }

    public IReadOnlyList<CharacterObject> TournamentTeamTemplatesForOneParticipant { get; private set; }

    public IReadOnlyList<CharacterObject> TournamentTeamTemplatesForTwoParticipant { get; private set; }

    public IReadOnlyList<CharacterObject> TournamentTeamTemplatesForFourParticipant { get; private set; }

    public int TownEdgeNumber { get; set; }

    public int MilitiaBonus { get; set; }

    public int ProsperityBonus { get; set; }

    public CultureObject.BoardGameType BoardGame { get; private set; }

    public override string ToString() => this.Name.ToString();

    public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    {
      base.Deserialize(objectManager, node);
      this.TownEdgeNumber = node.Attributes["town_edge_number"] == null ? 0 : Convert.ToInt32(node.Attributes["town_edge_number"].Value);
      this.MilitiaBonus = node.Attributes["militia_bonus"] == null ? 0 : Convert.ToInt32(node.Attributes["militia_bonus"].Value);
      this.ProsperityBonus = node.Attributes["prosperity_bonus"] == null ? 0 : Convert.ToInt32(node.Attributes["prosperity_bonus"].Value);
      this.DefaultPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("default_party_template", node);
      this.VillagerPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("villager_party_template", node);
      this.MilitiaPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("militia_party_template", node);
      this.RebelsPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("rebels_party_template", node);
      this.CaravanPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("caravan_party_template", node);
      this.EliteCaravanPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("elite_caravan_party_template", node);
      this.BanditBossPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("bandit_boss_party_template", node);
      this.VassalRewardTroopsPartyTemplate = objectManager.ReadObjectReferenceFromXml<PartyTemplateObject>("vassal_reward_party_template", node);
      this.EliteBasicTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("elite_basic_troop", node);
      this.MeleeEliteMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("melee_elite_militia_troop", node);
      this.RangedEliteMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("ranged_elite_militia_troop", node);
      this.MeleeMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("melee_militia_troop", node);
      this.RangedMilitiaTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("ranged_militia_troop", node);
      this.BasicTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("basic_troop", node);
      this.TournamentMaster = objectManager.ReadObjectReferenceFromXml<CharacterObject>("tournament_master", node);
      this.Villager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager", node);
      this.CaravanMaster = objectManager.ReadObjectReferenceFromXml<CharacterObject>("caravan_master", node);
      this.ArmedTrader = objectManager.ReadObjectReferenceFromXml<CharacterObject>("armed_trader", node);
      this.CaravanGuard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("caravan_guard", node);
      this.BasicMercenaryTroop = objectManager.ReadObjectReferenceFromXml<CharacterObject>("basic_mercenary_troop", node);
      this.DuelPreset = objectManager.ReadObjectReferenceFromXml<CharacterObject>("duel_preset", node);
      this.PrisonGuard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("prison_guard", node);
      this.Guard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("guard", node);
      this.Blacksmith = objectManager.ReadObjectReferenceFromXml<CharacterObject>("blacksmith", node);
      this.Weaponsmith = objectManager.ReadObjectReferenceFromXml<CharacterObject>("weaponsmith", node);
      this.Townswoman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman", node);
      this.TownswomanInfant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman_infant", node);
      this.TownswomanChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman_child", node);
      this.TownswomanTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townswoman_teenager", node);
      this.Townsman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman", node);
      this.TownsmanInfant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman_infant", node);
      this.TownsmanChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman_child", node);
      this.TownsmanTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("townsman_teenager", node);
      this.VillageWoman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("village_woman", node);
      this.VillagerMaleChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_male_child", node);
      this.VillagerMaleTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_male_teenager", node);
      this.VillagerFemaleChild = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_female_child", node);
      this.VillagerFemaleTeenager = objectManager.ReadObjectReferenceFromXml<CharacterObject>("villager_female_teenager", node);
      this.RansomBroker = objectManager.ReadObjectReferenceFromXml<CharacterObject>("ransom_broker", node);
      this.GangleaderBodyguard = objectManager.ReadObjectReferenceFromXml<CharacterObject>("gangleader_bodyguard", node);
      this.MerchantNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("merchant_notary", node);
      this.ArtisanNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("artisan_notary", node);
      this.PreacherNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("preacher_notary", node);
      this.RuralNotableNotary = objectManager.ReadObjectReferenceFromXml<CharacterObject>("rural_notable_notary", node);
      this.ShopWorker = objectManager.ReadObjectReferenceFromXml<CharacterObject>("shop_worker", node);
      this.Tavernkeeper = objectManager.ReadObjectReferenceFromXml<CharacterObject>("tavernkeeper", node);
      this.TavernGamehost = objectManager.ReadObjectReferenceFromXml<CharacterObject>("taverngamehost", node);
      this.Musician = objectManager.ReadObjectReferenceFromXml<CharacterObject>("musician", node);
      this.TavernWench = objectManager.ReadObjectReferenceFromXml<CharacterObject>("tavern_wench", node);
      this.Armorer = objectManager.ReadObjectReferenceFromXml<CharacterObject>("armorer", node);
      this.HorseMerchant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("horseMerchant", node);
      this.Barber = objectManager.ReadObjectReferenceFromXml<CharacterObject>("barber", node);
      this.Merchant = objectManager.ReadObjectReferenceFromXml<CharacterObject>("merchant", node);
      this.Beggar = objectManager.ReadObjectReferenceFromXml<CharacterObject>("beggar", node);
      this.FemaleBeggar = objectManager.ReadObjectReferenceFromXml<CharacterObject>("female_beggar", node);
      this.FemaleDancer = objectManager.ReadObjectReferenceFromXml<CharacterObject>("female_dancer", node);
      this.MilitiaArcher = objectManager.ReadObjectReferenceFromXml<CharacterObject>("militia_archer", node);
      this.MilitiaVeteranArcher = objectManager.ReadObjectReferenceFromXml<CharacterObject>("militia_veteran_archer", node);
      this.MilitiaSpearman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("militia_spearman      ", node);
      this.MilitiaVeteranSpearman = objectManager.ReadObjectReferenceFromXml<CharacterObject>("militia_veteran_spearman", node);
      this.GearPracticeDummy = objectManager.ReadObjectReferenceFromXml<CharacterObject>("gear_practice_dummy     ", node);
      this.WeaponPracticeStage1 = objectManager.ReadObjectReferenceFromXml<CharacterObject>("weapon_practice_stage_1", node);
      this.WeaponPracticeStage2 = objectManager.ReadObjectReferenceFromXml<CharacterObject>("weapon_practice_stage_2", node);
      this.WeaponPracticeStage3 = objectManager.ReadObjectReferenceFromXml<CharacterObject>("weapon_practice_stage_3", node);
      this.GearDummy = objectManager.ReadObjectReferenceFromXml<CharacterObject>("gear_dummy", node);
      this.BanditBandit = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_bandit", node);
      this.BanditRaider = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_raider", node);
      this.BanditChief = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_chief", node);
      this.BanditBoss = objectManager.ReadObjectReferenceFromXml<CharacterObject>("bandit_boss", node);
      this.EncyclopediaText = node.Attributes["text"] != null ? new TextObject(node.Attributes["text"].Value) : TextObject.Empty;
      CultureObject.BoardGameType result1;
      if (node.Attributes["board_game_type"] != null && Enum.TryParse<CultureObject.BoardGameType>(node.Attributes["board_game_type"].Value, out result1))
        this.BoardGame = result1;
      XmlNodeList childNodes = node.ChildNodes;
      this._defaultPolicyList = new List<PolicyObject>();
      this._maleNameList = new List<TextObject>();
      this._femaleNameList = new List<TextObject>();
      this._clanNameList = new List<TextObject>();
      this._cultureFeats = new List<FeatObject>();
      List<int> list1 = new List<int>();
      List<CharacterObject> list2 = new List<CharacterObject>();
      List<CharacterObject> list3 = new List<CharacterObject>();
      List<CharacterObject> list4 = new List<CharacterObject>();
      List<CharacterObject> list5 = new List<CharacterObject>();
      List<CharacterObject> list6 = new List<CharacterObject>();
      List<CharacterObject> list7 = new List<CharacterObject>();
      List<CharacterObject> list8 = new List<CharacterObject>();
      List<ItemObject> list9 = new List<ItemObject>();
      foreach (XmlNode xmlNode in childNodes)
      {
        if (xmlNode.Name == "default_policies")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            this._defaultPolicyList.Add(objectManager.GetObject<PolicyObject>(childNode.Attributes["id"].Value));
        }
        else if (xmlNode.Name == "male_names")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            this._maleNameList.Add(new TextObject(childNode.Attributes["name"].Value));
        }
        else if (xmlNode.Name == "female_names")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            this._femaleNameList.Add(new TextObject(childNode.Attributes["name"].Value));
        }
        else if (xmlNode.Name == "clan_names")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            this._clanNameList.Add(new TextObject(childNode.Attributes["name"].Value));
        }
        else if (xmlNode.Name == "cultural_feats")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
          {
            FeatObject featObject = MBObjectManager.Instance.GetObject<FeatObject>(childNode.Attributes["id"].Value);
            if (!this._cultureFeats.Contains(featObject))
              this._cultureFeats.Add(featObject);
            else
              Debug.FailedAssert("Feat object already exists!", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CultureObject.cs", nameof (Deserialize), 374);
          }
        }
        else if (xmlNode.Name == "possible_clan_banner_icon_ids")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
          {
            int result2;
            int.TryParse(childNode.Attributes["id"].Value, out result2);
            list1.Add(result2);
          }
        }
        else if (xmlNode.Name == "child_character_templates")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
          {
            CharacterObject characterObject = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode);
            list2.Add(characterObject);
          }
        }
        else if (xmlNode.Name == "notable_and_wanderer_templates")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
          {
            CharacterObject characterObject = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode);
            list3.Add(characterObject);
          }
        }
        else if (xmlNode.Name == "lord_templates")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
          {
            CharacterObject characterObject = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode);
            list5.Add(characterObject);
          }
        }
        else if (xmlNode.Name == "rebellion_hero_templates")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
          {
            CharacterObject characterObject = objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode);
            list4.Add(characterObject);
          }
        }
        else if (xmlNode.Name == "tournament_team_templates_one_participant")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            list6.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode));
        }
        else if (xmlNode.Name == "tournament_team_templates_two_participant")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            list7.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode));
        }
        else if (xmlNode.Name == "tournament_team_templates_four_participant")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            list8.Add(objectManager.ReadObjectReferenceFromXml<CharacterObject>("name", childNode));
        }
        else if (xmlNode.Name == "vassal_reward_items")
        {
          foreach (XmlNode childNode in xmlNode.ChildNodes)
            list9.Add(objectManager.ReadObjectReferenceFromXml<ItemObject>("id", childNode));
        }
      }
      this.DefaultPolicyList = (IReadOnlyList<PolicyObject>) this._defaultPolicyList.GetReadOnlyList<PolicyObject>();
      this.MaleNameList = (IReadOnlyList<TextObject>) this._maleNameList.GetReadOnlyList<TextObject>();
      this.FemaleNameList = (IReadOnlyList<TextObject>) this._femaleNameList.GetReadOnlyList<TextObject>();
      this.ClanNameList = (IReadOnlyList<TextObject>) this._clanNameList.GetReadOnlyList<TextObject>();
      this.PossibleClanBannerIconsIDs = (IReadOnlyList<int>) list1.GetReadOnlyList<int>();
      this.ChildCharacterTemplates = (IReadOnlyList<CharacterObject>) list2.GetReadOnlyList<CharacterObject>();
      this.NotableAndWandererTemplates = (IReadOnlyList<CharacterObject>) list3.GetReadOnlyList<CharacterObject>();
      this.RebelliousHeroTemplates = (IReadOnlyList<CharacterObject>) list4.GetReadOnlyList<CharacterObject>();
      this.LordTemplates = (IReadOnlyList<CharacterObject>) list5.GetReadOnlyList<CharacterObject>();
      this.TournamentTeamTemplatesForOneParticipant = (IReadOnlyList<CharacterObject>) list6.GetReadOnlyList<CharacterObject>();
      this.TournamentTeamTemplatesForTwoParticipant = (IReadOnlyList<CharacterObject>) list7.GetReadOnlyList<CharacterObject>();
      this.TournamentTeamTemplatesForFourParticipant = (IReadOnlyList<CharacterObject>) list8.GetReadOnlyList<CharacterObject>();
      list9.RemoveAll((Predicate<ItemObject>) (x => !x.IsReady));
      this.VassalRewardItems = (IReadOnlyList<ItemObject>) list9.GetReadOnlyList<ItemObject>();
    }

    public override TextObject GetName() => this.Name;

    public enum BoardGameType
    {
      None = -1, // 0xFFFFFFFF
      Seega = 0,
      Puluc = 1,
      Konane = 2,
      MuTorere = 3,
      Tablut = 4,
      BaghChal = 5,
      Total = 6,
    }
  }
}