using System;
using Godot;

namespace Knight.Prologue.Residents;

// Saved data only; a loader must configure the owning actor BEFORE AddChild.
public partial class ResidentSceneBinding : Node
{
#if REFCOUNTED_FATAL_IDENTITY
    internal readonly record struct ProfileAssignmentState(long BindingSerial, int Calls, int NullCalls,
        bool LastWasNull, string LastManagedType, int LastManagedHash, long LastManagedSerial, int Faults);
    private readonly long _assignmentBindingSerial = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.NextSerial();
    private ResidentSubtypeProfile _profile;
    private int _assignmentCalls, _assignmentNullCalls, _assignmentHash, _assignmentFaults;
    private long _assignmentSerial;
    private bool _assignmentWasNull;
    private string _assignmentType = "";
    internal ProfileAssignmentState ProfileAssignments => new(_assignmentBindingSerial, _assignmentCalls,
        _assignmentNullCalls, _assignmentWasNull, _assignmentType, _assignmentHash, _assignmentSerial, _assignmentFaults);
    [Export] public ResidentSubtypeProfile Profile
    {
        get => _profile;
        set
        {
            _profile = value; // Preserve the assignment even if its observation fails.
            try
            {
                _assignmentCalls++;
                _assignmentWasNull = ReferenceEquals(value, null);
                if (_assignmentWasNull) _assignmentNullCalls++;
                _assignmentType = _assignmentWasNull ? "" : value.GetType().FullName;
                _assignmentHash = _assignmentWasNull ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
                _assignmentSerial = _assignmentWasNull ? 0 : value.Task14ManagedSerial;
            }
            catch { _assignmentFaults++; }
        }
    }
#else
    [Export] public ResidentSubtypeProfile Profile { get; set; }
#endif
    [Export] public ResidentSceneState PresentationState { get; set; } = ResidentSceneState.ExplicitPlaceholder;

    public static CharacterBody2D Instantiate(PackedScene scene, ResidentSubtypeDefinition expected,
        ResidentPopulationDefinition slot, bool allowPlaceholder = false)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(slot);
        if (!GodotObject.IsInstanceValid(scene) || !Enum.IsDefined(expected.SceneState) ||
            expected.SceneState == ResidentSceneState.Unimplemented || string.IsNullOrWhiteSpace(expected.ScenePath) ||
            Normalize(scene.ResourcePath) != Normalize(expected.ScenePath) ||
            Normalize(expected.ScenePath) == Normalize(expected.ProbeScenePath))
            throw new ArgumentException("Load the distinct saved subtype leaf, not an unimplemented slot or probe alias.");

#if REFCOUNTED_FATAL_IDENTITY
        var assignmentOperation = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.BeginResidentAssignmentOperation(
            slot.StableId, expected.ScenePath);
        var profileSource = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ResidentProfileTraceActive &&
            expected.Id == "scrap-scatter"
            ? Knight.Tests.Diagnostics.Task14NativeResourceIdentity.CaptureResidentProfileSource(scene, slot.StableId, expected.ScenePath)
            : default;
#endif
        Node root = scene.Instantiate();
        try
        {
            ResidentSceneBinding binding = root.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
            // Capture only operands the original short-circuit gate evaluates.
            // A preparation rejection must identify its actual condition without
            // adding another native traversal, resource query or admission path.
            int? bindingCount = null;
            bool? profileMissing = null;
            ResidentSceneState? actualState = null;
#if REFCOUNTED_FATAL_IDENTITY
            ResidentSubtypeProfile evaluatedProfile = null;
#endif
            if (root is not CharacterBody2D actor || (bindingCount = CountBindings(root)) != 1 ||
#if REFCOUNTED_FATAL_IDENTITY
                (profileMissing = (evaluatedProfile = binding?.Profile) == null) == true ||
#else
                (profileMissing = binding?.Profile == null) == true ||
#endif
                !Enum.IsDefined((actualState = binding.PresentationState).Value) || binding.PresentationState != expected.SceneState ||
                binding.PresentationState == ResidentSceneState.Unimplemented ||
                !allowPlaceholder && binding.PresentationState != ResidentSceneState.Implemented)
            {
#if REFCOUNTED_FATAL_IDENTITY
                bool assignmentRowWritten = Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResidentAssignment(assignmentOperation,
                    binding, evaluatedProfile, profileMissing.HasValue, profileMissing, expected.Id == "scrap-scatter");
                if (expected.Id == "scrap-scatter")
                    Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResidentRejectionSource(assignmentOperation,
                        scene, assignmentRowWritten, profileMissing);
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResidentProfileInstance(profileSource,
                    evaluatedProfile, profileMissing.HasValue);
#endif
                throw new ArgumentException("Require one exported binding and explicit admission for placeholder visuals. " +
                    $"root_is_body={root is CharacterBody2D} named_binding={binding is not null} " +
                    $"binding_count={bindingCount?.ToString() ?? "not_evaluated"} " +
                    $"profile_missing={profileMissing?.ToString() ?? "not_evaluated"} " +
                    $"actual_state={(actualState.HasValue ? ((int)actualState.Value).ToString() : "not_evaluated")} " +
                    $"expected_state={(int)expected.SceneState} allow_placeholder={allowPlaceholder}");
            }
#if REFCOUNTED_FATAL_IDENTITY
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResidentAssignment(assignmentOperation,
                binding, evaluatedProfile, profileMissing.HasValue, profileMissing, expected.Id == "scrap-scatter");
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.ObserveResidentProfileInstance(profileSource,
                evaluatedProfile, profileMissing.HasValue);
            Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.GateSnapshot(evaluatedProfile, actor, binding, scene,
                assignmentOperation.Operation);
#endif
            binding.Profile.ConfigureActor(actor, expected, slot);
            return actor;
        }
        catch { root.Free(); throw; }
    }

    private static string Normalize(string path) => path?.Replace('\\', '/') ?? "";
    private static int CountBindings(Node root)
    {
        int count = root is ResidentSceneBinding ? 1 : 0;
        foreach (Node child in root.GetChildren()) count += CountBindings(child);
        return count;
    }
}
