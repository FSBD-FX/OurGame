using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Prologue;

public partial class PrologueGameFlowController
{
    private PrologueWorldProfile SelectedProfile=>IsInstanceValid(_level)?_level.WorldProfile:null;
    private PrologueSaveData SelectedNormalize(PrologueSaveData data)=>data.Normalize(SelectedProfile);
    private PrologueSaveData NewSelectedSave()=>SelectedProfile!=null?PrologueSaveData.NewForProfile(SelectedProfile):
        BasinQualitySampleMode?new PrologueSaveData():PrologueSaveData.NewForLayout(CanyonWorldLayout.CurrentLayoutVersion);
    private void SaveSelected(PrologueSaveData data,bool preserveExisting=false)=>
        PrologueSaveStore.SaveAtomic(Absolute(SavePath),data,preserveExisting,SelectedProfile);
}
