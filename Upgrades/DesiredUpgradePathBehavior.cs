using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyQOL.Upgrades
{
    /// <summary>
    /// Persists preferred upgrade paths as CharacterObject.StringId → target StringId.
    /// </summary>
    public sealed class DesiredUpgradePathBehavior : CampaignBehaviorBase
    {
        public static DesiredUpgradePathBehavior Instance { get; private set; }

        private Dictionary<string, string> _paths = new Dictionary<string, string>();

        public DesiredUpgradePathBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("PartyQOL_DesiredUpgradePaths", ref _paths);
            if (_paths == null)
            {
                _paths = new Dictionary<string, string>();
            }
        }

        public void SetPreferred(CharacterObject source, CharacterObject target)
        {
            if (source == null || target == null || string.IsNullOrEmpty(source.StringId) || string.IsNullOrEmpty(target.StringId))
            {
                return;
            }

            _paths[source.StringId] = target.StringId;
            string message = new TextObject("{=pqol_pref_set}Preferred upgrade set: {SOURCE} → {TARGET}")
                .SetTextVariable("SOURCE", source.Name)
                .SetTextVariable("TARGET", target.Name)
                .ToString();
            InformationManager.DisplayMessage(new InformationMessage(message));
        }

        public void ClearPreferred(CharacterObject source)
        {
            if (source == null || string.IsNullOrEmpty(source.StringId) || _paths == null)
            {
                return;
            }

            if (!_paths.Remove(source.StringId))
            {
                return;
            }

            string message = new TextObject("{=pqol_pref_clear}Preferred upgrade cleared: {SOURCE}")
                .SetTextVariable("SOURCE", source.Name)
                .ToString();
            InformationManager.DisplayMessage(new InformationMessage(message));
        }

        /// <summary>Sets preference, or clears it if this target is already preferred.</summary>
        public bool TogglePreferred(CharacterObject source, CharacterObject target)
        {
            if (IsPreferred(source, target))
            {
                ClearPreferred(source);
                return false;
            }

            SetPreferred(source, target);
            return true;
        }

        public bool TryGetPreferredTargetId(CharacterObject source, out string targetId)
        {
            targetId = null;
            if (source == null || string.IsNullOrEmpty(source.StringId) || _paths == null)
            {
                return false;
            }

            return _paths.TryGetValue(source.StringId, out targetId) && !string.IsNullOrEmpty(targetId);
        }

        public CharacterObject ResolvePreferredTarget(CharacterObject source)
        {
            if (source?.UpgradeTargets == null || source.UpgradeTargets.Length == 0)
            {
                return null;
            }

            if (source.UpgradeTargets.Length == 1)
            {
                return source.UpgradeTargets[0];
            }

            if (!TryGetPreferredTargetId(source, out string targetId))
            {
                return null;
            }

            foreach (CharacterObject target in source.UpgradeTargets)
            {
                if (target != null && target.StringId == targetId)
                {
                    return target;
                }
            }

            return null;
        }

        public int ResolvePreferredIndex(CharacterObject source)
        {
            CharacterObject preferred = ResolvePreferredTarget(source);
            if (preferred == null || source?.UpgradeTargets == null)
            {
                return -1;
            }

            for (int i = 0; i < source.UpgradeTargets.Length; i++)
            {
                if (source.UpgradeTargets[i] == preferred)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsPreferred(CharacterObject source, CharacterObject target)
        {
            if (source == null || target == null || !TryGetPreferredTargetId(source, out string targetId))
            {
                return false;
            }

            return target.StringId == targetId;
        }
    }
}
