using System.Collections.Generic;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace PartyQOL.UI
{
    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='OtherPartyPanel']/Children")]
    internal sealed class PartyLeftSelectorPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartyLeftPartySelector";
    }

    /// <summary>
    /// Hide the vanilla left name while our party picker occupies the title slot.
    /// </summary>
    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='OtherPartyPanel']/Children/TextWidget[@Text='@OtherPartyNameLbl']")]
    internal sealed class HideOtherPartyNameLabelPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsHidden", "@IsLeftPartySelectorVisible")
        };
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children")]
    internal sealed class PartyTroopSortBarPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartyTroopSortBar";
    }

    /// <summary>
    /// Auto-upgrade control on the upgrade popup header strip.
    /// </summary>
    [PrefabExtension("PartyTroopManagerPopUp", "descendant::ListPanel[@IsVisible='@IsUpgradePopUp']/Children")]
    internal sealed class PartyAutoUpgradeButtonPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartyAutoUpgradeButton";
    }

    /// <summary>
    /// Show preferred-path highlight ring on upgrade slots (vanilla keeps IsVisible=false).
    /// </summary>
    [PrefabExtension("PartyTroopUpgradeItem", "descendant::TutorialHighlightItemBrushWidget")]
    internal sealed class PartyUpgradePreferredHighlightPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsVisible", "@IsHighlighted")
        };
    }
}
