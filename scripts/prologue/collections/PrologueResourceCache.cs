using Godot;
using Knight.Interactions;
using Knight.Combat;
using Knight.Player;
using Knight.Localization;

namespace Knight.Prologue.Collections;

public enum ResourceRewardClaimResult { Unavailable, InvalidId, AlreadyClaimed, SaveFailed, Claimed }

// Runtime component with replaceable AvailableVisual/ClaimedVisual/Status children; no final art is embedded.
public partial class PrologueResourceCache : Node2D, IEnvironmentInteractable
{
    [Signal] public delegate void ClaimedEventHandler(string stableId);
    [Export] public string RewardId { get; set; } = string.Empty;
    public string StableId => RewardId;
    public Node2D InteractionNode => this;
    public string InteractionTextKey => ResourceRewardCatalog.TryGet(RewardId,out var d) && d.IsChest
        ? "interaction.resource_cache" : "interaction.resource_pile";
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private bool _claiming;
    public bool IsClaimed => IsInstanceValid(_level) && _level.Progress.ClaimedRewardIds.Contains(RewardId);
    public int FeedbackCount { get; private set; }
    public ResourceRewardClaimResult LastResult { get; private set; } = ResourceRewardClaimResult.Unavailable;
    public string LastFeedbackText { get; private set; } = string.Empty;
    public void BindRuntime(PrologueLevelController level)
    {
        _level=level;
        _flow=IsInstanceValid(level)?level.GetNodeOrNull<PrologueGameFlowController>("GameFlow"):null;
        RefreshVisuals();
    }
    public override void _EnterTree()=>AddToGroup(EnvironmentInteractionController.InteractableGroup);
    public override void _Ready()=>RefreshVisuals();
    public override void _Process(double delta)=>RefreshVisuals();
    public bool CanInteract(CharacterBody2D actor) =>
        IsInsideTree() && !IsQueuedForDeletion() && IsVisibleInTree() && !GetTree().Paused &&
        IsInstanceValid(_level) && _level.IsInsideTree() && !_level.IsQueuedForDeletion() && _level.IsAncestorOf(this) &&
        IsInstanceValid(_flow) && _flow.IsInsideTree() && !_flow.IsQueuedForDeletion() && !_flow.IsApplicationQuitting &&
        _flow.State==PrologueGameFlowState.Playing && IsInstanceValid(actor) && ReferenceEquals(actor,_level.Player) &&
        actor.IsInsideTree() && !actor.IsQueuedForDeletion() && actor.GetWorld2D()==GetWorld2D() &&
        actor.GetNodeOrNull<HealthComponent>("Health") is {CurrentHealth:>0} &&
        ResourceRewardCatalog.TryGet(RewardId,out _) && !IsClaimed;

    internal bool HasInteractionAuthorization(CharacterBody2D actor) => CanInteract(actor) &&
        actor.GetNodeOrNull<PlayerInputReader>("Input") is {Current.InteractPressed:true} &&
        actor.GetNodeOrNull<EnvironmentInteractionController>("EnvironmentInteraction") is {} controller &&
        ReferenceEquals(controller.CurrentCandidate,this);

    internal bool IsBoundTo(PrologueLevelController level)=>ReferenceEquals(_level,level);

    public bool TryInteract(CharacterBody2D actor)
    {
        if(_claiming || !HasInteractionAuthorization(actor))return false;
        _claiming=true;
        try
        {
            LastResult=_flow.TryClaimResourceReward(this,actor);
            RefreshVisuals();
            if(LastResult!=ResourceRewardClaimResult.Claimed)return false;
            FeedbackCount++;
            // All durable/live/presentation changes precede this callback. It may start a new session or free this node.
            EmitSignal(SignalName.Claimed,RewardId);
            return true;
        }
        finally{_claiming=false;}
    }
    private void RefreshVisuals()
    {
        bool claimed=IsClaimed;
        if(GetNodeOrNull<Node2D>("AvailableVisual") is {} available)available.Visible=!claimed;
        if(GetNodeOrNull<Node2D>("ClaimedVisual") is {} spent)spent.Visible=claimed;
        string text=string.Empty;
        if(IsInstanceValid(_flow) && PrologueStrings.KeyCount(_flow.Settings.Language)>0)
        {
            if(claimed && ResourceRewardCatalog.TryGet(RewardId,out var reward))
                text=$"{PrologueStrings.Get("resource."+reward.Resource,_flow.Settings.Language)} ×{reward.Quantity}";
            else if(LastResult==ResourceRewardClaimResult.SaveFailed)
                text=PrologueStrings.Get("resource_reward.save_failed",_flow.Settings.Language);
        }
        LastFeedbackText=text;
        if(GetNodeOrNull<Label>("Status") is {} status && status.Text!=text)status.Text=text;
    }
}
