using System;
using System.Collections.Generic;
using System.Linq;
using BOF.Overhaul.CampaignSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Clan = BOF.Overhaul.CampaignSystem.Clan;
using Hero = BOF.Overhaul.CampaignSystem.Hero;
using IFaction = BOF.Overhaul.CampaignSystem.IFaction;
using MobileParty = BOF.Overhaul.CampaignSystem.MobileParty;
using Settlement = BOF.Overhaul.CampaignSystem.Settlement;
using Town = BOF.Overhaul.CampaignSystem.Town;

namespace BOF.Overhaul.Helpers
{
  public static class SettlementHelper
  {
    public static Settlement FindNearestSettlement(
      Func<Settlement, bool> condition,
      IMapPoint toMapPoint = null)
    {
      return SettlementHelper.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint) MobileParty.MainParty, (IEnumerable<Settlement>) Settlement.All, condition);
    }

    public static Settlement FindNearestHideout(
      Func<Settlement, bool> condition = null,
      IMapPoint toMapPoint = null)
    {
      return SettlementHelper.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint) MobileParty.MainParty, Hideout.All.Select<Hideout, Settlement>((Func<Hideout, Settlement>) (x => x.Settlement)), condition);
    }

    public static Settlement FindNearestTown(
      Func<Settlement, bool> condition = null,
      IMapPoint toMapPoint = null)
    {
      return SettlementHelper.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint) MobileParty.MainParty, Town.AllTowns.Select<Town, Settlement>((Func<Town, Settlement>) (x => x.Settlement)), condition);
    }

    public static Settlement FindNearestFortification(
      Func<Settlement, bool> condition = null,
      IMapPoint toMapPoint = null)
    {
      return SettlementHelper.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint) MobileParty.MainParty, Town.AllFiefs.Select<Town, Settlement>((Func<Town, Settlement>) (x => x.Settlement)), condition);
    }

    public static Settlement FindNearestCastle(
      Func<Settlement, bool> condition = null,
      IMapPoint toMapPoint = null)
    {
      return SettlementHelper.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint) MobileParty.MainParty, Town.AllCastles.Select<Town, Settlement>((Func<Town, Settlement>) (x => x.Settlement)), condition);
    }

    public static Settlement FindNearestVillage(
      Func<Settlement, bool> condition = null,
      IMapPoint toMapPoint = null)
    {
      return SettlementHelper.FindNearestSettlementToMapPointInternal(toMapPoint ?? (IMapPoint) MobileParty.MainParty, Village.All.Select<Village, Settlement>((Func<Village, Settlement>) (x => x.Settlement)), condition);
    }

    private static Settlement FindNearestSettlementToMapPointInternal(
      IMapPoint mapPoint,
      IEnumerable<Settlement> settlementsToIterate,
      Func<Settlement, bool> condition = null)
    {
      Settlement settlement = (Settlement) null;
      float maximumDistance = 1E+09f;
      foreach (Settlement toSettlement in settlementsToIterate)
      {
        float distance;
        if ((condition == null || condition(toSettlement)) && BOFCampaign.Current.Models.MapDistanceModel.GetDistance(mapPoint, toSettlement, maximumDistance, out distance))
        {
          settlement = toSettlement;
          maximumDistance = distance;
        }
      }
      return settlement;
    }

    public static Settlement FindNearestSettlementToPoint(
      Vec2 point,
      Func<Settlement, bool> condition = null)
    {
      Settlement settlement1 = (Settlement) null;
      float maximumDistance = Campaign.MapDiagonal;
      foreach (Settlement settlement2 in Settlement.All)
      {
        float distance;
        if ((condition == null || condition(settlement2)) && BOFCampaign.Current.Models.MapDistanceModel.GetDistance((IMapPoint) settlement2, in point, maximumDistance, out distance))
        {
          settlement1 = settlement2;
          maximumDistance = distance;
        }
      }
      return settlement1;
    }

    public static List<Settlement> FindSettlementsAroundMapPoint(
      IMapPoint mapPoint,
      Func<Settlement, bool> condition,
      float maxdistance)
    {
      List<Settlement> settlementList = new List<Settlement>();
      foreach (Settlement toSettlement in Settlement.All)
      {
        float distance;
        if (condition(toSettlement) && BOFCampaign.Current.Models.MapDistanceModel.GetDistance(mapPoint, toSettlement, maxdistance, out distance) && (double) distance < (double) maxdistance)
          settlementList.Add(toSettlement);
      }
      return settlementList;
    }

    private static Settlement FindRandomInternal(
      Func<Settlement, bool> condition,
      IEnumerable<Settlement> settlementsToIterate)
    {
      List<Settlement> settlementList = new List<Settlement>();
      foreach (Settlement settlement in settlementsToIterate)
      {
        if (condition(settlement))
          settlementList.Add(settlement);
      }
      return settlementList.Count > 0 ? settlementList[MBRandom.RandomInt(settlementList.Count)] : (Settlement) null;
    }

    public static Settlement FindRandomSettlement(Func<Settlement, bool> condition = null) => SettlementHelper.FindRandomInternal(condition, (IEnumerable<Settlement>) Settlement.All);

    public static Settlement FindRandomHideout(Func<Settlement, bool> condition = null) => SettlementHelper.FindRandomInternal(condition, Hideout.All.Select<Hideout, Settlement>((Func<Hideout, Settlement>) (x => x.Settlement)));

    public static void TakeEnemyVillagersOutsideSettlements(Settlement settlementWhichChangedFaction)
    {
      if (settlementWhichChangedFaction.IsFortification)
      {
        bool flag1;
        do
        {
          flag1 = false;
          MobileParty mobileParty = (MobileParty) null;
          foreach (MobileParty party in settlementWhichChangedFaction.Parties)
          {
            if (party.IsVillager && party.HomeSettlement.IsVillage && party.HomeSettlement.Village.Bound == settlementWhichChangedFaction && party.HomeSettlement.MapFaction != settlementWhichChangedFaction.MapFaction)
            {
              mobileParty = party;
              flag1 = true;
              break;
            }
          }
          if (flag1 && mobileParty.MapEvent == null)
          {
            LeaveSettlementAction.ApplyForParty(mobileParty);
            mobileParty.SetMoveModeHold();
          }
        }
        while (flag1);
        bool flag2;
        do
        {
          flag2 = false;
          MobileParty mobileParty = (MobileParty) null;
          foreach (MobileParty party in settlementWhichChangedFaction.Parties)
          {
            if (party.IsCaravan && FactionManager.IsAtWarAgainstFaction(party.MapFaction, settlementWhichChangedFaction.MapFaction))
            {
              mobileParty = party;
              flag2 = true;
              break;
            }
          }
          if (flag2 && mobileParty.MapEvent == null)
          {
            LeaveSettlementAction.ApplyForParty(mobileParty);
            mobileParty.SetMoveModeHold();
          }
        }
        while (flag2);
        foreach (MobileParty mobileParty in MobileParty.All)
        {
          if ((mobileParty.IsVillager || mobileParty.IsCaravan) && mobileParty.TargetSettlement == settlementWhichChangedFaction && mobileParty.CurrentSettlement != settlementWhichChangedFaction)
            mobileParty.SetMoveModeHold();
        }
      }
      if (!settlementWhichChangedFaction.IsVillage)
        return;
      foreach (MobileParty mobileParty in MobileParty.All)
      {
        if (mobileParty.IsVillager && mobileParty.HomeSettlement == settlementWhichChangedFaction && mobileParty.CurrentSettlement != settlementWhichChangedFaction)
        {
          if (mobileParty.CurrentSettlement != null && mobileParty.MapEvent == null)
          {
            LeaveSettlementAction.ApplyForParty(mobileParty);
            mobileParty.SetMoveModeHold();
          }
          else
            mobileParty.SetMoveModeHold();
        }
      }
    }

    public static Settlement GetRandomTown(Clan fromFaction = null)
    {
      int num1 = 0;
      foreach (Settlement settlement in BOFCampaign.Current.Settlements)
      {
        if ((fromFaction == null || settlement.MapFaction == fromFaction) && (settlement.IsTown || settlement.IsVillage))
          ++num1;
      }
      int num2 = MBRandom.RandomInt(0, num1 - 1);
      foreach (Settlement settlement in BOFCampaign.Current.Settlements)
      {
        if ((fromFaction == null || settlement.MapFaction == fromFaction) && (settlement.IsTown || settlement.IsVillage))
        {
          --num2;
          if (num2 < 0)
            return settlement;
        }
      }
      return (Settlement) null;
    }

    public static Settlement GetBestSettlementToSpawnAround(Hero hero)
    {
      Settlement settlement1 = (Settlement) null;
      float num1 = -1f;
      int index = 0;
      foreach (Hero lord in hero.Clan.Lords)
      {
        if (lord != hero)
          ++index;
        else
          break;
      }
      IFaction mapFaction1 = hero.MapFaction;
      foreach (Settlement settlement2 in Settlement.All)
      {
        if (settlement2.Party.MapEvent == null)
        {
          IFaction mapFaction2 = settlement2.MapFaction;
          float num2 = 0.0001f;
          if (mapFaction2 == mapFaction1)
            num2 = 1f;
          else if (FactionManager.IsAlliedWithFaction(mapFaction2, mapFaction1))
            num2 = 0.01f;
          else if (FactionManager.IsNeutralWithFaction(mapFaction2, mapFaction1))
            num2 = 0.0005f;
          float num3 = 0.0f;
          if (settlement2.IsTown)
            num3 = 1f;
          else if (settlement2.IsCastle)
            num3 = 0.9f;
          else if (settlement2.IsVillage)
            num3 = 0.8f;
          else if (settlement2.IsHideout)
            num3 = mapFaction2 == mapFaction1 ? 0.2f : 0.0f;
          float num4 = settlement2.Town == null || settlement2.Town.GarrisonParty == null || settlement2.OwnerClan != hero.Clan ? 1f : settlement2.Town.GarrisonParty.Party.TotalStrength / (settlement2.IsTown ? 60f : 30f);
          float num5 = settlement2.IsUnderRaid || settlement2.IsUnderSiege ? 0.1f : 1f;
          float num6 = settlement2.OwnerClan == hero.Clan ? 1f : 0.25f;
          float num7 = (float) ((double) settlement2.Random.GetValueNormalized(index) * 0.5 + 0.5);
          float num8 = (float) (1.0 - (double) hero.MapFaction.InitialPosition.Distance(settlement2.Position2D) / (double) Campaign.MapDiagonal);
          float num9 = num8 * num8;
          float num10 = num2 * num3 * num5 * num6 * num4 * num7 * num9;
          if ((double) num10 > (double) num1)
          {
            num1 = num10;
            settlement1 = settlement2;
          }
        }
      }
      return settlement1;
    }

    public static IEnumerable<Hero> GetAllHeroesOfSettlement(
      Settlement settlement,
      bool includePrisoners)
    {
      foreach (MobileParty party in settlement.Parties)
      {
        if (party.LeaderHero != null)
          yield return party.LeaderHero;
      }
      foreach (Hero hero in settlement.HeroesWithoutParty)
        yield return hero;
      if (includePrisoners)
      {
        foreach (TroopRosterElement troopRosterElement in settlement.Party.PrisonRoster.GetTroopRoster())
        {
          if (troopRosterElement.Character.IsHero)
            yield return troopRosterElement.Character.HeroObject;
        }
      }
    }

    public static int NumberOfVolunteersCanBeRecruitedFrom(Hero hero, Settlement settlement)
    {
      int num1 = 0;
      if (hero.PartyBelongedTo.UnlimitedWage || hero.PartyBelongedTo.TotalWage < hero.PartyBelongedTo.PaymentLimit)
      {
        foreach (Hero notable in settlement.Notables)
        {
          int num2 = HeroHelper.MaximumIndexHeroCanRecruitFromHero(hero, notable);
          for (int index = 0; index < num2; ++index)
          {
            if (notable.VolunteerTypes[index] != null)
              ++num1;
          }
        }
      }
      return num1;
    }

    public static int NumberOfVolunteersCanBeRecruitedForGarrison(Settlement settlement)
    {
      int num1 = 0;
      Hero leader = settlement.OwnerClan.Leader;
      foreach (Hero notable in settlement.Notables)
      {
        int num2 = HeroHelper.MaximumIndexHeroCanRecruitFromHero(leader, notable);
        for (int index = 0; index < num2; ++index)
        {
          if (notable.VolunteerTypes[index] != null)
            ++num1;
        }
      }
      foreach (Village boundVillage in settlement.BoundVillages)
      {
        if (boundVillage.VillageState == Village.VillageStates.Normal)
          num1 += SettlementHelper.NumberOfVolunteersCanBeRecruitedForGarrison(boundVillage.Settlement);
      }
      return num1;
    }

    public static bool IsThereAnyVolunteerCanBeRecruitedForGarrison(Settlement settlement)
    {
      Hero leader = settlement.OwnerClan.Leader;
      foreach (Hero notable in settlement.Notables)
      {
        int num = HeroHelper.MaximumIndexHeroCanRecruitFromHero(leader, notable);
        for (int index = 0; index < num; ++index)
        {
          if (notable.VolunteerTypes[index] != null)
            return true;
        }
      }
      foreach (Village boundVillage in settlement.BoundVillages)
      {
        if (boundVillage.VillageState == Village.VillageStates.Normal && SettlementHelper.IsThereAnyVolunteerCanBeRecruitedForGarrison(boundVillage.Settlement))
          return true;
      }
      return false;
    }

    public static bool IsGarrisonStarving(Settlement settlement) => (double) settlement.Town.FoodChange < -((double) settlement.Prosperity / (double) BOFCampaign.Current.Models.SettlementFoodModel.NumberOfProsperityToEatOneFood);
  }
}