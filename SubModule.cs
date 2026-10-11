using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace PartyQOL
{
    public class SubModule : MBSubModuleBase
    {
        public const string HarmonyId = "com.PartyQOL";
        public const string ModuleId = "PartyQOL";

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Pass assembly explicitly: parameterless PatchAll() can NRE under BLSE.
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(SubModule).Assembly);
        }
    }
}
