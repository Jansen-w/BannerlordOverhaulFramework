using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BOF.Campaign.Utility;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace BOF.Campaign.Party.Roster
{
  public class TroopRoster : ISerializableObject
  {
    [CachedData]
    private bool _areCountsReady;

    public int _count;

    private bool _isPrisonRoster;

    [CachedData]
    private int _totalHeroes;

    [CachedData]
    private int _totalRegulars;

    [CachedData]
    private int _totalWoundedHeroes;

    [CachedData]
    private int _totalWoundedRegulars;

    [CachedData]
    private List<TroopRosterElement> _troopRosterElements;

    [CachedData]
    private int _troopRosterElementsVersion;

    public TroopRosterElement[] data;

    public TroopRoster(PartyBase ownerParty)
    {
      this.OwnerParty = ownerParty;
      this.data = (TroopRosterElement[]) null;
      this._count = 0;
      this._troopRosterElements = new List<TroopRosterElement>();
    }

    private TroopRoster()
    {
      this.data = (TroopRosterElement[]) null;
      this._count = 0;
      this._troopRosterElements = new List<TroopRosterElement>();
    }


    [CachedData]
    public NumberChangedCallback NumberChangedCallback { get; set; }

    [SaveableProperty(2)]
    public PartyBase OwnerParty { get; private set; }

    public int Count => this._count;

    [CachedData]
    public int VersionNo { get; private set; }

    public int TotalRegulars
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalRegulars;
      }
    }

    public int TotalWoundedRegulars
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalWoundedRegulars;
      }
    }

    public int TotalWoundedHeroes
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalWoundedHeroes;
      }
    }

    public int TotalHeroes
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalHeroes;
      }
    }

    public int TotalWounded
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalWoundedRegulars + this._totalWoundedHeroes;
      }
    }

    public int TotalManCount
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalRegulars + this._totalHeroes;
      }
    }

    public int TotalHealthyCount
    {
      get
      {
        if (!this._areCountsReady)
          this.CalculateCounts();
        return this._totalRegulars + this._totalHeroes - (this._totalWoundedRegulars + this._totalWoundedHeroes);
      }
    }

    public bool IsPrisonRoster
    {
      get => this._isPrisonRoster;
      set => this._isPrisonRoster = value;
    }

    void ISerializableObject.SerializeTo(IWriter writer)
    {
      writer.WriteInt(this.Count);
      writer.WriteInt(this.VersionNo);
      if (this.data != null)
      {
        writer.WriteInt(this.data.Length);
        foreach (TroopRosterElement troopRosterElement in this.data)
          writer.WriteSerializableObject((ISerializableObject) troopRosterElement);
      }
      else
        writer.WriteInt(0);
    }

    void ISerializableObject.DeserializeFrom(IReader reader)
    {
      this._count = reader.ReadInt();
      this.VersionNo = reader.ReadInt();
      int length = reader.ReadInt();
      this.data = new TroopRosterElement[length];
      for (int index = 0; index < length; ++index)
        this.data[index] = (TroopRosterElement) reader.ReadSerializableObject();
    }

    public static TroopRoster CreateDummyTroopRoster() => new TroopRoster();

    public override int GetHashCode() => base.GetHashCode();

    [LoadInitializationCallback]
    private void OnLoad(MetaData metaData)
    {
      this._troopRosterElementsVersion = -1;
      this._troopRosterElements = new List<TroopRosterElement>();
    }

    private void EnsureLength(int length)
    {
      if (length <= 0 || this.data != null && length <= this.data.Length)
        return;
      int length1 = 4;
      if (this.data != null)
        length1 = this.data.Length * 2;
      TroopRosterElement[] troopRosterElementArray = new TroopRosterElement[length1];
      for (int index = 0; index < this._count; ++index)
        troopRosterElementArray[index] = this.data[index];
      this.data = troopRosterElementArray;
    }

    public void PreAfterLoad() => this.CalculateCounts();

    private void CalculateCounts()
    {
      int num1 = 0;
      int num2 = 0;
      int num3 = 0;
      int num4 = 0;
      for (int index = 0; index < this._count; ++index)
      {
        TroopRosterElement troopRosterElement = this.data[index];
        if (troopRosterElement.Character.IsHero)
        {
          ++num1;
          if (troopRosterElement.Character.HeroObject.IsWounded)
            ++num2;
        }
        else
        {
          num3 += this.data[index].Number;
          num4 += this.data[index].WoundedNumber;
        }
      }
      this._totalWoundedHeroes = num2;
      this._totalWoundedRegulars = num4;
      this._totalHeroes = num1;
      this._totalRegulars = num3;
      this._areCountsReady = true;
    }

    public FlattenedTroopRoster ToFlattenedRoster() => new FlattenedTroopRoster(this.TotalManCount)
    {
      this.GetTroopRoster()
    };

    public void Add(
      IEnumerable<FlattenedTroopRosterElement> elementList)
    {
      foreach (FlattenedTroopRosterElement element in elementList)
        this.AddToCounts(element.Troop, 1, woundedCount: (element.IsWounded ? 1 : 0), xpChange: element.Xp);
    }

    public void Add(TroopRoster troopRoster)
    {
      foreach (TroopRosterElement troopRosterElement in troopRoster.GetTroopRoster())
        this.Add(troopRosterElement);
    }

    private void Add(TroopRosterElement troopRosterElement) => this.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, woundedCount: troopRosterElement.WoundedNumber, xpChange: troopRosterElement.Xp);

    public ICollection<TroopRosterElement> RemoveIf(
      Predicate<TroopRosterElement> match)
    {
      List<TroopRosterElement> troopRosterElementList = new List<TroopRosterElement>();
      bool flag = false;
      for (int index = 0; index < this._count; ++index)
      {
        if (match(this.data[index]))
        {
          TroopRosterElement elementCopyAtIndex = this.GetElementCopyAtIndex(index);
          troopRosterElementList.Add(elementCopyAtIndex);
          this.AddToCountsAtIndex(index, -elementCopyAtIndex.Number, -elementCopyAtIndex.WoundedNumber, -elementCopyAtIndex.Xp);
          --index;
          flag = true;
        }
      }
      if (flag)
        this.UpdateVersion();
      return (ICollection<TroopRosterElement>) troopRosterElementList;
    }

    public int FindIndexOfTroop(CharacterObject character)
    {
      for (int index = 0; index < this._count; ++index)
      {
        if (this.data[index].Character == character)
          return index;
      }
      return -1;
    }

    public CharacterObject GetManAtIndexFromFlattenedRosterWithFilter(
      int indexOfTroop,
      bool includeHeroes = false,
      bool countOnlyHealthyOnes = false)
    {
      for (int index = 0; index < this._count; ++index)
      {
        if (includeHeroes || !this.data[index].Character.IsHero)
        {
          indexOfTroop -= countOnlyHealthyOnes ? this.data[index].Number - this.data[index].WoundedNumber : this.data[index].Number;
          if (indexOfTroop < 0)
            return this.data[index].Character;
        }
      }
      return (CharacterObject) null;
    }

    private bool KillOneManRandomly(bool includeHeroes = false)
    {
      CharacterObject character = (CharacterObject) null;
      int maxValue = includeHeroes ? this.TotalManCount - this.TotalWounded : this._totalRegulars - this._totalWoundedRegulars;
      bool flag = maxValue > 0;
      while (flag)
      {
        flag = false;
        character = this.GetManAtIndexFromFlattenedRosterWithFilter(MBRandom.RandomInt(maxValue), includeHeroes, true);
        if (character == null || !includeHeroes && character.IsHero)
          flag = true;
      }
      if (character == null)
        return false;
      if (character.IsHero)
      {
        if (character.HeroObject.IsWanderer)
          MakeHeroFugitiveAction.Apply(character.HeroObject);
        else
          character.HeroObject.ChangeState(Hero.CharacterStates.Fugitive);
      }
      this.AddToCounts(character, -1);
      return true;
    }

    public void KillNumberOfMenRandomly(int numberOfMen, bool includeHeroes)
    {
      bool flag = true;
      for (int index = 0; index < numberOfMen & flag; ++index)
        flag = this.KillOneManRandomly(includeHeroes);
    }

    public void WoundNumberOfTroopsRandomly(int numberOfMen)
    {
      for (int index = 0; index < numberOfMen; ++index)
      {
        CharacterObject troop = (CharacterObject) null;
        int maxValue = this._totalRegulars - this._totalWoundedRegulars;
        bool flag = maxValue > 0;
        while (flag)
        {
          flag = false;
          troop = this.GetManAtIndexFromFlattenedRosterWithFilter(MBRandom.RandomInt(maxValue), true);
          if (troop == null || troop.IsHero)
            flag = true;
        }
        if (troop != null)
          this.WoundTroop(troop);
      }
    }

    public int AddToCountsAtIndex(
      int index,
      int countChange,
      int woundedCountChange = 0,
      int xpChange = 0,
      bool removeDepleted = true)
    {
      this.UpdateVersion();
      bool heroCountChanged = false;
      CharacterObject character = this.data[index].Character;
      bool isHero = character.IsHero;
      this.data[index].Number += countChange;
      int num1 = isHero ? 1 : 0;
      int num2 = this.data[index].WoundedNumber + woundedCountChange;
      if (num2 > this.data[index].Number)
        woundedCountChange += this.data[index].Number - num2;
      this.data[index].WoundedNumber += woundedCountChange;
      if (xpChange != 0)
        this.data[index].Xp += xpChange;
      if (this.IsPrisonRoster)
        this.ClampConformity(index);
      else
        this.ClampXp(index);
      if (isHero)
      {
        this._totalHeroes += countChange;
        if (character.HeroObject.IsWounded)
          this._totalWoundedHeroes += countChange;
        if (countChange != 0)
          heroCountChanged = true;
      }
      else
      {
        this._totalWoundedRegulars += woundedCountChange;
        this._totalRegulars += countChange;
      }
      if (removeDepleted && this.data[index].Number == 0)
      {
        this.RemoveRange(index, index + 1);
        index = -1;
      }
      if (this.OwnerParty != null & isHero)
      {
        if (countChange > 0)
        {
          if (!this.IsPrisonRoster)
            this.OwnerParty.OnHeroAdded(character.HeroObject);
          else
            this.OwnerParty.OnHeroAddedAsPrisoner(character.HeroObject);
        }
        else if (countChange < 0)
        {
          if (!this.IsPrisonRoster)
            this.OwnerParty.OnHeroRemoved(character.HeroObject);
          else
            this.OwnerParty.OnHeroRemovedAsPrisoner(character.HeroObject);
        }
      }
      if (countChange != 0 || woundedCountChange != 0)
        this.OnNumberChanged((uint) countChange > 0U, (uint) woundedCountChange > 0U, heroCountChanged);
      int num3 = removeDepleted ? 1 : 0;
      return index;
    }

    private void RemoveRange(int p, int p2)
    {
      int num = p2 - p;
      for (int index = p2; index < this._count; ++index)
        this.data[index - num] = this.data[index];
      for (int index = this._count - num; index < this._count; ++index)
        this.data[index].Clear();
      this.UpdateVersion();
      this._count -= num;
    }

    private int AddNewElement(CharacterObject character, bool insertAtFront = false, int insertionIndex = -1)
    {
      int length = this._count + 1;
      this.EnsureLength(length);
      int index = insertionIndex == -1 ? this._count : insertionIndex;
      if (insertAtFront)
        index = 0;
      if (this._count > index)
      {
        for (int count = this._count; count > index; --count)
          this.data[count] = this.data[count - 1];
      }
      this.data[index] = new TroopRosterElement(character);
      this._count = length;
      this.UpdateVersion();
      return index;
    }

    [Conditional("DEBUG_MORE")]
    public void CheckValidity()
    {
      if (this.data == null)
        return;
      int num = 0;
      for (int index = 0; index < this.data.Length; ++index)
      {
        TroopRosterElement troopRosterElement = this.data[index];
        if (troopRosterElement.Character != null)
        {
          int number = troopRosterElement.Number;
          int woundedNumber = troopRosterElement.WoundedNumber;
          ++num;
        }
      }
    }

    private void OnNumberChanged(
      bool numberChanged,
      bool woundedNumberChanged,
      bool heroCountChanged)
    {
      NumberChangedCallback numberChangedCallback = this.NumberChangedCallback;
      if (numberChangedCallback == null)
        return;
      numberChangedCallback(numberChanged, woundedNumberChanged, heroCountChanged);
    }

    public int AddToCounts(
      CharacterObject character,
      int count,
      bool insertAtFront = false,
      int woundedCount = 0,
      int xpChange = 0,
      bool removeDepleted = true,
      int index = -1)
    {
      int index1 = this.FindIndexOfTroop(character);
      if (index1 < 0)
      {
        if (index >= 0)
        {
          if (count + woundedCount <= 0)
            return -1;
          index1 = this.AddNewElement(character, insertAtFront, index);
        }
        else
        {
          if (count + woundedCount <= 0)
            return -1;
          index1 = this.AddNewElement(character, insertAtFront);
        }
      }
      return this.AddToCountsAtIndex(index1, count, woundedCount, xpChange, removeDepleted) != -1 ? index1 : -1;
    }

    public int GetTroopCount(CharacterObject troop)
    {
      int indexOfTroop = this.FindIndexOfTroop(troop);
      return indexOfTroop >= 0 ? this.data[indexOfTroop].Number : 0;
    }

    public void RemoveZeroCounts()
    {
      int index1 = 0;
      for (int index2 = 0; index2 < this._count; ++index2)
      {
        if (this.data[index2].Number > 0)
        {
          if (index1 != index2)
            this.data[index1] = this.data[index2];
          ++index1;
        }
      }
      for (int index3 = index1; index3 < this._count; ++index3)
        this.data[index3].Clear();
      this._count = index1;
      this.UpdateVersion();
    }

    public TroopRosterElement GetElementCopyAtIndex(int index) => this.data[index];

    public void SetElementNumber(int index, int number)
    {
      if (index >= this._count)
        throw new IndexOutOfRangeException();
      this.data[index].Number = number;
      this.UpdateVersion();
    }

    public int GetElementNumber(int index) => index >= 0 && index < this._count ? this.data[index].Number : 0;

    public int GetElementNumber(CharacterObject character) => this.GetElementNumber(this.FindIndexOfTroop(character));

    public void SetElementWoundedNumber(int index, int number)
    {
      if (index >= this._count)
        throw new IndexOutOfRangeException();
      this.data[index].WoundedNumber = number;
      this.UpdateVersion();
    }

    public int GetElementWoundedNumber(int index)
    {
      if (index < this._count)
        return this.data[index].WoundedNumber;
      throw new IndexOutOfRangeException();
    }

    public void SetElementXp(int index, int number)
    {
      if (index >= this._count)
        throw new IndexOutOfRangeException();
      this.data[index].Xp = number;
      this.UpdateVersion();
    }

    public int GetElementXp(int index) => index < this._count && index >= 0 ? this.data[index].Xp : 0;

    public int GetElementXp(CharacterObject character) => this.GetElementXp(this.FindIndexOfTroop(character));

    public CharacterObject GetCharacterAtIndex(int index)
    {
      if (index < this._count)
        return this.data[index].Character;
      throw new IndexOutOfRangeException();
    }

    public void FillMembersOfRoster(int neededNumber, CharacterObject basicTroop = null)
    {
      int count;
      for (int index = this.GetTroopRoster().Where<TroopRosterElement>((Func<TroopRosterElement, bool>) (element => !element.Character.IsHero)).Sum<TroopRosterElement>((Func<TroopRosterElement, int>) (element => element.Number)); index != neededNumber; index += count)
      {
        float num = MBRandom.RandomFloat * (float) index;
        CharacterObject character = basicTroop;
        foreach (TroopRosterElement troopRosterElement in this.GetTroopRoster().Where<TroopRosterElement>((Func<TroopRosterElement, bool>) (element => !element.Character.IsHero)))
        {
          num -= (float) troopRosterElement.Number;
          if ((double) num < 0.0)
          {
            character = troopRosterElement.Character;
            break;
          }
        }
        count = index > neededNumber ? -1 : 1;
        this.AddToCounts(character, count);
      }
    }

    public void WoundMembersOfRoster(float woundedRatio)
    {
      for (int index = 0; index < this.data.Length; ++index)
      {
        TroopRosterElement troopRosterElement = this.data[index];
        if (troopRosterElement.Character != null)
        {
          if (troopRosterElement.Character.IsHero && (double) MBRandom.RandomFloat < (double) woundedRatio)
          {
            this.data[index].Character.HeroObject.MakeWounded();
          }
          else
          {
            int woundedCount = (int) ((double) troopRosterElement.Number * (double) woundedRatio);
            this.AddToCounts(this.data[index].Character, 0, woundedCount: woundedCount);
          }
        }
      }
    }

    public void Reset()
    {
      this.Clear();
      this.UpdateVersion();
    }

    public override bool Equals(object obj) => (object) this == obj;

    public static bool operator ==(TroopRoster a, TroopRoster b)
    {
      if ((object) a == (object) b)
        return true;
      if ((object) a == null || (object) b == null || a.Count != b.Count)
        return false;
      for (int index = 0; index < a.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex1 = a.GetElementCopyAtIndex(index);
        int indexOfTroop = b.FindIndexOfTroop(elementCopyAtIndex1.Character);
        if (indexOfTroop == -1)
          return false;
        TroopRosterElement elementCopyAtIndex2 = b.GetElementCopyAtIndex(indexOfTroop);
        if (elementCopyAtIndex1.Character != elementCopyAtIndex2.Character || elementCopyAtIndex1.Number != elementCopyAtIndex2.Number)
          return false;
      }
      return true;
    }

    public static bool operator !=(TroopRoster a, TroopRoster b) => !(a == b);

    public bool Contains(CharacterObject character)
    {
      for (int index = 0; index < this.Count; ++index)
      {
        TroopRosterElement elementCopyAtIndex = this.GetElementCopyAtIndex(index);
        if (character == elementCopyAtIndex.Character)
          return true;
      }
      return false;
    }

    public List<TroopRosterElement> GetTroopRoster()
    {
      if (this._troopRosterElementsVersion != this.VersionNo)
      {
        this._troopRosterElementsVersion = this.VersionNo;
        this._troopRosterElements = new List<TroopRosterElement>(this.Count);
        for (int index = 0; index < this.Count; ++index)
          this._troopRosterElements.Add(this.GetElementCopyAtIndex(index));
      }
      return this._troopRosterElements;
    }

    public void Clear()
    {
      for (int index = this._count - 1; index >= 0; --index)
        this.AddToCountsAtIndex(index, -this.data[index].Number, -this.data[index].WoundedNumber);
      this.UpdateVersion();
    }

    private void ClampConformity(int index)
    {
      CharacterObject character = this.data[index].Character;
      if (!character.IsHero)
      {
        int maxValue = this.data[index].Number * character.ConformityNeededToRecruitPrisoner;
        int xp = this.data[index].Xp;
        this.data[index].Xp = MBMath.ClampInt(xp, 0, maxValue);
      }
      else
        this.data[index].Xp = Math.Max(this.data[index].Xp, 0);
    }

    private void ClampXp(int index)
    {
      CharacterObject character = this.data[index].Character;
      if (!character.IsHero)
      {
        int num1 = 0;
        for (int index1 = 0; index1 < character.UpgradeTargets.Length; ++index1)
        {
          int upgradeXpCost = character.GetUpgradeXpCost(this.OwnerParty, index1);
          if (num1 < upgradeXpCost)
            num1 = upgradeXpCost;
        }
        int num2 = MBMath.ClampInt(this.data[index].Xp, 0, this.data[index].Number * num1);
        this.data[index].Xp = num2;
      }
      else
        this.data[index].Xp = Math.Max(this.data[index].Xp, 0);
    }

    public int AddXpToTroop(int xpAmount, CharacterObject attackerTroop)
    {
      int indexOfTroop = this.FindIndexOfTroop(attackerTroop);
      return indexOfTroop >= 0 ? this.AddXpToTroopAtIndex(xpAmount, indexOfTroop) : 0;
    }

    public int AddXpToTroopAtIndex(int xpAmount, int index)
    {
      int xp = this.data[index].Xp;
      this.data[index].Xp += xpAmount;
      if (this.IsPrisonRoster)
        this.ClampConformity(index);
      else
        this.ClampXp(index);
      return this.data[index].Xp - xp;
    }

    public void RemoveTroop(
      CharacterObject troop,
      int numberToRemove = 1,
      UniqueTroopDescriptor troopSeed = default (UniqueTroopDescriptor),
      int xp = 0)
    {
      int indexOfTroop = this.FindIndexOfTroop(troop);
      bool removeDepleted = true;
      if (PlayerEncounter.CurrentBattleSimulation != null && !troop.IsHero)
        removeDepleted = false;
      this.AddToCountsAtIndex(indexOfTroop, -numberToRemove, xpChange: (troop.IsHero ? 0 : -xp), removeDepleted: removeDepleted);
    }

    public void WoundTroop(
      CharacterObject troop,
      int numberToWound = 1,
      UniqueTroopDescriptor troopSeed = default (UniqueTroopDescriptor))
    {
      this.AddToCountsAtIndex(this.FindIndexOfTroop(troop), 0, numberToWound);
    }

    public void SlideTroops(int firstTroopIndex, int newIndex)
    {
      if (firstTroopIndex == -1 || newIndex == -1 || firstTroopIndex == newIndex)
        return;
      if (newIndex >= this.data.Length)
        this.EnsureLength(newIndex + 1);
      TroopRosterElement troopRosterElement1 = this.data[firstTroopIndex];
      TroopRosterElement troopRosterElement2 = this.data[newIndex];
      if (firstTroopIndex > newIndex)
      {
        for (int index = firstTroopIndex - 1; index > newIndex; --index)
          this.data[index + 1] = this.data[index];
        this.data[newIndex] = troopRosterElement1;
        this.data[newIndex + 1] = troopRosterElement2;
      }
      else
      {
        for (int index = firstTroopIndex + 1; index < newIndex; ++index)
          this.data[index - 1] = this.data[index];
        this.data[newIndex] = troopRosterElement1;
        this.data[newIndex - 1] = troopRosterElement2;
      }
      this.UpdateVersion();
    }

    public int Sum(Func<TroopRosterElement, int> selector)
    {
      int num = 0;
      for (int index = 0; index < this._count; ++index)
        num += selector(this.data[index]);
      return num;
    }

    public void OnHeroHealthStatusChanged(Hero hero)
    {
      this.UpdateVersion();
      this._totalWoundedHeroes += hero.IsWounded ? 1 : -1;
      this.OnNumberChanged(false, true, false);
    }

    public void AddTroopTempXp(CharacterObject troop, int gainedXp)
    {
      int indexOfTroop = this.FindIndexOfTroop(troop);
      if (indexOfTroop < 0)
        return;
      this.data[indexOfTroop].TempXp += gainedXp;
    }

    public void ClearTempXp()
    {
      for (int index = 0; index < this._count; ++index)
        this.data[index].TempXp = 0;
    }

    public void UpdateVersion()
    {
      this.OwnerParty?.MobileParty?.UpdateVersionNo();
      ++this.VersionNo;
    }
  }

}