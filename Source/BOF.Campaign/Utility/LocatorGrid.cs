using System;
using System.Collections.Generic;
using BOF.Campaign.MapEntity;
using BOF.Campaign.Party;
using TaleWorlds.Library;

namespace BOF.Campaign.Utility
{
  public class LocatorGrid<T>
  {
    private T[] _nodes;
    private int _numNodes;
    private float _gridNodeSize;

    private int MapCoordinates(int x, int y)
    {
      int num = (x * 2027 + 7 * y) % this._numNodes;
      return num <= 0 ? 1 : num;
    }

    internal LocatorGrid(float gridNodeSize = 5f, int numNodes = 29584)
    {
      this._numNodes = numNodes;
      this._gridNodeSize = gridNodeSize;
      this._nodes = new T[numNodes];
    }

    internal bool UpdateParty(T party)
    {
      ILocatable<T> party1 = (object) party as ILocatable<T>;
      int nodeIndex = this.Pos2NodeIndex(party1.GetPosition2D);
      if (nodeIndex == party1.LocatorNodeIndex)
        return false;
      if (party1.LocatorNodeIndex > 0)
        this.RemoveFromList(party1);
      this.AddToList(nodeIndex, party);
      party1.LocatorNodeIndex = nodeIndex;
      return true;
    }

    private void RemoveFromList(ILocatable<T> party)
    {
      if ((object) this._nodes[party.LocatorNodeIndex] as ILocatable<T> == party)
      {
        this._nodes[party.LocatorNodeIndex] = party.NextLocatable;
        party.NextLocatable = default (T);
      }
      else
      {
        var locatable2 = this._nodes[party.LocatorNodeIndex] as ILocatable<T>;
        if (locatable2 == null)
          return;
        for (; (object) locatable2.NextLocatable != null; locatable2 = (object) locatable2.NextLocatable as ILocatable<T>)
        {
          if ((object) locatable2.NextLocatable as ILocatable<T> == party)
          {
            locatable2.NextLocatable = party.NextLocatable;
            party.NextLocatable = default (T);
            break;
          }
        }
      }
    }

    private void AddToList(int nodeIndex, T party)
    {
      T node = this._nodes[nodeIndex];
      this._nodes[nodeIndex] = party;
      ((ILocatable<T>)(object) party).NextLocatable = node;
    }

    internal void FindPartiesAroundPositionAsList(
      Vec2 position,
      float radius,
      List<T> closeParties)
    {
      closeParties.Clear();
      float num = radius * radius;
      int x1;
      int y1;
      this.GetGridIndices(position - new Vec2(radius, radius), out x1, out y1);
      int x2;
      int y2;
      this.GetGridIndices(position + new Vec2(radius, radius), out x2, out y2);
      for (int x3 = x1; x3 <= x2; ++x3)
      {
        for (int y3 = y1; y3 <= y2; ++y3)
        {
          for (T obj = this._nodes[this.MapCoordinates(x3, y3)]; (object) obj != null; obj = ((ILocatable<T>) (object) obj).NextLocatable)
          {
            if ((double) ((ILocatable<T>) (object) obj).GetPosition2D.DistanceSquared(position) < (double) num)
              closeParties.Add(obj);
          }
        }
      }
    }

    internal IEnumerable<T> FindPartiesAroundPosition(Vec2 position, float radius)
    {
      float r2 = radius * radius;
      int minY;
      int x;
      this.GetGridIndices(position - new Vec2(radius, radius), out x, out minY);
      int maxX;
      int maxY;
      this.GetGridIndices(position + new Vec2(radius, radius), out maxX, out maxY);
      for (int xi = x; xi <= maxX; ++xi)
      {
        for (int yi = minY; yi <= maxY; ++yi)
        {
          T curParty;
          for (curParty = this._nodes[this.MapCoordinates(xi, yi)]; (object) curParty != null; curParty = ((ILocatable<T>) (object) curParty).NextLocatable)
          {
            if ((double) ((ILocatable<T>) (object) curParty).GetPosition2D.DistanceSquared(position) < (double) r2)
              yield return curParty;
          }
          curParty = default (T);
        }
      }
    }

    internal IEnumerable<T> FindPartiesAroundPosition(
      Vec2 position,
      float radius,
      Func<T, bool> condition)
    {
      float r2 = radius * radius;
      int minY;
      int x;
      this.GetGridIndices(position - new Vec2(radius, radius), out x, out minY);
      int maxX;
      int maxY;
      this.GetGridIndices(position + new Vec2(radius, radius), out maxX, out maxY);
      for (int xi = x; xi <= maxX; ++xi)
      {
        for (int yi = minY; yi <= maxY; ++yi)
        {
          T curParty;
          for (curParty = this._nodes[this.MapCoordinates(xi, yi)]; (object) curParty != null; curParty = ((ILocatable<T>) (object) curParty).NextLocatable)
          {
            if ((curParty is Settlement settlement4 ? (double) settlement4.Position2D.DistanceSquared(position) : (double) ((MobileParty)(object) curParty).Position2D.DistanceSquared(position)) < (double) r2 && condition(curParty))
              yield return curParty;
          }
          curParty = default (T);
        }
      }
    }

    public void RemoveParty(T party)
    {
      ILocatable<T> party1 = (object) party as ILocatable<T>;
      if (party1.LocatorNodeIndex <= 0)
        return;
      this.RemoveFromList(party1);
    }

    private void GetGridIndices(Vec2 position, out int x, out int y)
    {
      x = MathF.Floor(position.x / this._gridNodeSize);
      y = MathF.Floor(position.y / this._gridNodeSize);
      if (x < 0)
        x = 0;
      if (y >= 0)
        return;
      y = 0;
    }

    private int Pos2NodeIndex(Vec2 position)
    {
      int x;
      int y;
      this.GetGridIndices(position, out x, out y);
      return this.MapCoordinates(x, y);
    }
  }
}
