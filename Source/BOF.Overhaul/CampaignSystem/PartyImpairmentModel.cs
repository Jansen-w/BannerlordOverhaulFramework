using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BOF.Overhaul.CampaignSystem
{
    public abstract class PartyImpairmentModel : GameModel
    {
        public abstract float GetDisorganizedStateDuration(MobileParty party, bool isSiegeOrRaid);

        public abstract float GetVulnerabilityStateDuration(PartyBase party);

        public abstract float GetSiegeExpectedVulnerabilityTime();
    }
}