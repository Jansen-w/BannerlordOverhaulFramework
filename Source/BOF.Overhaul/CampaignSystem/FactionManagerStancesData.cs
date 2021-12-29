using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BOF.Overhaul.CampaignSystem
{
    public class FactionManagerStancesData
    {
        // [SaveableField(10)]
        private Dictionary<(IFaction, IFaction), StanceLink> _stances = new Dictionary<(IFaction, IFaction), StanceLink>();

        public Dictionary<(IFaction, IFaction), StanceLink>.ValueCollection GetStanceLinks() => this._stances.Values;

        public StanceLink GetStance(IFaction faction1, IFaction faction2)
        {
            StanceLink stanceLink;
            this._stances.TryGetValue(this.GetKey(faction1, faction2), out stanceLink);
            return stanceLink;
        }

        public void AddStance(StanceLink stance)
        {
            (IFaction, IFaction) key = this.GetKey(stance);
            if (this._stances.ContainsKey(key))
                this._stances[key] = stance;
            else
                this._stances.Add(key, stance);
        }

        public void RemoveStance(StanceLink stance)
        {
            (IFaction, IFaction) key = this.GetKey(stance);
            if (!this._stances.ContainsKey(key))
                return;
            this._stances.Remove(key);
        }

        private (IFaction, IFaction) GetKey(IFaction faction1, IFaction faction2) => faction1.Id < faction2.Id ? (faction1, faction2) : (faction2, faction1);

        private (IFaction, IFaction) GetKey(StanceLink stance) => this.GetKey(stance.Faction1, stance.Faction2);
    }
}