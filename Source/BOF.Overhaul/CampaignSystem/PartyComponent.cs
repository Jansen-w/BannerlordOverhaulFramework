using TaleWorlds.Localization;

namespace BOF.Overhaul.CampaignSystem
{
    public abstract class PartyComponent
    {
        public MobileParty MobileParty { get; private set; }

        public PartyBase Party => this.MobileParty.Party;

        public abstract Hero PartyOwner { get; }

        public abstract TextObject Name { get; }

        public abstract Settlement HomeSettlement { get; }

        public virtual Hero Leader => (Hero) null;

        public void SetMobilePartyInternal(MobileParty party) => this.MobileParty = party;

        public void Initialize(MobileParty party)
        {
            this.SetMobilePartyInternal(party);
            this.OnInitialize();
        }

        public void Finish() => this.OnFinalize();

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnFinalize()
        {
        }

        public virtual void ClearCachedName()
        {
        }

        public virtual void ChangePartyLeader(Hero newLeader)
        {
        }

        public virtual void GetMountAndHarnessVisualIdsForPartyIcon(
            PartyBase party,
            out string mountStringId,
            out string harnessStringId)
        {
            mountStringId = "";
            harnessStringId = "";
        }

        public delegate void OnPartyComponentCreatedDelegate(MobileParty mobileParty);
    }
}