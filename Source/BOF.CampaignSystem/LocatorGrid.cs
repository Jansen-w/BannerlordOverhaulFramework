using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace BOF.CampaignSystem
{
  public class LocatorGrid<T>
  {
    private readonly T[] _nodes;
    private readonly float _gridNodeSize;
    private readonly int _width;
    private readonly int _height;

    public LocatorGrid(float gridNodeSize = 5f, int gridWidth = 32, int gridHeight = 32)
    {
      this._width = gridWidth;
      this._height = gridHeight;
      this._gridNodeSize = gridNodeSize;
      this._nodes = new T[this._width * this._height];
    }

    private int MapCoordinates(int x, int y)
    {
      x %= this._width;
      if (x < 0)
        x += this._width;
      y %= this._height;
      if (y < 0)
        y += this._height;
      return y * this._width + x;
    }

    public bool UpdateParty(T party)
    {
      ILocatable<T> party1 = (object) party as ILocatable<T>;
      int nodeIndex = this.Pos2NodeIndex(party1.GetPosition2D);
      if (nodeIndex == party1.LocatorNodeIndex)
        return false;
      if (party1.LocatorNodeIndex >= 0)
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
        if (!(this._nodes[party.LocatorNodeIndex] is ILocatable<T> locatable2))
          return;
        for (; (object) locatable2.NextLocatable != null; locatable2 = (object) locatable2.NextLocatable as ILocatable<T>)
        {
          if ((object) locatable2.NextLocatable as ILocatable<T> == party)
          {
            locatable2.NextLocatable = party.NextLocatable;
            party.NextLocatable = default (T);
            return;
          }
        }
        Debug.FailedAssert("cannot remove party from MapLocator: " + party.ToString(), "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\LocatorGrid.cs", nameof (RemoveFromList), 101);
      }
    }

    private void AddToList(int nodeIndex, T party)
    {
      T node = this._nodes[nodeIndex];
      this._nodes[nodeIndex] = party;
      ((object) party as ILocatable<T>).NextLocatable = node;
    }

    public void FindPartiesAroundPositionAsList(
      Vec2 position,
      float radius,
      List<T> closeParties)
    {
      closeParties.Clear();
      float num = radius * radius;
      int minX;
      int minY;
      int maxX;
      int maxY;
      this.GetBoundaries(position, radius, out minX, out minY, out maxX, out maxY);
      for (int x = minX; x <= maxX; ++x)
      {
        for (int y = minY; y <= maxY; ++y)
        {
          for (T obj = this._nodes[this.MapCoordinates(x, y)]; (object) obj != null; obj = ((ILocatable<T>) (object) obj).NextLocatable)
          {
            if ((double) ((ILocatable<T>) (object) obj).GetPosition2D.DistanceSquared(position) < (double) num)
              closeParties.Add(obj);
          }
        }
      }
    }

    public IEnumerable<T> FindPartiesAroundPosition(Vec2 position, float radius)
    {
      float r2 = radius * radius;
      int minY;
      int maxX;
      int maxY;
      int minX;
      this.GetBoundaries(position, radius, out minX, out minY, out maxX, out maxY);
      for (int xi = minX; xi <= maxX; ++xi)
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

    public IEnumerable<T> FindPartiesAroundPosition(
      Vec2 position,
      float radius,
      Func<T, bool> condition)
    {
      float r2 = radius * radius;
      int minY;
      int maxX;
      int maxY;
      int minX;
      this.GetBoundaries(position, radius, out minX, out minY, out maxX, out maxY);
      for (int xi = minX; xi <= maxX; ++xi)
      {
        for (int yi = minY; yi <= maxY; ++yi)
        {
          T curParty;
          for (curParty = this._nodes[this.MapCoordinates(xi, yi)]; (object) curParty != null; curParty = ((ILocatable<T>) (object) curParty).NextLocatable)
          {
            if ((double) ((ILocatable<T>) (object) curParty).GetPosition2D.DistanceSquared(position) < (double) r2 && condition(curParty))
              yield return curParty;
          }
          curParty = default (T);
        }
      }
    }

    public void RemoveParty(T party)
    {
      ILocatable<T> party1 = (object) party as ILocatable<T>;
      if (party1.LocatorNodeIndex < 0)
        return;
      this.RemoveFromList(party1);
    }

    private void GetBoundaries(
      Vec2 position,
      float radius,
      out int minX,
      out int minY,
      out int maxX,
      out int maxY)
    {
      Vec2 vec2 = new Vec2(MathF.Min(radius, (float) ((double) (this._width - 1) * (double) this._gridNodeSize * 0.5)), MathF.Min(radius, (float) ((double) (this._height - 1) * (double) this._gridNodeSize * 0.5)));
      this.GetGridIndices(position - vec2, out minX, out minY);
      this.GetGridIndices(position + vec2, out maxX, out maxY);
    }

    private void GetGridIndices(Vec2 position, out int x, out int y)
    {
      x = MathF.Floor(position.x / this._gridNodeSize);
      y = MathF.Floor(position.y / this._gridNodeSize);
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
