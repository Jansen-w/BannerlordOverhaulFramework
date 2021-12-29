using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace BOF.Overhaul.Core
{
  public class WeaponDescription : MBObjectBase
  {
    public bool UseCenterOfMassAsHandBase;
    private List<CraftingPiece> _availablePieces;

    public WeaponClass WeaponClass { get; private set; }

    public WeaponFlags WeaponFlags { get; private set; }

    public string ItemUsageFeatures { get; private set; }

    public bool RotatedInHand { get; private set; }

    public bool IsHiddenFromUI { get; private set; }

    public IReadOnlyList<CraftingPiece> AvailablePieces => (IReadOnlyList<CraftingPiece>) this._availablePieces;

    // public override void Deserialize(MBObjectManager objectManager, XmlNode node)
    // {
    //   base.Deserialize(objectManager, node);
    //   this.WeaponClass = node.Attributes["weapon_class"] != null ? (WeaponClass) Enum.Parse(typeof (WeaponClass), node.Attributes["weapon_class"].Value) : WeaponClass.Undefined;
    //   this.ItemUsageFeatures = node.Attributes["item_usage_features"] != null ? node.Attributes["item_usage_features"].Value : "";
    //   this.RotatedInHand = XmlHelper.ReadBool(node, "rotated_in_hand");
    //   this.UseCenterOfMassAsHandBase = XmlHelper.ReadBool(node, "use_center_of_mass_as_hand_base");
    //   foreach (XmlNode childNode1 in node.ChildNodes)
    //   {
    //     if (childNode1.Name == "WeaponFlags")
    //     {
    //       foreach (XmlNode childNode2 in childNode1.ChildNodes)
    //         this.WeaponFlags |= (WeaponFlags) Enum.Parse(typeof (WeaponFlags), childNode2.Attributes["value"].Value);
    //     }
    //     else if (childNode1.Name == "AvailablePieces")
    //     {
    //       this._availablePieces = new List<CraftingPiece>();
    //       foreach (XmlNode childNode3 in childNode1.ChildNodes)
    //       {
    //         if (childNode3.NodeType == XmlNodeType.Element)
    //         {
    //           CraftingPiece craftingPiece = MBObjectManager.Instance.GetObject<CraftingPiece>(childNode3.Attributes["id"].Value);
    //           if (craftingPiece != null)
    //             this._availablePieces.Add(craftingPiece);
    //         }
    //       }
    //     }
    //   }
    // }

    public void SetHiddenFromUI() => this.IsHiddenFromUI = true;
  }
}