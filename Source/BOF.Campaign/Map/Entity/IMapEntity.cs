using BOF.Campaign.Faction;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BOF.Campaign.Map.Entity
{
    public interface IMapEntity
    {
        Vec2 InteractionPosition { get; }

        TextObject Name { get; }

        bool OnMapClick(bool followModifierUsed);

        void OnHover();

        void OnOpenEncyclopedia();

        bool IsMobileEntity { get; }

        IMapEntity AttachedEntity { get; }

        IPartyVisual PartyVisual { get; }

        bool ShowCircleAroundEntity { get; }

        bool IsMainEntity();

        bool IsEnemyOf(IFaction faction);

        bool IsAllyOf(IFaction faction);
    }
}