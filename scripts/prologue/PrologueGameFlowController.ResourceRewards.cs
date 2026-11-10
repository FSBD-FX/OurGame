using Godot;
using System;
using System.IO;
using Knight.Prologue.Collections;
using Knight.Save;

namespace Knight.Prologue;

public partial class PrologueGameFlowController
{
    internal ResourceRewardClaimResult TryClaimResourceReward(PrologueResourceCache cache, CharacterBody2D actor)
    {
        if(!IsInstanceValid(cache) || !cache.IsBoundTo(_level) || !cache.HasInteractionAuthorization(actor) ||
            !IsInsideTree() || IsQueuedForDeletion() || IsApplicationQuitting || _startup!=null ||
            _awaitingLayoutChoice || BasinQualitySampleMode || State!=PrologueGameFlowState.Playing)
            return ResourceRewardClaimResult.Unavailable;
        string id=cache.RewardId;
        if(!ResourceRewardCatalog.TryGet(id,out _))return ResourceRewardClaimResult.InvalidId;
        if(_level.Progress.ClaimedRewardIds.Contains(id))return ResourceRewardClaimResult.AlreadyClaimed;
        PrologueSaveData candidate;
        try
        {
            if(!CanWriteFullWorldSlot())return ResourceRewardClaimResult.SaveFailed;
            candidate=_level.Progress.ToSave(Save);
            if(_minimap!=null)candidate.ExplorationMap=_minimap.CaptureExploration();
            candidate.ClaimedRewardIds.Add(id);
            candidate.Objective=_completion.CurrentObjective.ToString();
            candidate=SelectedNormalize(candidate);
            SaveSelected(candidate);
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or InvalidDataException or
            NotSupportedException or ArgumentException or System.Security.SecurityException)
        {return ResourceRewardClaimResult.SaveFailed;}
        // No await, public callback or external game-state mutation between durable commit and live publication.
        if(!_level.Progress.TryClaimResourceReward(id))throw new InvalidOperationException("Committed resource claim unexpectedly already exists.");
        Save=candidate;
        HasSave=IsContinueSaveAllowed();
        return ResourceRewardClaimResult.Claimed;
    }
}
