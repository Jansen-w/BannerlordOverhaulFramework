using System.Collections.Generic;
using BOF.Campaign.Character;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace BOF.Campaign.Party.Roster
{
  public struct TroopRosterElement : ISerializableObject
  {
    private int _number;
    private int _woundedNumber;
    private int _xp;
    public CharacterObject Character;

    public int Number
    {
      get => this._number;
      set => this._number = value >= 0 ? value : throw new MBUnderFlowException("ItemRosterElement::Character");
    }

    public int WoundedNumber
    {
      get
      {
        if (!this.Character.IsHero)
          return this._woundedNumber;
        return !this.Character.HeroObject.IsWounded ? 0 : 1;
      }
      set => this._woundedNumber = value >= 0 ? value : throw new MBUnderFlowException("ItemRosterElement::WoundedNumber");
    }

    public int Xp
    {
      get => this._xp;
      set
      {
        if (value < 0)
          this._xp = 0;
        else
          this._xp = value;
      }
    }

    public int TempXp { get; set; }

    public TroopRosterElement(CharacterObject character)
    {
      this.Character = character;
      this._number = 0;
      this._woundedNumber = 0;
      this._xp = 0;
      this.TempXp = 0;
    }

    public void Clear()
    {
      this.Character = (CharacterObject) null;
      this._number = 0;
    }

    void ISerializableObject.SerializeTo(IWriter writer)
    {
      writer.WriteUInt(this.Character != null ? this.Character.Id.InternalValue : 0U);
      writer.WriteInt(this._number);
      writer.WriteInt(this._woundedNumber);
      writer.WriteInt(this._xp);
    }

    void ISerializableObject.DeserializeFrom(IReader reader)
    {
      uint id = reader.ReadUInt();
      this.Character = (CharacterObject) null;
      if (id != 0U)
        this.Character = MBObjectManager.Instance.GetObject(new MBGUID(id)) as CharacterObject;
      this._number = reader.ReadInt();
      this._woundedNumber = reader.ReadInt();
    }

    public override string ToString() => this.Number.ToString() + " " + this.Character?.ToString();

    public override bool Equals(object obj) => obj is TroopRosterElement other && this.Equals(other);

    public bool Equals(TroopRosterElement other) => this.Character == other.Character;

    public override int GetHashCode() => this.Character == null ? 0 : this.Character.GetHashCode();
  }
}