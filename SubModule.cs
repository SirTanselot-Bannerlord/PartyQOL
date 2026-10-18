using Bannerlord.UIExtenderEx;
using HarmonyLib;
using PartyQOL.UI.Popups;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PartyQOL
{
    public class SubModule : MBSubModuleBase
    {
        public const string HarmonyId = "com.PartyQOL";
        public const string ModuleId = "PartyQOL";

        private UIExtender _uiExtender;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Pass assembly explicitly: parameterless PatchAll() can NRE under BLSE.
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(SubModule).Assembly);

            _uiExtender = UIExtender.Create(ModuleId);
            _uiExtender.Register(typeof(SubModule).Assembly);
            _uiExtender.Enable();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            base.InitializeGameStarter(game, starterObject);
            if (starterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(new AbsorbPartyDialogueBehavior());
                starter.AddBehavior(new Upgrades.DesiredUpgradePathBehavior());
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            UI.PartyVMMixin.TickQueuedReopen();
            if (PopupHost.IsAnyOpen)
            {
                PopupHost.Tick();
            }
        }
    }
}
