using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BOF.CampaignSystem.CampaignSystem
{
    public abstract class CharacterStatsModel : GameModel
    {
        public abstract ExplainedNumber MaxHitpoints(
            CharacterObject character,
            bool includeDescriptions = false);
    }
}