using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using MountAndBlade.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.Overhaul.Helpers
{
  public static class CharacterHelper
  {
    public static TextObject GetReputationDescription(CharacterObject character)
    {
      CharacterObject character1 = character;
      TextObject textObject = new TextObject("{=!}{REPUTATION_SUMMARY}.");
      TextObject matchingTextOrNull = BOFCampaign.Current.ConversationManager.FindMatchingTextOrNull("reputation", character1);
      StringHelpers.SetCharacterProperties("NOTABLE", character, matchingTextOrNull);
      textObject.SetTextVariable("REPUTATION_SUMMARY", matchingTextOrNull.ToString());
      return textObject;
    }

    public static IFaceGeneratorCustomFilter GetFaceGeneratorFilter() => Campaign.Current.GetCampaignBehavior<IFacegenCampaignBehavior>()?.GetFaceGenFilter();

    public static string GetStandingBodyIdle(CharacterObject character, bool forEncyclopedia)
    {
      HeroHelper.WillLordAttack();
      string str = "normal";
      TraitObject persona = character.GetPersona();
      bool flag1 = Settlement.CurrentSettlement != null | forEncyclopedia;
      if (character.IsHero)
      {
        if (character.HeroObject.IsWounded)
          return "weary";
        int num = !character.HeroObject.IsHumanPlayerCharacter ? 1 : 0;
        int superiorityState = GetSuperiorityState(character);
        if (num != 0)
        {
          int relation = Hero.MainHero.GetRelation(character.HeroObject);
          bool flag2 = CheckFavorable(character);
          if (character.IsFemale && character.HeroObject.Noncombatant)
          {
            if (relation < 0)
              str = "closed";
            else if (persona == DefaultTraits.PersonaIronic)
              str = (double) MBRandom.RandomFloat <= 0.5 ? "confident" : "confident2";
            else if (persona == DefaultTraits.PersonaCurt)
              str = (double) MBRandom.RandomFloat <= 0.5 ? "closed" : "confident";
            else if (persona == DefaultTraits.PersonaEarnest || persona == DefaultTraits.PersonaSoftspoken)
              str = (double) MBRandom.RandomFloat <= 0.699999988079071 ? "demure" : "confident";
          }
          else if (relation < 0)
          {
            switch (superiorityState)
            {
              case -1:
                str = persona != DefaultTraits.PersonaSoftspoken ? (persona != DefaultTraits.PersonaIronic ? (character.IsFemale ? "closed" : "warrior") : (!flag2 ? "closed" : ((double) MBRandom.RandomFloat <= 0.5 ? "closed" : "aggressive"))) : (!flag2 ? (character.IsFemale ? "closed" : "normal") : "closed");
                break;
              case 1:
                str = persona != DefaultTraits.PersonaSoftspoken ? (persona != DefaultTraits.PersonaIronic ? (character.IsFemale ? "closed" : "warrior") : (character.IsFemale ? "closed" : "aggressive")) : (character.IsFemale ? "closed" : "warrior");
                break;
              default:
                str = "closed";
                break;
            }
          }
          else if (relation >= 0 && superiorityState >= 0)
          {
            if (persona == DefaultTraits.PersonaIronic)
              str = !flag1 ? "confident2" : (!flag2 ? ((double) MBRandom.RandomFloat <= 0.5 ? "hip" : "normal") : ((double) MBRandom.RandomFloat <= 0.699999988079071 ? "confident2" : "normal"));
            else if (persona == DefaultTraits.PersonaSoftspoken)
              str = !flag1 ? "normal" : (!flag2 ? ((double) MBRandom.RandomFloat <= 0.5 ? "normal" : "demure") : ((double) MBRandom.RandomFloat <= 0.5 ? "normal" : "closed"));
            else if (persona == DefaultTraits.PersonaCurt)
              str = !flag1 ? "normal" : (!flag2 ? ((double) MBRandom.RandomFloat <= 0.400000005960464 ? "warrior" : "closed") : ((double) MBRandom.RandomFloat <= 0.600000023841858 ? "normal" : "closed"));
            else if (persona == DefaultTraits.PersonaEarnest)
              str = !flag1 ? "normal" : (!flag2 ? ((double) MBRandom.RandomFloat <= 0.200000002980232 ? "normal" : "confident") : ((double) MBRandom.RandomFloat <= 0.600000023841858 ? "normal" : "confident"));
          }
        }
      }
      if (character.Occupation == Occupation.Bandit || character.Occupation == Occupation.Gangster)
        str = (double) MBRandom.RandomFloat <= 0.699999988079071 ? "aggressive" : "hip";
      if (character.Occupation == Occupation.Guard || character.Occupation == Occupation.PrisonGuard || character.Occupation == Occupation.Soldier)
        str = "normal";
      return str;
    }

    public static string GetDefaultFaceIdle(CharacterObject character)
    {
      string str1 = "convo_normal";
      string str2 = "convo_bemused";
      string str3 = "convo_mocking_aristocratic";
      string str4 = "convo_mocking_teasing";
      string str5 = "convo_mocking_revenge";
      string str6 = "convo_contemptuous";
      string str7 = "convo_delighted";
      string str8 = "convo_approving";
      string str9 = "convo_relaxed_happy";
      string str10 = "convo_nonchalant";
      string str11 = "convo_thinking";
      string str12 = "convo_grave";
      string str13 = "convo_stern";
      string str14 = "convo_very_stern";
      string str15 = "convo_beaten";
      string str16 = "convo_predatory";
      string str17 = "convo_confused_annoyed";
      bool flag1 = false;
      bool flag2 = false;
      if (character.IsHero)
      {
        flag1 = character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) + character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) > 0;
        flag2 = character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) + character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) < 0;
      }
      bool flag3 = (double) Hero.MainHero.Clan.Renown < 0.0;
      bool flag4 = false;
      if (PlayerEncounter.Current != null && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && (PlayerEncounter.EncounteredMobileParty == null || PlayerEncounter.EncounteredMobileParty.GetCookie<DoNotAttackMainPartyCookie>() == null) && PlayerEncounter.EncounteredParty.Owner != null && FactionManager.IsAtWarAgainstFaction(PlayerEncounter.EncounteredParty.MapFaction, Hero.MainHero.MapFaction))
        flag4 = true;
      if (Campaign.Current.CurrentConversationContext == ConversationContext.CapturedLord && character.IsHero && character.HeroObject.MapFaction == PlayerEncounter.EncounteredParty.MapFaction)
        return str13;
      if (character.HeroObject != null)
      {
        int relation = character.HeroObject.GetRelation(Hero.MainHero);
        if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaIronic)
        {
          if (relation > 4)
            return str4;
          if (relation < -10)
            return str5;
          if (character.Occupation == Occupation.GangLeader && character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) < 0 || character.Occupation == Occupation.GangLeader & flag3)
            return str10;
          if (!character.HeroObject.IsNoble)
            return str3;
          return character.IsFemale ? str2 : str4;
        }
        if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaCurt)
        {
          if (relation > 4)
            return str7;
          if (relation < -20)
            return str4;
          if (character.Occupation == Occupation.GangLeader & flag3)
            return str16;
          return flag2 ? str12 : str1;
        }
        if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaSoftspoken)
        {
          if (relation > 4)
            return str7;
          if (relation < -20)
            return str17;
          if (((!(character.HeroObject.IsNoble & flag3) ? 0 : (!character.IsFemale ? 1 : 0)) & (flag2 ? 1 : 0)) != 0)
            return str6;
          if (((!(character.HeroObject.IsNoble & flag3) ? 0 : (!character.IsFemale ? 1 : 0)) & (flag2 ? 1 : 0)) != 0)
            return str10;
          return flag1 ? str8 : str11;
        }
        if (character.HeroObject != null && character.GetPersona() == DefaultTraits.PersonaEarnest)
        {
          if (relation > 4)
            return str7;
          if (relation < -40)
            return str14;
          if (relation < -20)
            return str13;
          if (character.HeroObject.IsNoble & flag2)
            return str10;
          return flag1 ? str8 : str1;
        }
      }
      else if (character.Occupation == Occupation.Villager || character.Occupation == Occupation.Townsfolk)
      {
        int deterministicHashCode = character.StringId.GetDeterministicHashCode();
        if (Settlement.CurrentSettlement != null && (double) Settlement.CurrentSettlement.Prosperity < (double) (200 * (Settlement.CurrentSettlement.IsTown ? 5 : 1)) && deterministicHashCode % 2 == 0)
          return str15;
        if (deterministicHashCode % 2 == 1)
          return str9;
      }
      else if (flag4 && character.Occupation == Occupation.Bandit)
        return str13;
      return str1;
    }

    private static int GetSuperiorityState(CharacterObject character)
    {
      if (character.IsHero && character.HeroObject.MapFaction != null && character.HeroObject.MapFaction.IsKingdomFaction && character.HeroObject.IsNoble)
        return 1;
      return character.Occupation == Occupation.Villager || character.Occupation == Occupation.Townsfolk || character.Occupation == Occupation.Bandit || character.Occupation == Occupation.Gangster || character.Occupation == Occupation.Wanderer ? -1 : 0;
    }

    private static bool CheckFavorable(CharacterObject otherCharacter) => (otherCharacter.HeroObject.PartyBelongedTo == null ? (double) otherCharacter.HeroObject.Power : (double) otherCharacter.HeroObject.PartyBelongedTo.Party.TotalStrength) > (double) MobileParty.MainParty.Party.TotalStrength;

    public static CharacterObject FindUpgradeRootOf(CharacterObject character)
    {
      foreach (CharacterObject characterObject in CharacterObject.All)
      {
        if (characterObject.IsBasicTroop && UpgradeTreeContains(characterObject, characterObject, character))
          return characterObject;
      }
      return character;
    }

    private static bool UpgradeTreeContains(
      CharacterObject rootTroop,
      CharacterObject baseTroop,
      CharacterObject character)
    {
      if (baseTroop == character)
        return true;
      for (int index = 0; index < baseTroop.UpgradeTargets.Length && baseTroop.UpgradeTargets[index] != rootTroop; ++index)
      {
        if (UpgradeTreeContains(rootTroop, baseTroop.UpgradeTargets[index], character))
          return true;
      }
      return false;
    }

    public static ItemObject GetDefaultWeapon(CharacterObject affectorCharacter)
    {
      for (int index = 0; index <= 4; ++index)
      {
        EquipmentElement equipmentFromSlot = affectorCharacter.Equipment.GetEquipmentFromSlot((EquipmentIndex) index);
        if (equipmentFromSlot.Item?.PrimaryWeapon != null && equipmentFromSlot.Item.PrimaryWeapon.WeaponFlags.HasAnyFlag<WeaponFlags>(WeaponFlags.WeaponMask))
          return equipmentFromSlot.Item;
      }
      return (ItemObject) null;
    }

    public static bool CanUseItemBasedOnSkill(
      BasicCharacterObject currentCharacter,
      EquipmentElement itemRosterElement)
    {
      ItemObject itemObject = itemRosterElement.Item;
      SkillObject relevantSkill = itemObject.RelevantSkill;
      if (relevantSkill != null && currentCharacter.GetSkillValue(relevantSkill) < itemObject.Difficulty || currentCharacter.IsFemale && itemObject.ItemFlags.HasAnyFlag<ItemFlags>(ItemFlags.NotUsableByFemale))
        return false;
      return currentCharacter.IsFemale || !itemObject.ItemFlags.HasAnyFlag<ItemFlags>(ItemFlags.NotUsableByMale);
    }

    public static int GetPartyMemberFaceSeed(
      PartyBase party,
      BasicCharacterObject character,
      int rank)
    {
      int num = party.Index * 171 + character.StringId.GetDeterministicHashCode() * 6791 + rank * 197;
      return (num >= 0 ? num : -num) % 2000;
    }

    public static int GetDefaultFaceSeed(BasicCharacterObject character, int rank) => character.GetDefaultFaceSeed(rank);

    public static int GetCharacterTier(CampaignSystem.CharacterObject character) => character.IsHero ? 0 : MathF.Min(MathF.Max(MathF.Ceiling((float) (((double) character.Level - 5.0) / 5.0)), 0), 7);

    public static IEnumerable<CharacterObject> GetTroopTree(
      CharacterObject baseTroop,
      float minTier = -1f,
      float maxTier = 3.402823E+38f)
    {
      MBQueue<CharacterObject> queue = new MBQueue<CharacterObject>();
      queue.Enqueue(baseTroop);
      while (queue.Count > 0)
      {
        CharacterObject character = queue.Dequeue();
        if ((double) character.Tier >= (double) minTier && (double) character.Tier <= (double) maxTier)
          yield return character;
        foreach (CharacterObject upgradeTarget in character.UpgradeTargets)
          queue.Enqueue(upgradeTarget);
        character = (CharacterObject) null;
      }
    }

    public static void DeleteQuestCharacter(CharacterObject character, Settlement questSettlement)
    {
      if (questSettlement != null)
      {
        IList<LocationCharacter> listOfCharacters = questSettlement.LocationComplex.GetListOfCharacters();
        if (listOfCharacters.Any<LocationCharacter>((Func<LocationCharacter, bool>) (x => x.Character == character)))
        {
          LocationCharacter locationCharacter = listOfCharacters.First<LocationCharacter>((Func<LocationCharacter, bool>) (x => x.Character == character));
          questSettlement.LocationComplex.RemoveCharacterIfExists(locationCharacter);
        }
      }
      Game.Current.ObjectManager.UnregisterObject((MBObjectBase) character);
    }
  }
}