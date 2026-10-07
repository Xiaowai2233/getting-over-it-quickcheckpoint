using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
public class QuickCheckpoint : MonoBehaviour {
 const string Version="2.1.0";
 static QuickCheckpoint instance;
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 CheckpointSettings settings;
 KeyCode saveKey=KeyCode.F5, loadKey=KeyCode.F9;
 object saviour, pendingSave;
 string modal="", capture="", action="", status="", lastSave="";
 float previousScale=1, toastUntil;
 bool previousCursorVisible, languageReturnToSettings;
 CursorLockMode previousCursorLock;
 int confirmOpenedFrame;
 Vector2 languageScroll, settingsScroll;
 readonly Dictionary<string,Font> fonts=new Dictionary<string,Font>();
 static MethodInfo arabicFix;
 static string SavePath {get {return Path.Combine(Application.persistentDataPath,"QuickCheckpoint.xml");}}
 static string ConfigPath {get {return Path.Combine(Application.persistentDataPath,"QuickCheckpoint.settings.xml");}}
 string T(string key,params object[] args) {return Shape(CheckpointLocalization.Get(settings.Language,key,args),settings.Language);}
 static string Shape(string text,string language) {
  if(language!="ar") return text;
  if(arabicFix==null) {
   var type=Type.GetType("ArabicSupport.ArabicFixer, ArabicSupport",false);
   if(type!=null) arabicFix=type.GetMethod("Fix",new Type[]{typeof(string),typeof(bool),typeof(bool)});
  }
  return arabicFix==null?text:(string)arabicFix.Invoke(null,new object[]{text,true,false});
 }
 public static void Tick(object owner) {
  try {
   if(instance==null) {var go=new GameObject("QuickCheckpoint"); DontDestroyOnLoad(go); instance=go.AddComponent<QuickCheckpoint>(); instance.Initialize();}
   instance.saviour=owner; instance.HandleInput();
  } catch(Exception e) {Debug.LogError("QuickCheckpoint: "+e); if(instance!=null) instance.Notify(instance.T("Error"));}
 }
 void Initialize() {
  bool chinese=Application.systemLanguage==SystemLanguage.ChineseSimplified || Application.systemLanguage==SystemLanguage.ChineseTraditional;
  settings=new CheckpointSettings {Chinese=chinese};
  try {settings=CheckpointSettings.Read(ConfigPath,chinese);} catch(Exception e) {Debug.LogWarning("QuickCheckpoint settings: "+e.Message);}
  settings.NormalizeLanguage();
  saveKey=ParseKey(settings.SaveKey,KeyCode.F5); loadKey=ParseKey(settings.LoadKey,KeyCode.F9);
  if(saveKey==loadKey) loadKey=saveKey==KeyCode.F9?KeyCode.F5:KeyCode.F9;
  settings.SaveKey=saveKey.ToString(); settings.LoadKey=loadKey.ToString();
  if(File.Exists(SavePath)) lastSave=File.GetLastWriteTime(SavePath).ToString("MM-dd HH:mm:ss");
  Notify(T("Ready")); Debug.Log("QuickCheckpoint v"+Version+" ready");
 }
 Font FontFor(string language) {
  Font result; if(fonts.TryGetValue(language,out result)) return result;
  string[] names;
  switch(language) {
   case "zh-Hans": names=new string[]{"Microsoft YaHei","SimHei","Arial Unicode MS"}; break;
   case "zh-Hant": names=new string[]{"Microsoft JhengHei","Microsoft YaHei","Arial Unicode MS"}; break;
   case "ja": names=new string[]{"Yu Gothic","Meiryo","MS Gothic","Microsoft YaHei"}; break;
   case "ko": names=new string[]{"Malgun Gothic","Gulim","Arial Unicode MS"}; break;
   default: names=new string[]{"Segoe UI","Arial","Tahoma"}; break;
  }
  result=Font.CreateDynamicFontFromOSFont(names,18); fonts.Add(language,result); return result;
 }
 static KeyCode ParseKey(string name,KeyCode fallback) {try {var key=(KeyCode)Enum.Parse(typeof(KeyCode),name,true); return ValidKey(key)?key:fallback;} catch {return fallback;}}
 static bool ValidKey(KeyCode key) {return Enum.IsDefined(typeof(KeyCode),key) && key!=KeyCode.None && key!=KeyCode.Escape && key!=KeyCode.F10 && (int)key<(int)KeyCode.Mouse0;}
 void HandleInput() {
  if(action!="") {
   string todo=action; action="";
   if(todo=="save") {try {SaveCheckpoint(pendingSave);} finally {CloseModal();}}
   else if(todo=="close") CloseModal();
   else if(todo=="language") {languageReturnToSettings=true; modal="language";}
   else if(todo=="accept-language") {
    bool previous=settings.LanguageChosen; settings.LanguageChosen=true;
    try {settings.Write(ConfigPath);} catch {settings.LanguageChosen=previous; throw;}
    if(languageReturnToSettings) {modal="settings"; languageReturnToSettings=false;} else CloseModal();
   }
   return;
  }
  if(!settings.LanguageChosen && modal=="") {languageReturnToSettings=false; OpenModal("language"); return;}
  if(capture!="") return;
  if(modal=="language") return;
  if(CheckpointInputPolicy.ConfirmPressed(modal=="confirm",false,Input.GetKeyDown(saveKey),confirmOpenedFrame,Time.frameCount)) {
   try {SaveCheckpoint(pendingSave);} finally {CloseModal();} return;
  }
  if(Input.GetKeyDown(KeyCode.F10)) {if(modal=="settings") CloseModal(); else if(modal=="") OpenModal("settings"); return;}
  if(modal!="") {if(Input.GetKeyDown(KeyCode.Escape)) CloseModal(); return;}
  if(Time.timeScale<=0) return;
  if(Input.GetKeyDown(saveKey)) {
   pendingSave=saviour.GetType().GetMethod("Save",Flags).Invoke(saviour,null);
   if(settings.ConfirmSave) {confirmOpenedFrame=Time.frameCount; OpenModal("confirm");}
   else {SaveCheckpoint(pendingSave); pendingSave=null;}
  } else if(Input.GetKeyDown(loadKey)) LoadCheckpoint();
 }
 void SaveCheckpoint(object state) {
  if(state==null) throw new Exception("No captured checkpoint");
  var serializer=new XmlSerializer(state.GetType());
  AtomicCheckpointFile.Write(SavePath,delegate(string temp){using(var writer=new StreamWriter(temp)) serializer.Serialize(writer,state);});
  lastSave=File.GetLastWriteTime(SavePath).ToString("MM-dd HH:mm:ss"); Notify(T("Saved"));
 }
 void LoadCheckpoint() {
  if(!File.Exists(SavePath)) {Notify(T("NoSave",saveKey)); return;}
  var type=saviour.GetType().Assembly.GetType("SaveState"); object state;
  using(var reader=new StreamReader(SavePath)) state=new XmlSerializer(type).Deserialize(reader);
  var pt=(Transform)saviour.GetType().GetField("playerTransform",Flags).GetValue(saviour);
  int count=pt.GetComponentsInChildren<Rigidbody2D>().Length;
  foreach(string property in new string[]{"rbPositions","rbAngles","rbLinearVelocities","rbAngularVelocities"}) {
   var array=type.GetProperty(property).GetValue(state,null) as Array;
   if(array==null || array.Length!=count) throw new Exception("Checkpoint body data does not match this player: "+property);
  }
  var mode=Physics2D.simulationMode;
  try {saviour.GetType().GetMethod("Load",Flags).Invoke(saviour,new object[]{state});} finally {Physics2D.simulationMode=mode;}
  saviour.GetType().GetMethod("SaveGameNow",Flags).Invoke(saviour,new object[]{true}); Notify(T("Loaded"));
 }
 void OpenModal(string name) {
  previousScale=Time.timeScale; previousCursorVisible=Cursor.visible; previousCursorLock=Cursor.lockState;
  Time.timeScale=0; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; modal=name;
 }
 void CloseModal() {
  if(modal=="") return;
  try {if(modal=="settings") settings.Write(ConfigPath);}
  catch(Exception e) {Debug.LogError("QuickCheckpoint config: "+e); Notify(T("ConfigError"));}
  finally {
   modal=""; capture=""; pendingSave=null; Time.timeScale=previousScale; Cursor.lockState=previousCursorLock; Cursor.visible=previousCursorVisible;
   if(saviour!=null) {var pc=saviour.GetType().GetField("pc",Flags).GetValue(saviour); if(pc!=null) pc.GetType().GetMethod("PauseInput",Flags).Invoke(pc,new object[]{0.3f});}
  }
 }
 void Notify(string message) {status=message; toastUntil=Time.unscaledTime+3; Debug.Log("QuickCheckpoint: "+message);}
 void OnDestroy() {if(modal!="") CloseModal(); foreach(var font in fonts.Values) if(font!=null) Destroy(font); if(instance==this) instance=null;}
 void OnGUI() {
  if(settings==null) return;
  var oldFont=GUI.skin.font; GUI.skin.font=FontFor(settings.Language);
  var label=new GUIStyle(GUI.skin.label) {fontSize=18,wordWrap=true,alignment=settings.Language=="ar"?TextAnchor.UpperRight:TextAnchor.UpperLeft};
  var button=new GUIStyle(GUI.skin.button) {fontSize=17,wordWrap=true};
  var toggle=new GUIStyle(GUI.skin.toggle) {fontSize=17,wordWrap=true};
  try {
   float barWidth=Math.Min(680,Screen.width-24);
   if(settings.ShowStatusBar) {GUI.Box(new Rect(12,12,barWidth,42),""); GUI.Label(new Rect(22,18,barWidth-20,32),T("Bar",saveKey,loadKey),label);}
   if(Time.unscaledTime<toastUntil) {GUI.Box(new Rect((Screen.width-barWidth)/2,Screen.height-80,barWidth,60),""); GUI.Label(new Rect((Screen.width-barWidth)/2+10,Screen.height-72,barWidth-20,45),status,label);}
   if(modal=="") return;
   Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
   if(capture!="" && Event.current.type==EventType.KeyDown) {
    var key=Event.current.keyCode; Event.current.Use();
    if(key==KeyCode.Escape) capture="";
    else if(!ValidKey(key) || key==(capture=="save"?loadKey:saveKey)) Notify(T("KeyError"));
    else {if(capture=="save") {saveKey=key; settings.SaveKey=key.ToString();} else {loadKey=key; settings.LoadKey=key.ToString();} capture="";}
   }
   float width=Math.Min(modal=="language"?680:620,Screen.width-24);
   float height=Math.Min(modal=="confirm"?330:530,Screen.height-24);
   var rect=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);
   GUI.Box(rect,""); GUILayout.BeginArea(new Rect(rect.x+22,rect.y+18,rect.width-44,rect.height-36));
   GUILayout.Label(T(modal=="confirm"?"ConfirmTitle":modal=="language"?"ChooseTitle":"SettingsTitle"),new GUIStyle(label){fontSize=23}); GUILayout.Space(12);
   if(modal=="language") {
    GUILayout.Label(T("ChooseHint"),label); GUILayout.Space(6);
    languageScroll=GUILayout.BeginScrollView(languageScroll);
    for(int i=0;i<CheckpointLocalization.Languages.Length;i+=2) {
     GUILayout.BeginHorizontal();
     for(int j=i;j<Math.Min(i+2,CheckpointLocalization.Languages.Length);j++) {
      var language=CheckpointLocalization.Languages[j];
      var nativeStyle=new GUIStyle(button) {font=FontFor(language.Code)};
      string name=(language.Code==settings.Language?"✓ ":"")+Shape(language.NativeName,language.Code);
      if(GUILayout.Button(name,nativeStyle,GUILayout.Height(38))) {settings.Language=language.Code; status="";}
     }
     GUILayout.EndHorizontal();
    }
    GUILayout.EndScrollView(); GUILayout.Space(8);
    if(GUILayout.Button(T("Continue"),button,GUILayout.Height(42))) action="accept-language";
   } else if(modal=="confirm") {
    settingsScroll=GUILayout.BeginScrollView(settingsScroll);
    GUILayout.Label(T("ConfirmBody"),label); GUILayout.Space(12); GUILayout.Label(T("ConfirmKeyHint",saveKey),label);
    if(lastSave!="") GUILayout.Label(T("LastSave",lastSave),label);
    GUILayout.EndScrollView(); GUILayout.Space(8); GUILayout.BeginHorizontal();
    if(GUILayout.Button(T("Confirm"),button,GUILayout.Height(44))) action="save";
    if(GUILayout.Button(T("Cancel"),button,GUILayout.Height(44))) action="close"; GUILayout.EndHorizontal();
   } else {
    settingsScroll=GUILayout.BeginScrollView(settingsScroll);
    settings.ConfirmSave=GUILayout.Toggle(settings.ConfirmSave,T("ConfirmToggle"),toggle,GUILayout.MinHeight(32));
    settings.ShowStatusBar=GUILayout.Toggle(settings.ShowStatusBar,T("BarToggle"),toggle,GUILayout.MinHeight(32)); GUILayout.Space(12);
    GUILayout.BeginHorizontal(); GUILayout.Label(T("Language"),label);
    if(GUILayout.Button(Shape(CheckpointLocalization.Name(settings.Language),settings.Language),button,GUILayout.Width(210),GUILayout.Height(36))) action="language"; GUILayout.EndHorizontal(); GUILayout.Space(12);
    GUILayout.BeginHorizontal(); GUILayout.Label(T("SaveKey"),label); if(GUILayout.Button(saveKey.ToString(),button,GUILayout.Width(150),GUILayout.Height(34))) capture="save"; GUILayout.EndHorizontal();
    GUILayout.BeginHorizontal(); GUILayout.Label(T("LoadKey"),label); if(GUILayout.Button(loadKey.ToString(),button,GUILayout.Width(150),GUILayout.Height(34))) capture="load"; GUILayout.EndHorizontal();
    GUILayout.Space(10); GUILayout.Label(T(capture!=""?"BindHint":"SettingsHint"),label);
    GUILayout.EndScrollView(); GUILayout.Space(8); GUILayout.BeginHorizontal();
    if(GUILayout.Button(T("Reset"),button,GUILayout.Height(44))) {string language=settings.Language; bool chosen=settings.LanguageChosen; settings=new CheckpointSettings {Language=language,LanguageChosen=chosen}; saveKey=KeyCode.F5; loadKey=KeyCode.F9; capture="";}
    if(GUILayout.Button(T("Close"),button,GUILayout.Height(44))) action="close"; GUILayout.EndHorizontal();
   }
   GUILayout.EndArea();
  } finally {GUI.skin.font=oldFont;}
 }
}
