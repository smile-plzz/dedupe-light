using DedupeLight;
using System.Collections.Concurrent;

var root=Path.Combine(Path.GetTempPath(),"dedupe-checks-"+Guid.NewGuid());
Directory.CreateDirectory(root);
try
{
    var data=Enumerable.Repeat((byte)7,200000).ToArray();
    File.WriteAllBytes(Path.Combine(root,"a.bin"),data);
    File.WriteAllBytes(Path.Combine(root,"b.bin"),data);
    var cache=Path.Combine(root,"cache.json");
    var reports=new Reports();
    var options=new ScanOptions(0,true);
    var first=await DuplicateScanner.FindAsync(root,cache,options,reports,CancellationToken.None);
    Check(first.Items.Count==2&&first.FullHashes==2,"exact duplicates verified");
    var second=await DuplicateScanner.FindAsync(root,cache,options,reports,CancellationToken.None);
    Check(second.Items.Count==2&&second.CacheHits==2,"warm scan reuses hashes");
    // Change both samples identically but only one middle block: stale full hashes must not survive.
    data[0]=8;File.WriteAllBytes(Path.Combine(root,"a.bin"),data);
    data[100000]=9;File.WriteAllBytes(Path.Combine(root,"b.bin"),data);
    File.SetLastWriteTimeUtc(Path.Combine(root,"a.bin"),DateTime.UtcNow.AddMinutes(1));
    File.SetLastWriteTimeUtc(Path.Combine(root,"b.bin"),DateTime.UtcNow.AddMinutes(1));
    var changed=await DuplicateScanner.FindAsync(root,cache,options,reports,CancellationToken.None);
    Check(changed.Items.Count==0&&changed.FullHashes==2,"changed files invalidate full hashes");
    File.WriteAllText(cache,"invalid JSON");
    Check((await DuplicateScanner.FindAsync(root,cache,options,reports,CancellationToken.None)).Items.Count==0,"corrupt cache recovers");
    using var cancelled=new CancellationTokenSource();cancelled.Cancel();
    try{await DuplicateScanner.FindAsync(root,cache,options,reports,cancelled.Token);throw new Exception("Cancellation failed");}
    catch(OperationCanceledException){Console.WriteLine("PASS cancellation");}
    // A context that fails on capture exposes accidental work scheduled back to a UI context.
    SynchronizationContext.SetSynchronizationContext(new RejectContext());
    var task=DuplicateScanner.FindAsync(root,cache,options,reports,CancellationToken.None);
    SynchronizationContext.SetSynchronizationContext(null);
    await task;
    Console.WriteLine("PASS scanner independent of caller synchronization context");
}
finally{Directory.Delete(root,true);}
static void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);}
sealed class Reports:IProgress<string>{public void Report(string message){}}
sealed class RejectContext:SynchronizationContext{public override void Post(SendOrPostCallback d,object? state)=>throw new Exception("Captured UI context");}
namespace DedupeLight { public static class MainForm { public static string FormatSize(long size)=>size.ToString(); } }
