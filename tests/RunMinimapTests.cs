using System;
using System.Linq;
using Godot;
using Knight.Tests.Unit;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunMinimapTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            int regressions = args.Count(v => v == "--minimap-red-assertion=DenseBoundaryCannotStarveNearCell" ||
                v == "--minimap-red-assertion=SameAnchorMovementReordersNearCandidates");
            int cold = args.Count(v => v == "--minimap-cold-read");
            int oracle=args.Count(v=>v.StartsWith("--minimap-oracle=",StringComparison.Ordinal));
            TestAssert.True(args.Length == 2 + regressions + cold + oracle && oracle<=1 && (oracle==0 || args.Contains("--minimap-phase=oracle")) && regressions <= 1 && cold <= 1 &&
                args.Count(v => v == "--minimap-phase=rawschema" || v == "--minimap-phase=presentation" || v == "--minimap-phase=glstills" || v == "--minimap-phase=normalperf" || v == "--minimap-phase=timingcontrols" || v == "--minimap-phase=oracle" || v == "--minimap-phase=profile" || v == "--minimap-phase=performance" || v == "--minimap-phase=formal" || v == "--minimap-phase=catalog" || v == "--minimap-phase=visibility" || v == "--minimap-phase=model" || v == "--minimap-phase=save" || v == "--minimap-phase=lifecycle" || v == "--minimap-phase=hud" || v == "--minimap-phase=hudregression") == 1 &&
                args.Count(v => v.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                (regressions == 0 || args.Contains("--minimap-phase=model")) &&
                (cold == 0 || args.Contains("--minimap-phase=save") && regressions == 0),
                "minimap runner requires an implemented phase and exactly one external case root");
            IsolatedPrologueFixture.ValidateStorageRoot(); // Read-only, before any fixture or IO.
            string phase = args.Single(v => v.StartsWith("--minimap-phase=", StringComparison.Ordinal)).Split('=')[1];
            if (phase == "formal") await MinimapFormalTests.Run(this);
            else if (phase == "rawschema") MinimapCatalogTests.RawSchemaControls();
            else if (phase == "presentation" || phase=="glstills") await MinimapFormalTests.Presentation(this);
            else if (phase == "normalperf") await Knight.Tests.Presentation.MinimapNormalTimeProbe.Run(this);
            else if (phase == "timingcontrols") Knight.Tests.Presentation.MinimapStrictSampleBufferTests.Run();
            else if (phase == "performance") await MinimapFormalTests.Run(this, true);
            else if (phase == "profile") await MinimapFormalTests.Run(this, profileOnly:true);
            else if (phase == "oracle") await MinimapFormalTests.Run(this, oracle:true, oraclePath:oracle==0?null:args.Single(v=>v.StartsWith("--minimap-oracle=",StringComparison.Ordinal))["--minimap-oracle=".Length..]);
            else if (phase == "catalog") await MinimapCatalogTests.Run(this);
            else if (phase == "hud") await MinimapHudTests.Run(this);
            else if (phase == "hudregression") await MinimapHudTests.Regressions(this);
            else if (phase == "lifecycle") await MinimapLifecycleTests.Run(this);
            else if (phase == "visibility") MinimapVisibilityTests.Run();
            else if (phase == "save") { if (cold == 1) ExplorationMapColdReadTests.Run(); else ExplorationMapSaveTests.Run(); }
            else ExplorationMapModelTests.Run(regressions == 0 ? "" :
                args.Single(v => v.StartsWith("--minimap-red-assertion=", StringComparison.Ordinal)).Split('=')[1]);
            GD.Print($"MINIMAP_PHASE_OK phase={phase}");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
