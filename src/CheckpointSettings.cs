using System;
using System.IO;
using System.Xml.Serialization;
public class CheckpointSettings {
 public string SaveKey = "F5";
 public string LoadKey = "F9";
 public bool ConfirmSave = true;
 public bool ShowStatusBar = true;
 // Retained only to read configuration files from development test builds.
 public bool Chinese = true;
 public string Language = "";
 public bool LanguageChosen = false;
 public void NormalizeLanguage() {
  if(string.IsNullOrEmpty(Language)) Language=Chinese?"zh-Hans":"en";
  else if(!CheckpointLocalization.Has(Language)) {Language="en"; LanguageChosen=false;}
 }
 public static CheckpointSettings Read(string path, bool chinese) {
  if(!File.Exists(path)) return new CheckpointSettings { Chinese=chinese };
  using(var reader=new StreamReader(path)) return (CheckpointSettings)new XmlSerializer(typeof(CheckpointSettings)).Deserialize(reader);
 }
 public void Write(string path) { AtomicCheckpointFile.Write(path, delegate(string temp) { using(var writer=new StreamWriter(temp)) new XmlSerializer(typeof(CheckpointSettings)).Serialize(writer,this); }); }
}
public static class AtomicCheckpointFile {
 public static void Write(string path, Action<string> write) {
  string temp=path+".tmp";
  try {
   write(temp);
   if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
  } finally { if(File.Exists(temp)) File.Delete(temp); }
 }
}


