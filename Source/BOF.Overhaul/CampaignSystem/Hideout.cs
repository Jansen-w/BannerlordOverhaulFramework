using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace BOF.Overhaul.CampaignSystem
{
  public class Hideout : SettlementComponent
  {
    // [SaveableField(200)]
    private CampaignTime _nextPossibleAttackTime;
    // [SaveableField(201)]
    public bool IsSpotted;

    public CampaignTime NextPossibleAttackTime => this._nextPossibleAttackTime;

    public static IReadOnlyList<Hideout> All => BOFCampaign.Current.AllHideouts;

    public void UpdateNextPossibleAttackTime() => this._nextPossibleAttackTime = CampaignTime.Now + CampaignTime.Hours(12f);

    public bool IsInfested => this.Owner.Settlement.Parties.CountQ<MobileParty>((Func<MobileParty, bool>) (x => x.IsBandit)) >= BOFCampaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;

    public string SceneName { get; private set; }

    public IFaction MapFaction
    {
      get
      {
        foreach (MobileParty party in this.Settlement.Parties)
        {
          if (party.IsBandit)
            return (IFaction) party.ActualClan;
        }
        foreach (Clan clan in Clan.All)
        {
          if (clan.IsBanditFaction)
            return (IFaction) clan;
        }
        return (IFaction) null;
      }
    }

    public void SetScene(string sceneName) => this.SceneName = sceneName;

    public Hideout() => this.IsSpotted = false;

    public override void OnPartyEntered(MobileParty mobileParty)
    {
      base.OnPartyEntered(mobileParty);
      this.UpdateOwnership();
      if (!mobileParty.MapFaction.IsBanditFaction)
        return;
      mobileParty.BanditPartyComponent.SetHomeHideout(this.Owner.Settlement.Hideout);
    }

    public override void OnPartyLeft(MobileParty mobileParty)
    {
      this.UpdateOwnership();
      if (this.Owner.Settlement.Parties.Count != 0)
        return;
      this.OnHideoutIsEmpty();
    }

    public override void OnRelatedPartyRemoved(MobileParty mobileParty)
    {
      if (this.Owner.Settlement.Parties.Count != 0)
        return;
      this.OnHideoutIsEmpty();
    }

    private void OnHideoutIsEmpty()
    {
      this.IsSpotted = false;
      this.Owner.Settlement.IsVisible = false;
      CampaignEventDispatcher.Instance.OnHideoutDeactivated(this.Settlement);
    }

    public override void OnStart() => this.Owner.Settlement.Hideout = this;

    public override void OnInit() => this.Owner.Settlement.IsVisible = false;

    public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    {
      this.BackgroundCropPosition = float.Parse(node.Attributes["background_crop_position"].Value);
      this.BackgroundMeshName = node.Attributes["background_mesh"].Value;
      this.WaitMeshName = node.Attributes["wait_mesh"].Value;
      base.Deserialize(objectManager, node);
      if (node.Attributes["scene_name"] == null)
        return;
      this.SceneName = node.Attributes["scene_name"].InnerText;
    }

    private void UpdateOwnership()
    {
      if (this.Owner.MemberRoster.Count != 0 && !this.Owner.Settlement.Parties.All<MobileParty>((Func<MobileParty, bool>) (x => x.Party.Owner != this.Owner.Owner)))
        return;
      this.Owner.Settlement.Party.Visuals.SetMapIconAsDirty();
    }

    [CommandLineFunctionality.CommandLineArgumentFunction("show_hideouts", "campaign")]
    public static string ShowHideouts(List<string> strings)
    {
      if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
        return CampaignCheats.ErrorType;
      int result;
      if (!CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings) || !int.TryParse(strings[0], out result) || result != 1 && result != 2)
        return "Format is \"campaign.show_hideouts [1/2]\n 1: Show infested hideouts\n2: Show all hideouts\".";
      foreach (Settlement settlement in Settlement.All)
      {
        if (settlement.IsHideout && (result != 1 || settlement.Hideout.IsInfested))
        {
          Hideout component = settlement.GetComponent<Hideout>();
          component.IsSpotted = true;
          component.Owner.Settlement.IsVisible = true;
        }
      }
      return (result == 1 ? "Infested" : "All") + " hideouts is visible now.";
    }

    [CommandLineFunctionality.CommandLineArgumentFunction("hide_hideouts", "campaign")]
    public static string HideHideouts(List<string> strings)
    {
      if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
        return CampaignCheats.ErrorType;
      foreach (Settlement settlement in Settlement.All)
      {
        if (settlement.IsHideout)
        {
          Hideout component = settlement.GetComponent<Hideout>();
          component.IsSpotted = false;
          component.Owner.Settlement.IsVisible = false;
        }
      }
      return "All hideouts should be invisible now.";
    }

    protected override void OnInventoryUpdated(ItemRosterElement item, int count)
    {
    }
  }
}