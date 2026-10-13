using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace PartyQOL.UI.Popups
{
    public abstract class PopupVM : ViewModel
    {
        internal PopupHost Host { get; set; }

        public abstract void ExecuteCancel();

        protected void Close()
        {
            Host?.Close();
        }
    }

    /// <summary>
    /// CAE-style focused Gauntlet layer on top of the current screen; Escape cancels the topmost popup.
    /// </summary>
    internal sealed class PopupHost
    {
        private static readonly List<PopupHost> Stack = new List<PopupHost>();

        private readonly ScreenBase _screen;
        private readonly GauntletLayer _layer;
        private readonly PopupVM _vm;

        private PopupHost(ScreenBase screen, GauntletLayer layer, PopupVM vm)
        {
            _screen = screen;
            _layer = layer;
            _vm = vm;
        }

        public static bool IsAnyOpen => Stack.Count > 0;

        public static void Show(string movie, PopupVM vm)
        {
            ScreenBase screen = ScreenManager.TopScreen;
            if (screen == null)
            {
                return;
            }

            GauntletLayer layer = new GauntletLayer("PartyQOLPopup", 300 + Stack.Count * 10);
            layer.LoadMovie(movie, vm);
            layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            layer.IsFocusLayer = true;
            layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            screen.AddLayer(layer);
            ScreenManager.TrySetFocus(layer);
            PopupHost host = new PopupHost(screen, layer, vm);
            vm.Host = host;
            Stack.Add(host);
        }

        public static void Tick()
        {
            PopupHost top = Stack.LastOrDefault();
            if (top != null && top._layer.Input.IsHotKeyReleased("Exit"))
            {
                top._vm.ExecuteCancel();
            }
        }

        public static void CloseAll()
        {
            for (int i = Stack.Count - 1; i >= 0; i--)
            {
                Stack[i].Close();
            }
        }

        public void Close()
        {
            if (!Stack.Remove(this))
            {
                return;
            }

            _layer.InputRestrictions.ResetInputRestrictions();
            ScreenManager.TryLoseFocus(_layer);
            _screen.RemoveLayer(_layer);
            _vm.OnFinalize();
            PopupHost below = Stack.LastOrDefault();
            if (below != null)
            {
                ScreenManager.TrySetFocus(below._layer);
            }
        }
    }
}
