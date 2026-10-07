using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
public class PatchTool {
 static MethodDefinition Validate(AssemblyDefinition game) {
  var owner=game.MainModule.Types.SingleOrDefault(t=>t.FullName=="Saviour");
  var state=game.MainModule.Types.SingleOrDefault(t=>t.FullName=="SaveState");
  if(owner==null || state==null) throw new Exception("Unsupported game: Saviour/SaveState missing. Mono builds only.");
  if(!owner.Methods.Any(m=>m.Name=="Save" && m.Parameters.Count==0 && m.ReturnType.FullName=="SaveState") ||
     !owner.Methods.Any(m=>m.Name=="Load" && m.Parameters.Count==1 && m.Parameters[0].ParameterType.FullName=="SaveState") ||
     !owner.Methods.Any(m=>m.Name=="SaveGameNow" && m.Parameters.Count==1 && m.Parameters[0].ParameterType.FullName=="System.Boolean")) throw new Exception("Unsupported save API.");
  foreach(var name in new[]{"pc","playerTransform"}) if(!owner.Fields.Any(f=>f.Name==name)) throw new Exception("Unsupported player fields.");
  foreach(var name in new[]{"rbPositions","rbAngles","rbLinearVelocities","rbAngularVelocities"}) if(!state.Properties.Any(p=>p.Name==name)) throw new Exception("Unsupported checkpoint format.");
  var update=owner.Methods.Single(m=>m.Name=="Update" && m.Parameters.Count==0);
  if(!update.HasBody) throw new Exception("Missing managed update body."); return update;
 }
 static bool IsHook(Instruction i) { var m=i.Operand as MethodReference; return i.OpCode==OpCodes.Call && m!=null && m.DeclaringType.FullName=="QuickCheckpoint" && m.Name=="Tick"; }
 static void RemoveHook(AssemblyDefinition game,MethodDefinition update) {
  var calls=update.Body.Instructions.Where(IsHook).ToArray();
  if(calls.Length>1) throw new Exception("Unexpected multiple checkpoint hooks.");
  if(calls.Length==0) return;
  var call=calls[0]; var arg=call.Previous;
  if(arg==null || arg.OpCode!=OpCodes.Ldarg_0 || arg!=update.Body.Instructions[0]) throw new Exception("Unexpected checkpoint hook location.");
  var il=update.Body.GetILProcessor(); il.Remove(call); il.Remove(arg);
  foreach(var reference in game.MainModule.AssemblyReferences.Where(r=>r.Name=="QuickCheckpoint").ToArray()) game.MainModule.AssemblyReferences.Remove(reference);
 }
 public static int Main(string[] args) {
  try {
   if(args.Length<2) throw new Exception("Usage: PatchTool inspect game.dll | clean input output | patch input mod managed output");
   var resolver=new DefaultAssemblyResolver(); resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
   if(args[0]=="patch") { resolver.AddSearchDirectory(args[3]); resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))); }
   using(var game=AssemblyDefinition.ReadAssembly(args[1],new ReaderParameters { AssemblyResolver=resolver })) {
    var update=Validate(game);
    if(args[0]=="inspect") { Console.WriteLine(update.Body.Instructions.Any(IsHook)?"PATCHED":"CLEAN"); return 0; }
    RemoveHook(game,update);
    string output;
    if(args[0]=="clean") output=args[2];
    else if(args[0]=="patch") {
     using(var mod=AssemblyDefinition.ReadAssembly(args[2])) {
      var tick=game.MainModule.ImportReference(mod.MainModule.Types.Single(t=>t.Name=="QuickCheckpoint").Methods.Single(m=>m.Name=="Tick"));
      var il=update.Body.GetILProcessor(); var first=update.Body.Instructions[0];
      il.InsertBefore(first,il.Create(OpCodes.Ldarg_0)); il.InsertBefore(first,il.Create(OpCodes.Call,tick));
      output=args[4]; game.Write(output);
     }
     using(var check=AssemblyDefinition.ReadAssembly(output)) if(Validate(check).Body.Instructions.Count(IsHook)!=1) throw new Exception("Output verification failed.");
     Console.WriteLine("Patch verified."); return 0;
    } else throw new Exception("Unknown command.");
    game.Write(output);
    using(var check=AssemblyDefinition.ReadAssembly(output)) if(Validate(check).Body.Instructions.Any(IsHook)) throw new Exception("Clean verification failed.");
    Console.WriteLine("Clean backup verified."); return 0;
   }
  } catch(Exception e) { Console.Error.WriteLine(e.Message); return 1; }
 }
}
