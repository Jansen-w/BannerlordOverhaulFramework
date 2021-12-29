using TaleWorlds.Library;

namespace BOF.Overhaul.CampaignSystem
{
    public interface ILocatable<T>
    {
        // [CachedData]
        int LocatorNodeIndex { get; set; }

        // [CachedData]
        T NextLocatable { get; set; }

        // [CachedData]
        Vec2 GetPosition2D { get; }
    }
}