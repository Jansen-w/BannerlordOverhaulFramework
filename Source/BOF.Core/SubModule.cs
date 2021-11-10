using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace BOF.Core
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            if(BOFHelpers.IsDebug())
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                var versionString = $"Loaded Bannerlord Overhaul Framework [Version {version}]";

                BOFHelpers.PrintInfoMessage(versionString);
            }
        }
    }
}