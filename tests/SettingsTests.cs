using System;
using System.IO;
class SettingsTests {
 static void Check(bool value,string label) {if(!value) throw new Exception(label); Console.WriteLine("PASS "+label);}
 static int Main(string[] args) {
  string path=Path.Combine(args[0],"settings-test.xml");
  var defaults=CheckpointSettings.Read(path,false); defaults.NormalizeLanguage();
  Check(defaults.SaveKey=="F5" && defaults.LoadKey=="F9" && defaults.ConfirmSave && defaults.ShowStatusBar && defaults.Language=="en" && !defaults.LanguageChosen,"first launch requires language selection");
  var edited=new CheckpointSettings {SaveKey="F6",LoadKey="F8",ConfirmSave=false,ShowStatusBar=false,Language="fr",LanguageChosen=true}; edited.Write(path);
  var read=CheckpointSettings.Read(path,false); read.NormalizeLanguage();
  Check(read.SaveKey=="F6" && read.LoadKey=="F8" && !read.ConfirmSave && !read.ShowStatusBar && read.Language=="fr" && read.LanguageChosen,"language and shortcuts persist; no repeated first-run dialog");
  string original=File.ReadAllText(path); bool failed=false;
  try {AtomicCheckpointFile.Write(path,delegate(string temp){File.WriteAllText(temp,"partial"); throw new IOException("simulated write failure");});} catch(IOException){failed=true;}
  Check(failed && File.ReadAllText(path)==original && !File.Exists(path+".tmp"),"failed write preserves checkpoint and cleans temp");
  new CheckpointSettings().Write(path); Check(File.ReadAllText(path+".bak")==original,"overwrite retains previous checkpoint");
  string legacy=Path.Combine(args[0],"legacy.xml");
  File.WriteAllText(legacy,"<CheckpointSettings><SaveKey>F7</SaveKey><LoadKey>F8</LoadKey><ConfirmSave>true</ConfirmSave><ShowStatusBar>false</ShowStatusBar><Chinese>true</Chinese></CheckpointSettings>");
  var migrated=CheckpointSettings.Read(legacy,false); migrated.NormalizeLanguage();
  Check(migrated.Language=="zh-Hans" && !migrated.LanguageChosen && migrated.SaveKey=="F7" && !migrated.ShowStatusBar,"2.0 migration retains settings and asks for language once");
  migrated.Language="unsupported"; migrated.LanguageChosen=true; migrated.NormalizeLanguage(); Check(migrated.Language=="en" && !migrated.LanguageChosen,"unsupported language safely returns to selection");
  Check(CheckpointLocalization.Languages.Length==18,"18 supported languages");
  foreach(var language in CheckpointLocalization.Languages) {
   foreach(string key in CheckpointLocalization.Keys) {
    string text=CheckpointLocalization.Get(language.Code,key,"F6","F8");
    CheckpointLocalization.Get(language.Code,key,"2026-10-07","F9");
    if(string.IsNullOrWhiteSpace(text) || text.Contains("{0}") || text.Contains("{1}")) throw new Exception("Incomplete translation: "+language.Code+" "+key);
    int index=Array.IndexOf(CheckpointLocalization.Keys,key);
    int english=Array.FindIndex(CheckpointLocalization.Languages,l=>l.Code=="en");
    for(int n=0;n<2;n++) if(language.Text[index].Contains("{"+n+"}")!=CheckpointLocalization.Languages[english].Text[index].Contains("{"+n+"}")) throw new Exception("Placeholder mismatch: "+language.Code+" "+key);
   }
  }
  Check(true,"all 486 translated strings are present and format correctly");
  Check(!CheckpointInputPolicy.ConfirmPressed(true,false,true,10,10),"first key press only opens confirmation");
  Check(!CheckpointInputPolicy.ConfirmPressed(true,false,false,10,11),"holding save key does not confirm");
  Check(CheckpointInputPolicy.ConfirmPressed(true,false,true,10,12),"second press confirms while paused");
  Check(!CheckpointInputPolicy.ConfirmPressed(false,false,true,10,12) && !CheckpointInputPolicy.ConfirmPressed(true,true,true,10,12),"save key does not confirm language/settings/rebinding dialogs");
  return 0;
 }
}
