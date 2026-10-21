using System;
using System.IO;

namespace Knight.Tests.Presentation;

internal static class KeyboardInputTraceSidecar
{
    internal const string FileName="task11-keyboard-event-tail-v1.json";
    internal const string FocusFileName="task4-os-focus-tail-v1.json";
    internal static string Write(bool accepted,string root,KeyboardInputTraceBuffer trace,string outcome)
    {
        if(!accepted) throw new InvalidOperationException("route isolated storage has not been accepted");
        if(trace==null || !trace.Frozen) throw new InvalidOperationException("keyboard evidence must be frozen before serialization");
        return WriteJson(root, FileName, () => trace.CreateJson(outcome));
    }
    internal static string WriteFocus(bool accepted,string root,KeyboardFocusTraceBuffer trace,string outcome)
    {
        if(!accepted) throw new InvalidOperationException("route isolated storage has not been accepted");
        if(trace==null || !trace.Frozen) throw new InvalidOperationException("focus evidence must be frozen before serialization");
        return WriteJson(root, FocusFileName, () => trace.CreateJson(outcome));
    }
    private static string WriteJson(string root,string fileName,Func<string> createJson)
    {
        if(string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root)) throw new IOException("absolute accepted storage required");
        if(!Directory.Exists(root)) throw new IOException("accepted storage no longer exists");
        for(DirectoryInfo current=new(root);current!=null;current=current.Parent)
            if(!current.Exists || (current.Attributes & FileAttributes.ReparsePoint)!=0)
                throw new IOException("accepted storage has missing/reparse ancestor");
        string json=createJson(); // Serialize before creating any destination.
        string path=Path.Combine(root,fileName);
        using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        using(var writer=new StreamWriter(stream)) writer.Write(json);
        return path; // A partial/failed write never returns a written receipt.
    }
}
