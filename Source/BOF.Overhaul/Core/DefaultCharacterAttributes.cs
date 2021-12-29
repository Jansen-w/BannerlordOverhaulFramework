using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BOF.Overhaul.Core
{
  public class DefaultCharacterAttributes
  {
    private CharacterAttribute _control;
    private CharacterAttribute _vigor;
    private CharacterAttribute _endurance;
    private CharacterAttribute _cunning;
    private CharacterAttribute _social;
    private CharacterAttribute _intelligence;

    private static DefaultCharacterAttributes Instance => Game.Current.DefaultCharacterAttributes;

    public static CharacterAttribute Vigor => DefaultCharacterAttributes.Instance._vigor;

    public static CharacterAttribute Control => DefaultCharacterAttributes.Instance._control;

    public static CharacterAttribute Endurance => DefaultCharacterAttributes.Instance._endurance;

    public static CharacterAttribute Cunning => DefaultCharacterAttributes.Instance._cunning;

    public static CharacterAttribute Social => DefaultCharacterAttributes.Instance._social;

    public static CharacterAttribute Intelligence => DefaultCharacterAttributes.Instance._intelligence;

    private CharacterAttribute Create(string stringId) => Game.Current.ObjectManager.RegisterPresumedObject<CharacterAttribute>(new CharacterAttribute(stringId));

    internal DefaultCharacterAttributes() => this.RegisterAll();

    private void RegisterAll()
    {
      this._vigor = this.Create("vigor");
      this._control = this.Create("control");
      this._endurance = this.Create("endurance");
      this._cunning = this.Create("cunning");
      this._social = this.Create("social");
      this._intelligence = this.Create("intelligence");
      this.InitializeAll();
    }

    private void InitializeAll()
    {
      this._vigor.Initialize(new TextObject("{=YWkdD7Ki}Vigor"), new TextObject("{=jJ9sLOLb}Vigor represents the ability to move with speed and force. It's important for melee combat."), new TextObject("{=Ve8xoa3i}VIG"));
      this._control.Initialize(new TextObject("{=controlskill}Control"), new TextObject("{=vx0OCvaj}Control represents the ability to use strength without sacrificing precision. It's necessary for using ranged weapons."), new TextObject("{=HuXafdmR}CTR"));
      this._endurance.Initialize(new TextObject("{=kvOavzcs}Endurance"), new TextObject("{=K8rCOQUZ}Endurance is the ability to perform taxing physical activity for a long time."), new TextObject("{=d2ApwXJr}END"));
      this._cunning.Initialize(new TextObject("{=JZM1mQvb}Cunning"), new TextObject("{=YO5LUfiO}Cunning is the ability to predict what other people will do, and to outwit their plans."), new TextObject("{=tH6Ooj0P}CNG"));
      this._social.Initialize(new TextObject("{=socialskill}Social"), new TextObject("{=XMDTt96y}Social is the ability to understand people's motivations and to sway them."), new TextObject("{=PHoxdReD}SOC"));
      this._intelligence.Initialize(new TextObject("{=sOrJoxiC}Intelligence"), new TextObject("{=TeUtEGV0}Intelligence represents aptitude for reading and theoretical learning."), new TextObject("{=Bn7IsMpu}INT"));
    }
  }
}