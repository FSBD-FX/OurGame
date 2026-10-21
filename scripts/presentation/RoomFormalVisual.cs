using System.Collections.Generic;
using Godot;
using Knight.Prologue;

namespace Knight.Presentation;

public partial class RoomFormalVisual : Node2D
{
    [Export] public string RoomKey { get; set; } = "environment-kit";
    [Export] public PrologueRoomId Room { get; set; }
    [Export] public string LandmarkId { get; set; } = "environment-kit";
    [Export] public Vector2 CameraBasis { get; set; } = new(1280, 720);
    [Export] public float AtmosphereSpeed { get; set; } = 0.35f;

    public float AtmospherePhase { get; private set; }
    private Vector2 _atmosphereParallax;
    private Vector2 _lastCameraVelocity;
    private Rect2[] _callerProtectedScreenRegions = [];
    private readonly List<Rect2> _liveProtectedScreenRegions = new(32);
    private readonly List<Rect2> _mergedProtectedScreenRegions = new(32);
    private readonly List<RoomProtectionRegionAdapter.Candidate> _protectionCandidates = new(32);
    private readonly List<ForegroundOccluder> _foregroundOccluders = new(8);
    private SceneTree _candidateTree;
    private Node _front;
    private Node2D _atmosphere;
    private bool _protectionCandidatesDirty = true;

    public int ProtectionCandidateRefreshCount { get; private set; }

    public override void _Ready()
    {
        _front = GetNodeOrNull("Front");
        _atmosphere = GetNodeOrNull<Node2D>("Atmosphere");
        RefreshForegroundOccluders();
        _candidateTree = GetTree();
        _candidateTree.NodeAdded += OnTreeTopologyChanged;
        _candidateTree.NodeRemoved += OnTreeTopologyChanged;
        _protectionCandidatesDirty = true;
        RoomVisualDescriptor descriptor = RoomVisualCatalog.For(Room);
        if (string.IsNullOrWhiteSpace(LandmarkId) || LandmarkId == "environment-kit")
            LandmarkId = descriptor.Landmark;
        Present(descriptor, Vector2.Zero, []);
    }

    public override void _ExitTree()
    {
        if (_candidateTree != null)
        {
            _candidateTree.NodeAdded -= OnTreeTopologyChanged;
            _candidateTree.NodeRemoved -= OnTreeTopologyChanged;
            _candidateTree = null;
        }
        _protectionCandidates.Clear();
        _foregroundOccluders.Clear();
        _front = null;
        _atmosphere = null;
        _protectionCandidatesDirty = true;
    }

    public override void _Process(double delta)
    {
        ApplyProtectedRegions();
        AdvanceAtmosphere((float)delta);
    }

    public void Present(RoomVisualDescriptor descriptor, Vector2 cameraVelocity,
        IReadOnlyList<Rect2> protectedScreenRegions)
    {
        if (descriptor == null) throw new System.ArgumentNullException(nameof(descriptor));
        Room = descriptor.Room;
        LandmarkId = descriptor.Landmark;
        _lastCameraVelocity = cameraVelocity;
        _callerProtectedScreenRegions = CopyRegions(protectedScreenRegions);
        SetRolePosition("Far", -cameraVelocity * descriptor.FarParallax);
        SetRolePosition("Back", -cameraVelocity * descriptor.FarParallax);
        SetRolePosition("Mid", -cameraVelocity * descriptor.MidParallax);
        Vector2 playPosition = -cameraVelocity * (descriptor.MidParallax * .35f);
        SetRolePosition("Platforms", Vector2.Zero);
        SetRolePosition("Props", playPosition);
        SetRolePosition("Front", -cameraVelocity * descriptor.FrontParallax);
        _atmosphereParallax = -cameraVelocity * (descriptor.FarParallax * .5f);

        ApplyProtectedRegions();
    }

    private void ApplyProtectedRegions()
    {
        if (_front == null) return;
        if (_protectionCandidatesDirty)
        {
            RefreshForegroundOccluders();
            RoomProtectionRegionAdapter.CollectCandidatesInto(this, _protectionCandidates);
            _protectionCandidatesDirty = false;
            ProtectionCandidateRefreshCount++;
        }
        RoomProtectionRegionAdapter.CollectFromCandidatesInto(
            _protectionCandidates, _liveProtectedScreenRegions);
        _mergedProtectedScreenRegions.Clear();
        _mergedProtectedScreenRegions.AddRange(_callerProtectedScreenRegions);
        _mergedProtectedScreenRegions.AddRange(_liveProtectedScreenRegions);
        for (int index = 0; index < _foregroundOccluders.Count; index++)
            _foregroundOccluders[index].SetProtectedRegions(_mergedProtectedScreenRegions);
    }

    private void OnTreeTopologyChanged(Node node)
    {
        if (node is ForegroundOccluder || RoomProtectionRegionAdapter.IsCandidateNode(node))
            _protectionCandidatesDirty = true;
    }

    private void RefreshForegroundOccluders()
    {
        _foregroundOccluders.Clear();
        if (_front == null) return;
        int childCount = _front.GetChildCount();
        for (int index = 0; index < childCount; index++)
            if (_front.GetChild(index) is ForegroundOccluder occluder)
                _foregroundOccluders.Add(occluder);
    }

    public void AdvanceAtmosphere(float delta)
    {
        if (!float.IsFinite(delta) || delta <= 0f) return;
        AtmospherePhase += delta * Mathf.Max(0f, AtmosphereSpeed);
        if (_atmosphere != null)
            _atmosphere.Position = _atmosphereParallax + new Vector2(Mathf.Sin(AtmospherePhase) * 7f,
                Mathf.Cos(AtmospherePhase * 0.61f) * 3f);
    }

    public static bool ForegroundCoverageAvoidsProtectedRegions(Rect2 coverage,
        IReadOnlyList<Rect2> protectedRegions)
    {
        if (!FinitePositive(coverage)) return false;
        if (protectedRegions == null) return true;
        for (int index = 0; index < protectedRegions.Count; index++)
        {
            Rect2 protectedRegion = protectedRegions[index];
            if (!FinitePositive(protectedRegion)) continue;
            if (coverage.Intersects(protectedRegion)) return false;
        }
        return true;
    }

    private void SetRolePosition(string path, Vector2 position)
    {
        Node2D role = GetNodeOrNull<Node2D>(path);
        if (role != null) role.Position = position;
    }

    private static Rect2[] CopyRegions(IReadOnlyList<Rect2> regions)
    {
        if (regions == null || regions.Count == 0) return [];
        var copy = new Rect2[regions.Count];
        for (int index = 0; index < copy.Length; index++) copy[index] = regions[index];
        return copy;
    }

    private static bool FinitePositive(Rect2 value) =>
        float.IsFinite(value.Position.X) && float.IsFinite(value.Position.Y) &&
        float.IsFinite(value.Size.X) && value.Size.X > 0f &&
        float.IsFinite(value.Size.Y) && value.Size.Y > 0f;
}
