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

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children")]
    internal sealed class PartyRightSelectorPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartyRightPartySelector";
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='OtherPartyPanel']/Children/TextWidget[@Text='@OtherPartyNameLbl']")]
    internal sealed class HideOtherPartyNameLabelPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsHidden", "@IsLeftPartySelectorVisible")
        };
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children/TextWidget[@Text='@MainPartyNameLbl']")]
    internal sealed class HideMainPartyNameLabelPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsHidden", "@IsRightPartySelectorVisible")
        };
    }

    /// <summary>
    /// Row 1: name left, composition right. Row 2: upgrades left; Sort centered; Filter right.
    /// </summary>
    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children")]
    internal sealed class PartySortFilterStripPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartySortFilterStrip";
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children")]
    internal sealed class PartyFilterStripPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartyFilterStrip";
    }

    // Isolation test: composition rows stay vanilla. The button replacement (empty
    // PartyQOL.Composition.Button brush) was the last party-screen widget change
    // already running when the native crash streak started. Click handlers remain
    // on the mixin but are not bound until these four Replace patches come back.

    // Do not touch per-row TroopNum — Brush.FontColor on Party.Text.Tuple mutates it globally.

    [PrefabExtension("PartyScreen", "descendant::TextWidget[@Id='MainPartyTroopsLabel']")]
    internal sealed class MainPartyTroopsLabelFilteredPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Replace;

        [PrefabExtensionFileName(true)]
        public string File => "PartyMainTroopsLabelFiltered";
    }

    [PrefabExtension("PartyScreen", "descendant::TextWidget[@Id='OtherPartyTroopsLabel']")]
    internal sealed class OtherPartyTroopsLabelFilteredPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Replace;

        [PrefabExtensionFileName(true)]
        public string File => "PartyOtherTroopsLabelFiltered";
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children/PartyComposition")]
    internal sealed class MainCompositionToNameRowPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("HorizontalAlignment", "Right"),
            new Attribute("MarginTop", "10"),
            new Attribute("MarginLeft", "0"),
            new Attribute("MarginRight", "25")
        };
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children/ListPanel[@MarginTop='10' and @MarginRight='65']")]
    internal sealed class MoveUpgradeRecruitToSecondRowPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("HorizontalAlignment", "Left"),
            new Attribute("MarginTop", "52"),
            new Attribute("MarginLeft", "25"),
            new Attribute("MarginRight", "0")
        };
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']/Children/TextWidget[@Text='@MainPartyNameLbl']")]
    internal sealed class MainPartyNameRoomPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("SuggestedWidth", "340"),
            new Attribute("MarginRight", "280")
        };
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='MainPartyPanel']//PartySortController")]
    internal sealed class HideMainPartySortControllerPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsVisible", "false")
        };
    }

    [PrefabExtension("PartyScreen", "descendant::BrushWidget[@Id='OtherPartyPanel']//PartySortController")]
    internal sealed class HideOtherPartySortControllerPatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsVisible", "false")
        };
    }

    [PrefabExtension("PartyTroopTuple", "descendant::PartyTroopTupleButtonWidget[@Id='PartyTroopTuple']")]
    internal sealed class PartyTroopTupleFilterHidePatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsHidden", "@IsHiddenByPartyQolFilter")
        };
    }

    [PrefabExtension("PartyTroopTupleLeft", "descendant::PartyTroopTupleButtonWidget[@Id='PartyTroopTuple']")]
    internal sealed class PartyTroopTupleLeftFilterHidePatch : PrefabExtensionSetAttributePatch
    {
        public override List<Attribute> Attributes => new List<Attribute>
        {
            new Attribute("IsHidden", "@IsHiddenByPartyQolFilter")
        };
    }

    // Frame chrome below title plaque / above column headers (see PartyAutoUpgradeButton margins).
    [PrefabExtension("PartyTroopManagerPopUp", "descendant::BrushWidget[@Brush='Party.UpgradePopup.Frame']/Children")]
    internal sealed class PartyAutoUpgradeButtonPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Child;

        public override int Index => int.MaxValue;

        [PrefabExtensionFileName(true)]
        public string File => "PartyAutoUpgradeButton";
    }

    [PrefabExtension("PartyTroopUpgradeItem", "descendant::TutorialHighlightItemBrushWidget")]
    internal sealed class PartyUpgradePreferredHighlightPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Replace;

        [PrefabExtensionFileName(true)]
        public string File => "PartyUpgradePreferredHighlight";
    }

    // Troop-row upgrade slots (regular party screen). The popup uses the patch above.
    // Only the preferred-path highlight — leave transfer/recruit tutorial pulses alone.
    [PrefabExtension("PartyTroopTuple", "descendant::TutorialHighlightItemBrushWidget[@IsHighlightEnabled='@IsHighlighted']")]
    internal sealed class PartyTroopTupleUpgradeHighlightPatch : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Replace;

        [PrefabExtensionFileName(true)]
        public string File => "PartyUpgradePreferredHighlight";
    }
}
