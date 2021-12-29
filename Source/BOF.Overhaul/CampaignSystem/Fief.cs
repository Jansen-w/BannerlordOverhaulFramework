using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BOF.Overhaul.CampaignSystem
{
  public abstract class Fief : SettlementComponent
  {
    // [CachedData]
    public GarrisonPartyComponent GarrisonPartyComponent;

    public float Prosperity => this.Owner.Settlement.Prosperity;

    // [SaveableProperty(100)]
    public float FoodStocks { get; set; }

    public float Militia => this.Owner.Settlement.Militia;

    public MobileParty GarrisonParty => this.GarrisonPartyComponent?.MobileParty;
  }
}