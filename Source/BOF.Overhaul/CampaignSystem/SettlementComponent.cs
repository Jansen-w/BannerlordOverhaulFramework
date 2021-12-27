using System.Collections.Generic;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace BOF.CampaignSystem.CampaignSystem
{
  public abstract class SettlementComponent : MBObjectBase
  {
    // [SaveableField(56)]
    public bool IsTaken;
    private PartyBase _owner;
    // [SaveableField(58)]
    private Settlement _tradeBound;

    // [SaveableProperty(50)]
    public int Gold { get; private set; }

    public virtual SettlementComponent.ProsperityLevel GetProsperityLevel() => SettlementComponent.ProsperityLevel.Mid;

    public float BackgroundCropPosition { get; protected set; }

    public string BackgroundMeshName { get; protected set; }

    public string WaitMeshName { get; protected set; }

    public string CastleBackgroundMeshName { get; protected set; }

    public PartyBase Owner
    {
      get => this._owner;
      internal set
      {
        if (this._owner == value)
          return;
        if (this._owner != null)
          this._owner.ItemRoster.RosterUpdatedEvent -= new ItemRoster.RosterUpdatedEventDelegate(this.OnInventoryUpdated);
        this._owner = value;
        if (this._owner == null)
          return;
        this._owner.ItemRoster.RosterUpdatedEvent += new ItemRoster.RosterUpdatedEventDelegate(this.OnInventoryUpdated);
      }
    }

    public Settlement Settlement => this._owner.Settlement;

    protected abstract void OnInventoryUpdated(ItemRosterElement item, int count);

    public TextObject Name => this.Owner.Name;

    public Settlement TradeBound
    {
      get => this._tradeBound;
      internal set => this._tradeBound = value;
    }

    // [SaveableProperty(80)]
    public bool IsOwnerUnassigned { get; set; }

    public virtual void OnPartyEntered(MobileParty mobileParty) => mobileParty.Ai.SetAIState(AIState.Undefined);

    public virtual void OnPartyLeft(MobileParty mobileParty)
    {
    }

    public virtual void OnStart()
    {
    }

    public virtual void OnInit()
    {
    }

    public virtual void OnFinishLoadState()
    {
      if (!this.IsTaken || this.Settlement.Town?.LastCapturedBy == Clan.PlayerClan)
        return;
      this.IsTaken = false;
    }

    public void ChangeGold(int changeAmount)
    {
      this.Gold += changeAmount;
      if (this.Gold >= 0)
        return;
      this.Gold = 0;
    }

    public int GetNumberOfTroops()
    {
      int num = 0;
      foreach (MobileParty party in this.Owner.Settlement.Parties)
      {
        if (party.IsMilitia || party.IsGarrison)
          num += party.Party.NumberOfAllMembers;
      }
      return num;
    }

    public override void Deserialize(MBObjectManager objectManager, XmlNode node) => base.Deserialize(objectManager, node);

    public virtual int GetItemPrice(ItemObject item, MobileParty tradingParty = null, bool isSelling = false) => 0;

    public virtual int GetItemPrice(
      EquipmentElement itemRosterElement,
      MobileParty tradingParty = null,
      bool isSelling = false)
    {
      return 0;
    }

    public virtual bool IsTown => false;

    public virtual bool IsCastle => false;

    public virtual void OnRelatedPartyRemoved(MobileParty mobileParty)
    {
    }

    public List<CharacterObject> GetPrisonerHeroes()
    {
      List<PartyBase> partyBaseList = new List<PartyBase>()
      {
        this.Owner
      };
      foreach (MobileParty party in this.Owner.Settlement.Parties)
      {
        if (party.IsCommonAreaParty || party.IsGarrison)
          partyBaseList.Add(party.Party);
      }
      List<CharacterObject> characterObjectList = new List<CharacterObject>();
      foreach (PartyBase partyBase in partyBaseList)
      {
        for (int index1 = 0; index1 < partyBase.PrisonRoster.Count; ++index1)
        {
          for (int index2 = 0; index2 < partyBase.PrisonRoster.GetElementNumber(index1); ++index2)
          {
            CharacterObject character = partyBase.PrisonRoster.GetElementCopyAtIndex(index1).Character;
            if (character.IsHero)
              characterObjectList.Add(character);
          }
        }
      }
      return characterObjectList;
    }

    public enum ProsperityLevel
    {
      Low,
      Mid,
      High,
      NumberOfLevels,
    }
  }
}