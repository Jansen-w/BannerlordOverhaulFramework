namespace BOF.Overhaul.CampaignSystem
{
    public abstract class WarPartyComponent : PartyComponent
    {
        public Clan Clan => this.Party.MobileParty.ActualClan;

        protected override void OnInitialize() => this.Clan.AddWarPartyInternal(this);

        protected override void OnFinalize() => this.Clan.RemoveWarPartyInternal(this);

        public void OnClanChange(Clan oldClan, Clan newClan)
        {
            oldClan.RemoveWarPartyInternal(this);
            newClan.AddWarPartyInternal(this);
        }
    }
}