using System;
using System.Collections.Generic;
using System.Linq;
using BOF.Overhaul.CampaignSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using CampaignData = BOF.Overhaul.CampaignSystem.CampaignData;
using Clan = BOF.Overhaul.CampaignSystem.Clan;
using Hero = BOF.Overhaul.CampaignSystem.Hero;
using IFaction = BOF.Overhaul.CampaignSystem.IFaction;
using Kingdom = BOF.Overhaul.CampaignSystem.Kingdom;
using MobileParty = BOF.Overhaul.CampaignSystem.MobileParty;
using StanceLink = BOF.Overhaul.CampaignSystem.StanceLink;

namespace BOF.Overhaul.Helpers
{
  public class FactionManager
  {
    // [SaveableField(20)]
    private FactionManagerStancesData _stances;

    internal static void AutoGeneratedStaticCollectObjectsFactionManager(
      object o,
      List<object> collectedObjects)
    {
      ((FactionManager) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
    }

    protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects) => collectedObjects.Add((object) this._stances);

    internal static object AutoGeneratedGetMemberValue_stances(object o) => (object) ((FactionManager) o)._stances;

    public static FactionManager Instance => BOFCampaign.Current.FactionManager;

    public FactionManager() => this._stances = new FactionManagerStancesData();

    // [LoadInitializationCallback]
    // private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    // {
    //   if (this._stances != null)
    //     return;
    //   this._stances = new FactionManagerStancesData();
    //   if (objectLoadData.GetDataValueBySaveId(4) is Dictionary<(IFaction, IFaction), StanceLink> dataValueBySaveId2)
    //   {
    //     foreach (KeyValuePair<(IFaction, IFaction), StanceLink> keyValuePair in dataValueBySaveId2)
    //       this._stances.AddStance(new StanceLink(keyValuePair.Value.StanceType, keyValuePair.Key.Item1, keyValuePair.Key.Item2));
    //   }
    //   else
    //   {
    //     if (!(objectLoadData.GetDataValueBySaveId(10) is Dictionary<FactionManager.FactionPairObsolete, StanceLink> dataValueBySaveId3))
    //       return;
    //     foreach (KeyValuePair<FactionManager.FactionPairObsolete, StanceLink> keyValuePair in dataValueBySaveId3)
    //       this._stances.AddStance(keyValuePair.Value);
    //   }
    // }

    public void AfterLoad()
    {
      StanceLink[] array = this._stances.GetStanceLinks().Where<StanceLink>((Func<StanceLink, bool>) (x =>
      {
        if (x.Faction1 == x.Faction2 || x.Faction1.IsBanditFaction || x.Faction2.IsBanditFaction)
          return true;
        if (!x.IsAtWar)
          return false;
        return x.Faction1 == CampaignData.NeutralFaction || x.Faction2 == CampaignData.NeutralFaction;
      })).ToArray<StanceLink>();
      if (!((IEnumerable<StanceLink>) array).IsEmpty<StanceLink>())
      {
        foreach (StanceLink stance in array)
          this._stances.RemoveStance(stance);
      }
      foreach (StanceLink stanceLink in this._stances.GetStanceLinks())
      {
        if (stanceLink.Faction1 != stanceLink.Faction2)
        {
          FactionManager.AddStanceToFaction(stanceLink.Faction1, stanceLink);
          FactionManager.AddStanceToFaction(stanceLink.Faction2, stanceLink);
        }
      }
    }

    internal StanceLink GetStanceLinkInternal(IFaction faction1, IFaction faction2)
    {
      StanceLink stanceLink = this._stances.GetStance(faction1, faction2);
      if (stanceLink == null)
      {
        stanceLink = new StanceLink(StanceType.Neutral, faction1, faction2);
        if (!faction1.IsBanditFaction && !faction2.IsBanditFaction)
          this.AddStance(faction1, faction2, stanceLink);
      }
      return stanceLink;
    }

    private void AddStance(IFaction faction1, IFaction faction2, StanceLink stanceLink)
    {
      this._stances.AddStance(stanceLink);
      FactionManager.AddStanceToFaction(faction1, stanceLink);
      FactionManager.AddStanceToFaction(faction2, stanceLink);
    }

    private void RemoveStance(StanceLink stance)
    {
      this._stances.RemoveStance(stance);
      FactionManager.RemoveStanceFromFaction(stance.Faction1, stance);
      FactionManager.RemoveStanceFromFaction(stance.Faction2, stance);
    }

    private static void AddStanceToFaction(IFaction faction1, StanceLink stanceLink)
    {
      if (faction1 is Kingdom kingdom)
        kingdom.AddStanceInternal(stanceLink);
      else
        (faction1 as Clan).AddStanceInternal(stanceLink);
    }

    private static void RemoveStanceFromFaction(IFaction faction1, StanceLink stanceLink)
    {
      if (faction1 is Kingdom kingdom)
        kingdom.RemoveStanceInternal(stanceLink);
      else
        (faction1 as Clan).RemoveStanceInternal(stanceLink);
    }

    private static StanceLink SetStance(
      IFaction faction1,
      IFaction faction2,
      StanceType stanceType)
    {
      StanceLink stanceLinkInternal = FactionManager.Instance.GetStanceLinkInternal(faction1, faction2);
      stanceLinkInternal.StanceType = stanceType;
      return stanceLinkInternal;
    }

    public static void DeclareAlliance(IFaction faction1, IFaction faction2)
    {
      if (faction1 == faction2 || faction1.IsBanditFaction || faction2.IsBanditFaction)
        return;
      FactionManager.SetStance(faction1, faction2, StanceType.Neutral);
    }

    public static void DeclareWar(IFaction faction1, IFaction faction2, bool isAtConstantWar = false)
    {
      if (faction1 == faction2 || faction1.IsBanditFaction || faction2.IsBanditFaction)
        return;
      FactionManager.SetStance(faction1, faction2, StanceType.War).IsAtConstantWar = isAtConstantWar;
    }

    public static void SetNeutral(IFaction faction1, IFaction faction2)
    {
      if (faction1 == faction2 || faction1.IsBanditFaction || faction2.IsBanditFaction)
        return;
      FactionManager.Instance.GetStanceLinkInternal(faction1, faction2).StanceType = StanceType.Neutral;
    }

    public static bool IsAtWarAgainstFaction(IFaction faction1, IFaction faction2)
    {
      if (faction1 == null || faction2 == null || faction1 == faction2)
        return false;
      if (faction1.IsBanditFaction && !faction2.IsBanditFaction && !faction2.IsOutlaw || faction2.IsBanditFaction && !faction1.IsBanditFaction && !faction1.IsOutlaw)
        return true;
      StanceLink stance = FactionManager.Instance._stances.GetStance(faction1, faction2);
      return stance != null && stance.IsAtWar;
    }

    public static bool IsAlliedWithFaction(IFaction faction1, IFaction faction2)
    {
      if (faction1 == null || faction2 == null)
        return false;
      if (faction1 == faction2)
        return true;
      StanceLink stance = FactionManager.Instance._stances.GetStance(faction1, faction2);
      return stance != null && stance.IsAllied;
    }

    public static bool IsNeutralWithFaction(IFaction faction1, IFaction faction2)
    {
      if (faction1 == null || faction2 == null || faction1 == faction2)
        return false;
      StanceLink stance = FactionManager.Instance._stances.GetStance(faction1, faction2);
      if (stance == null && (faction1.IsBanditFaction && !faction2.IsBanditFaction && !faction2.IsOutlaw || faction2.IsBanditFaction && !faction1.IsBanditFaction && !faction1.IsOutlaw))
        return false;
      return stance == null || stance.IsNeutral;
    }

    public static void SetStanceTwoSided(IFaction faction1, IFaction faction2, int value)
    {
      bool flag1 = FactionManager.IsAtWarAgainstFaction(faction1, faction2);
      StanceType stanceType = value < 0 ? StanceType.War : (value > 0 ? StanceType.Alliance : StanceType.Neutral);
      FactionManager.SetStance(faction1, faction2, stanceType);
      bool flag2 = FactionManager.IsAtWarAgainstFaction(faction1, faction2);
      if (faction1 == Hero.MainHero.MapFaction)
      {
        if (!flag1 & flag2)
        {
          foreach (MobileParty mobileParty in BOFCampaign.Current.MobileParties)
          {
            IFaction mapFaction = mobileParty.MapFaction;
          }
        }
        else
        {
          if (!flag1 || flag2)
            return;
          foreach (MobileParty mobileParty in BOFCampaign.Current.MobileParties)
          {
            if (mobileParty.MapFaction == faction2)
              mobileParty.Party.Visuals.SetMapIconAsDirty();
          }
        }
      }
      else
      {
        if (faction2 != Hero.MainHero.MapFaction)
          return;
        if (!flag1 & flag2)
        {
          foreach (MobileParty mobileParty in BOFCampaign.Current.MobileParties)
          {
            IFaction mapFaction = mobileParty.MapFaction;
          }
        }
        else
        {
          if (!flag1 || flag2)
            return;
          foreach (MobileParty mobileParty in BOFCampaign.Current.MobileParties)
          {
            if (mobileParty.MapFaction == faction1)
              mobileParty.Party.Visuals.SetMapIconAsDirty();
          }
        }
      }
    }

    internal void RemoveFactionsFromCampaignWars(IFaction faction1)
    {
      if (faction1.MapFaction != faction1)
        return;
      foreach (StanceLink stance in faction1.Stances.ToArray<StanceLink>())
        this.RemoveStance(stance);
    }

    public List<StanceLink> FindCampaignWarsOfFaction(IFaction faction) => faction.Stances.Where<StanceLink>((Func<StanceLink, bool>) (x => x.IsAtWar)).ToList<StanceLink>();

    public static IEnumerable<StanceLink> GetStanceLinksOf(IFaction faction)
    {
      foreach (StanceLink stanceLink in FactionManager.Instance._stances.GetStanceLinks())
      {
        if (stanceLink.Faction1 == faction || stanceLink.Faction2 == faction)
          yield return stanceLink;
      }
    }

    public static IEnumerable<IFaction> GetEnemyFactions(IFaction faction)
    {
      foreach (StanceLink stanceLink in FactionManager.Instance._stances.GetStanceLinks())
      {
        if (stanceLink.IsAtWar)
        {
          IFaction faction1 = (IFaction) null;
          if (stanceLink.Faction1 == faction)
            faction1 = stanceLink.Faction2;
          else if (stanceLink.Faction2 == faction)
            faction1 = stanceLink.Faction1;
          if (faction1 != null && faction1.IsMapFaction && !faction1.IsBanditFaction)
            yield return faction1;
        }
      }
    }

    public static IEnumerable<Kingdom> GetEnemyKingdoms(Kingdom faction)
    {
      foreach (StanceLink stanceLink in FactionManager.Instance._stances.GetStanceLinks())
      {
        if (stanceLink.IsAtWar)
        {
          IFaction faction1 = (IFaction) null;
          if (stanceLink.Faction1 == faction)
            faction1 = stanceLink.Faction2;
          else if (stanceLink.Faction2 == faction)
            faction1 = stanceLink.Faction1;
          if (faction1 != null && faction1.IsKingdomFaction)
            yield return faction1 as Kingdom;
        }
      }
    }

    public static int GetRelationBetweenClans(Clan clan1, Clan clan2)
    {
      float num1 = 0.0f;
      float num2 = 1E-05f;
      if (!clan1.Lords.Any<Hero>() && clan1.IsBanditFaction && !clan2.IsBanditFaction || !clan2.Lords.Any<Hero>() && clan2.IsBanditFaction && !clan1.IsBanditFaction)
        return -10;
      foreach (Hero lord1 in clan1.Lords)
      {
        if ((double) lord1.Age > (double) BOFCampaign.Current.Models.AgeModel.HeroComesOfAge)
        {
          foreach (Hero lord2 in clan2.Lords)
          {
            if ((double) lord2.Age > (double) BOFCampaign.Current.Models.AgeModel.HeroComesOfAge)
            {
              float num3 = 0.1f;
              if (lord1 == clan1.Leader)
                num3 += 0.2f;
              else if (lord1 == clan1.Leader?.Spouse)
                num3 += 0.05f;
              if (lord2 == clan2.Leader)
                num3 += 0.2f;
              else if (lord2 == clan2.Leader?.Spouse)
                num3 += 0.05f;
              if (lord1 == clan1.Leader && lord2 == clan2.Leader)
                num3 *= 20f;
              int baseHeroRelation = lord1.GetBaseHeroRelation(lord2);
              num1 += num3 * (float) baseHeroRelation;
              num2 += num3;
            }
          }
        }
      }
      return (int) ((double) num1 / (double) num2);
    }

    public struct FactionPairObsolete
    {
      public static void AutoGeneratedStaticCollectObjectsFactionPairObsolete(
        object o,
        List<object> collectedObjects)
      {
        ((FactionManager.FactionPairObsolete) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
      }

      private void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
      {
        collectedObjects.Add((object) this.Faction1);
        collectedObjects.Add((object) this.Faction2);
      }

      internal static object AutoGeneratedGetMemberValueFaction1(object o) => (object) ((FactionManager.FactionPairObsolete) o).Faction1;

      internal static object AutoGeneratedGetMemberValueFaction2(object o) => (object) ((FactionManager.FactionPairObsolete) o).Faction2;

      // [SaveableProperty(1)]
      public IFaction Faction1 { get; private set; }

      // [SaveableProperty(2)]
      public IFaction Faction2 { get; private set; }
    }
  }
}