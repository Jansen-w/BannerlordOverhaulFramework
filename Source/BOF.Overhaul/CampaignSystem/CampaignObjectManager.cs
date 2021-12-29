using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace BOF.Overhaul.CampaignSystem
{
  public class CampaignObjectManager
  {
    internal const uint HeroObjectManagerTypeID = 32;
    internal const uint MobilePartyObjectManagerTypeID = 14;
    internal const uint ClanObjectManagerTypeID = 18;
    internal const uint KingdomObjectManagerTypeID = 20;
    private CampaignObjectManager.ICampaignObjectType[] _objects;
    private Dictionary<Type, uint> _objectTypesAndNextIds;
    //[SaveableField(20)]
    private readonly List<Hero> _deadOrDisabledHeroes;
    //[SaveableField(30)]
    private readonly List<Hero> _aliveHeroes;
    //[SaveableField(40)]
    private readonly List<Clan> _clans;
    //[SaveableField(50)]
    private readonly List<Kingdom> _kingdoms;
    private List<IFaction> _factions;
    private bool _forceCopyListsForSaveCompability;
    //[SaveableField(71)]
    private List<MobileParty> _mobileParties;

    //[SaveableProperty(80)]
    public MBReadOnlyList<Settlement> Settlements { get; private set; }

    public MBReadOnlyList<MobileParty> MobileParties { get; private set; }

    public MBReadOnlyList<Hero> AliveHeroes { get; private set; }

    public MBReadOnlyList<Hero> DeadOrDisabledHeroes { get; private set; }

    public MBReadOnlyList<Clan> Clans { get; private set; }

    public MBReadOnlyList<Kingdom> Kingdoms { get; private set; }

    public MBReadOnlyList<IFaction> Factions { get; private set; }

    public CampaignObjectManager()
    {
      this._objects = new CampaignObjectManager.ICampaignObjectType[5];
      this._mobileParties = new List<MobileParty>();
      this._deadOrDisabledHeroes = new List<Hero>();
      this._aliveHeroes = new List<Hero>();
      this._clans = new List<Clan>();
      this._kingdoms = new List<Kingdom>();
      this._factions = new List<IFaction>();
      this.AliveHeroes = this._aliveHeroes.GetReadOnlyList<Hero>();
      this.DeadOrDisabledHeroes = this._deadOrDisabledHeroes.GetReadOnlyList<Hero>();
      this.Clans = this._clans.GetReadOnlyList<Clan>();
      this.Kingdoms = this._kingdoms.GetReadOnlyList<Kingdom>();
      this.Factions = this._factions.GetReadOnlyList<IFaction>();
      this.MobileParties = this._mobileParties.GetReadOnlyList<MobileParty>();
    }

    private void InitializeManagerObjectLists()
    {
      this._objects[4] = (CampaignObjectManager.ICampaignObjectType) new CampaignObjectManager.CampaignObjectType<MobileParty>((IEnumerable<MobileParty>) this._mobileParties);
      this._objects[0] = (CampaignObjectManager.ICampaignObjectType) new CampaignObjectManager.CampaignObjectType<Hero>((IEnumerable<Hero>) this._deadOrDisabledHeroes);
      this._objects[1] = (CampaignObjectManager.ICampaignObjectType) new CampaignObjectManager.CampaignObjectType<Hero>((IEnumerable<Hero>) this._aliveHeroes);
      this._objects[2] = (CampaignObjectManager.ICampaignObjectType) new CampaignObjectManager.CampaignObjectType<Clan>((IEnumerable<Clan>) this._clans);
      this._objects[3] = (CampaignObjectManager.ICampaignObjectType) new CampaignObjectManager.CampaignObjectType<Kingdom>((IEnumerable<Kingdom>) this._kingdoms);
      this._objectTypesAndNextIds = new Dictionary<Type, uint>();
      foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
      {
        uint maxObjectSubId = campaignObjectType.GetMaxObjectSubId();
        uint num;
        if (this._objectTypesAndNextIds.TryGetValue(campaignObjectType.ObjectClass, out num))
        {
          if (num <= maxObjectSubId)
            this._objectTypesAndNextIds[campaignObjectType.ObjectClass] = maxObjectSubId + 1U;
        }
        else
          this._objectTypesAndNextIds.Add(campaignObjectType.ObjectClass, maxObjectSubId + 1U);
      }
    }

    // [LoadInitializationCallback]
    // private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
    // {
    //   if (this._mobileParties == null)
    //   {
    //     this._mobileParties = new List<MobileParty>();
    //     foreach (KeyValuePair<int, PartyBase> keyValuePair in objectLoadData.GetDataBySaveId(10) as Dictionary<int, PartyBase>)
    //     {
    //       if (keyValuePair.Value.IsMobile)
    //         this._mobileParties.Add(keyValuePair.Value.MobileParty);
    //     }
    //   }
    //   this._objects = new CampaignObjectManager.ICampaignObjectType[5];
    //   this._factions = new List<IFaction>();
    //   this.AliveHeroes = this._aliveHeroes.GetReadOnlyList<Hero>();
    //   this.DeadOrDisabledHeroes = this._deadOrDisabledHeroes.GetReadOnlyList<Hero>();
    //   this.Clans = this._clans.GetReadOnlyList<Clan>();
    //   this.Kingdoms = this._kingdoms.GetReadOnlyList<Kingdom>();
    //   this.Factions = this._factions.GetReadOnlyList<IFaction>();
    //   this.MobileParties = this._mobileParties.GetReadOnlyList<MobileParty>();
    // }

    internal void PreAfterLoad()
    {
      foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
        campaignObjectType.PreAfterLoad();
    }

    internal void AfterLoad()
    {
      foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
        campaignObjectType.AfterLoad();
    }

    internal void SetForceCopyListsForSaveCompability() => this._forceCopyListsForSaveCompability = true;

    public void InitializeForOldSaves()
    {
      if (!this._forceCopyListsForSaveCompability)
        return;
      this.InitializeOnNewGame();
    }

    internal void InitializeOnLoad()
    {
      this.Settlements = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
      if (this._forceCopyListsForSaveCompability)
        return;
      foreach (Clan clan in this._clans)
      {
        if (!this._factions.Contains((IFaction) clan))
          this._factions.Add((IFaction) clan);
      }
      foreach (Kingdom kingdom in this._kingdoms)
      {
        if (!this._factions.Contains((IFaction) kingdom))
          this._factions.Add((IFaction) kingdom);
      }
      this.InitializeManagerObjectLists();
    }

    internal void InitializeOnNewGame()
    {
      MBReadOnlyList<Hero> objectTypeList1 = MBObjectManager.Instance.GetObjectTypeList<Hero>();
      MBReadOnlyList<MobileParty> objectTypeList2 = MBObjectManager.Instance.GetObjectTypeList<MobileParty>();
      MBReadOnlyList<Clan> objectTypeList3 = MBObjectManager.Instance.GetObjectTypeList<Clan>();
      MBReadOnlyList<Kingdom> objectTypeList4 = MBObjectManager.Instance.GetObjectTypeList<Kingdom>();
      this.Settlements = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
      foreach (Hero hero in objectTypeList1)
      {
        if (hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled)
        {
          if (!this._deadOrDisabledHeroes.Contains(hero))
            this._deadOrDisabledHeroes.Add(hero);
        }
        else if (!this._aliveHeroes.Contains(hero))
          this._aliveHeroes.Add(hero);
      }
      foreach (Clan clan in objectTypeList3)
      {
        if (!this._clans.Contains(clan))
          this._clans.Add(clan);
        if (!this._factions.Contains((IFaction) clan))
          this._factions.Add((IFaction) clan);
      }
      foreach (Kingdom kingdom in objectTypeList4)
      {
        if (!this._kingdoms.Contains(kingdom))
          this._kingdoms.Add(kingdom);
        if (!this._factions.Contains((IFaction) kingdom))
          this._factions.Add((IFaction) kingdom);
      }
      foreach (MobileParty mobileParty in objectTypeList2)
        this._mobileParties.Add(mobileParty);
      this.InitializeManagerObjectLists();
    }

    internal void AddMobileParty(MobileParty party)
    {
      party.Id = new MBGUID(14U, BOFCampaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<MobileParty>());
      this._mobileParties.Add(party);
      this.OnItemAdded<MobileParty>(CampaignObjectManager.CampaignObjects.MobileParty, party);
    }

    internal void RemoveMobileParty(MobileParty party)
    {
      this._mobileParties.Remove(party);
      this.OnItemRemoved<MobileParty>(CampaignObjectManager.CampaignObjects.MobileParty, party);
    }

    internal void AddHero(Hero hero)
    {
      hero.Id = new MBGUID(32U, BOFCampaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Hero>());
      this.OnHeroAdded(hero);
    }

    internal void UnregisterDeadHero(Hero hero)
    {
      this._deadOrDisabledHeroes.Remove(hero);
      this.OnItemRemoved<Hero>(CampaignObjectManager.CampaignObjects.DeadOrDisabledHeroes, hero);
    }

    private void OnHeroAdded(Hero hero)
    {
      if (hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled)
      {
        this._deadOrDisabledHeroes.Add(hero);
        this.OnItemAdded<Hero>(CampaignObjectManager.CampaignObjects.DeadOrDisabledHeroes, hero);
      }
      else
      {
        this._aliveHeroes.Add(hero);
        this.OnItemAdded<Hero>(CampaignObjectManager.CampaignObjects.AliveHeroes, hero);
      }
    }

    internal void HeroStateChanged(Hero hero, Hero.CharacterStates oldState)
    {
      int num1 = oldState == Hero.CharacterStates.Dead ? 1 : (oldState == Hero.CharacterStates.Disabled ? 1 : 0);
      bool flag = hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled;
      int num2 = flag ? 1 : 0;
      if (num1 == num2)
        return;
      if (flag)
      {
        if (this._aliveHeroes.Contains(hero))
          this._aliveHeroes.Remove(hero);
      }
      else if (this._deadOrDisabledHeroes.Contains(hero))
        this._deadOrDisabledHeroes.Remove(hero);
      this.OnHeroAdded(hero);
    }

    internal void AddClan(Clan clan)
    {
      clan.Id = new MBGUID(18U, BOFCampaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Clan>());
      this._clans.Add(clan);
      this.OnItemAdded<Clan>(CampaignObjectManager.CampaignObjects.Clans, clan);
      this._factions.Add((IFaction) clan);
    }

    internal void RemoveClan(Clan clan)
    {
      if (this._clans.Contains(clan))
      {
        this._clans.Remove(clan);
        this.OnItemRemoved<Clan>(CampaignObjectManager.CampaignObjects.Clans, clan);
      }
      if (!this._factions.Contains((IFaction) clan))
        return;
      this._factions.Remove((IFaction) clan);
    }

    internal void AddKingdom(Kingdom kingdom)
    {
      kingdom.Id = new MBGUID(20U, BOFCampaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Kingdom>());
      this._kingdoms.Add(kingdom);
      this.OnItemAdded<Kingdom>(CampaignObjectManager.CampaignObjects.Kingdoms, kingdom);
      this._factions.Add((IFaction) kingdom);
    }

    private void OnItemAdded<T>(CampaignObjectManager.CampaignObjects targetList, T obj) where T : MBObjectBase => ((CampaignObjectManager.CampaignObjectType<T>) this._objects[(int) targetList])?.OnItemAdded(obj);

    private void OnItemRemoved<T>(CampaignObjectManager.CampaignObjects targetList, T obj) where T : MBObjectBase => ((CampaignObjectManager.CampaignObjectType<T>) this._objects[(int) targetList])?.UnregisterItem(obj);

    public T Find<T>(Predicate<T> predicate) where T : MBObjectBase
    {
      foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
      {
        if (typeof (T) == campaignObjectType.ObjectClass)
        {
          T obj = ((CampaignObjectManager.CampaignObjectType<T>) campaignObjectType).Find(predicate);
          if ((object) obj != null)
            return obj;
        }
      }
      return default (T);
    }

    private uint GetNextUniqueObjectIdOfType<T>() where T : MBObjectBase
    {
      uint num;
      if (this._objectTypesAndNextIds.TryGetValue(typeof (T), out num))
        this._objectTypesAndNextIds[typeof (T)] = num + 1U;
      return num;
    }

    public T Find<T>(string id) where T : MBObjectBase
    {
      foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
      {
        if (campaignObjectType != null && typeof (T) == campaignObjectType.ObjectClass)
        {
          T obj = ((CampaignObjectManager.CampaignObjectType<T>) campaignObjectType).Find(id);
          if ((object) obj != null)
            return obj;
        }
      }
      return default (T);
    }

    public string FindNextUniqueStringId<T>(string id) where T : MBObjectBase
    {
      List<CampaignObjectManager.CampaignObjectType<T>> lists = new List<CampaignObjectManager.CampaignObjectType<T>>();
      foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
      {
        if (campaignObjectType != null && typeof (T) == campaignObjectType.ObjectClass)
          lists.Add(campaignObjectType as CampaignObjectManager.CampaignObjectType<T>);
      }
      return CampaignObjectManager.CampaignObjectType<T>.FindNextUniqueStringId(lists, id);
    }

    internal static void AutoGeneratedStaticCollectObjectsCampaignObjectManager(
      object o,
      List<object> collectedObjects)
    {
      ((CampaignObjectManager) o).AutoGeneratedInstanceCollectObjects(collectedObjects);
    }

    protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
    {
      collectedObjects.Add((object) this._deadOrDisabledHeroes);
      collectedObjects.Add((object) this._aliveHeroes);
      collectedObjects.Add((object) this._clans);
      collectedObjects.Add((object) this._kingdoms);
      collectedObjects.Add((object) this._mobileParties);
      collectedObjects.Add((object) this.Settlements);
    }

    internal static object AutoGeneratedGetMemberValueSettlements(object o) => (object) ((CampaignObjectManager) o).Settlements;

    internal static object AutoGeneratedGetMemberValue_deadOrDisabledHeroes(object o) => (object) ((CampaignObjectManager) o)._deadOrDisabledHeroes;

    internal static object AutoGeneratedGetMemberValue_aliveHeroes(object o) => (object) ((CampaignObjectManager) o)._aliveHeroes;

    internal static object AutoGeneratedGetMemberValue_clans(object o) => (object) ((CampaignObjectManager) o)._clans;

    internal static object AutoGeneratedGetMemberValue_kingdoms(object o) => (object) ((CampaignObjectManager) o)._kingdoms;

    internal static object AutoGeneratedGetMemberValue_mobileParties(object o) => (object) ((CampaignObjectManager) o)._mobileParties;

    private interface ICampaignObjectType : IEnumerable
    {
      Type ObjectClass { get; }

      void PreAfterLoad();

      void AfterLoad();

      uint GetMaxObjectSubId();
    }

    private class CampaignObjectType<T> : 
      CampaignObjectManager.ICampaignObjectType,
      IEnumerable,
      IEnumerable<T>
      where T : MBObjectBase
    {
      private readonly IEnumerable<T> _registeredObjects;

      public uint MaxCreatedPostfixIndex { get; private set; }

      public CampaignObjectType(IEnumerable<T> registeredObjects)
      {
        this._registeredObjects = registeredObjects;
        foreach (T registeredObject in this._registeredObjects)
        {
          (string str, uint number) idParts = CampaignObjectManager.CampaignObjectType<T>.GetIdParts(registeredObject.StringId);
          if (idParts.number > this.MaxCreatedPostfixIndex)
            this.MaxCreatedPostfixIndex = idParts.number;
        }
      }

      Type CampaignObjectManager.ICampaignObjectType.ObjectClass => typeof (T);

      public void PreAfterLoad()
      {
        foreach (T obj in this._registeredObjects.ToList<T>())
          obj.PreAfterLoadInternal();
      }

      public void AfterLoad()
      {
        foreach (T obj in this._registeredObjects.ToList<T>())
          obj.AfterLoadInternal();
      }

      IEnumerator<T> IEnumerable<T>.GetEnumerator() => this._registeredObjects.GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this._registeredObjects.GetEnumerator();

      public uint GetMaxObjectSubId()
      {
        uint num = 0;
        foreach (T registeredObject in this._registeredObjects)
        {
          if (registeredObject.Id.SubId > num)
            num = registeredObject.Id.SubId;
        }
        return num;
      }

      public void OnItemAdded(T item)
      {
        (string str, uint number) idParts = CampaignObjectManager.CampaignObjectType<T>.GetIdParts(item.StringId);
        if (idParts.number > this.MaxCreatedPostfixIndex)
          this.MaxCreatedPostfixIndex = idParts.number;
        this.RegisterItem(item);
      }

      private void RegisterItem(T item) => item.IsReady = true;

      public void UnregisterItem(T item) => item.IsReady = false;

      public T Find(string id)
      {
        foreach (T registeredObject in this._registeredObjects)
        {
          if (registeredObject.StringId == id)
            return registeredObject;
        }
        return default (T);
      }

      public T Find(Predicate<T> predicate)
      {
        foreach (T registeredObject in this._registeredObjects)
        {
          if (predicate(registeredObject))
            return registeredObject;
        }
        return default (T);
      }

      public static string FindNextUniqueStringId(
        List<CampaignObjectManager.CampaignObjectType<T>> lists,
        string id)
      {
        if (!CampaignObjectManager.CampaignObjectType<T>.Exist(lists, id))
          return id;
        (string str, uint number) idParts = CampaignObjectManager.CampaignObjectType<T>.GetIdParts(id);
        return idParts.str + (object) (MathF.Max(idParts.number, lists.Max<CampaignObjectManager.CampaignObjectType<T>, uint>((Func<CampaignObjectManager.CampaignObjectType<T>, uint>) (x => x.MaxCreatedPostfixIndex))) + 1U);
      }

      private static (string str, uint number) GetIdParts(string stringId)
      {
        int index = stringId.Length - 1;
        while (index > 0 && char.IsDigit(stringId[index]))
          --index;
        string str = stringId.Substring(0, index + 1);
        uint result = 0;
        if (index < stringId.Length - 1)
          uint.TryParse(stringId.Substring(index + 1, stringId.Length - index - 1), out result);
        int num = (int) result;
        return (str, (uint) num);
      }

      private static bool Exist(
        List<CampaignObjectManager.CampaignObjectType<T>> lists,
        string id)
      {
        foreach (CampaignObjectManager.CampaignObjectType<T> list in lists)
        {
          if (list.Find(id) != null)
            return true;
        }
        return false;
      }
    }

    private enum CampaignObjects
    {
      DeadOrDisabledHeroes,
      AliveHeroes,
      Clans,
      Kingdoms,
      MobileParty,
      Count,
    }
  }
}