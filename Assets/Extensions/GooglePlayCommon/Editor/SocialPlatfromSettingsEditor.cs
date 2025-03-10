
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(SocialPlatfromSettings))]
public class SocialPlatfromSettingsEditor : Editor {





	static GUIContent SdkVersion   = new GUIContent("Plugin Version [?]", "This is Plugin version.  If you have problems or compliments please include this so we know exactly what version to look out for.");
	static GUIContent SupportEmail = new GUIContent("Support [?]", "If you have any technical quastion, feel free to drop an e-mail");



	private const string IOS_SOURCE_PATH 			= "Plugins/StansAssets/IOS/";
	private const string IOS_DESTANATION_PATH 		= "Plugins/IOS/";
	private const string ANDROID_SOURCE_PATH 		= "Plugins/StansAssets/Android/";
	private const string ANDROID_DESTANATION_PATH 	= "Plugins/Android/";




	void Awake() {
		if(IsInstalled && IsUpToDate) {
			PluginsInstalationUtil.IOS_Install_SocialPart();
			UpdateManifest();

			UpdatePluginDefines();
		}
	}


	public static string SP_FB_API_v7_Path = "Extensions/GooglePlayCommon/Social/Facebook/Manage/SP_FB_API_v7.cs";
	public static string SP_FB_GrapRequest_V7_Path = "Extensions/GooglePlayCommon/Social/Facebook/Tasks/FB_GrapRequest_V7.cs";

	public static string SP_FB_API_v6_Path = "Extensions/GooglePlayCommon/Social/Facebook/Manage/SP_FB_API_v6.cs";
	public static string SP_FB_GrapRequest_V6_Path = "Extensions/GooglePlayCommon/Social/Facebook/Tasks/FB_GrapRequest_V6.cs";


	public static void UpdatePluginDefines() {

		if(PluginsInstalationUtil.IsFacebookInstalled) {
			int version = SA_ModulesInfo.FB_SDK_MajorVersionCode;
			if(version != 0) {
				if(version >= 7) {
					SA_EditorTool.ChnageDefineState(SP_FB_API_v7_Path, "FBV7_API_ENABLED", 	true);
					SA_EditorTool.ChnageDefineState(SP_FB_GrapRequest_V7_Path, "FBV7_API_ENABLED", 	true);
				} else {
					SA_EditorTool.ChnageDefineState(SP_FB_API_v6_Path, "FBV6_API_ENABLED", 	true);
					SA_EditorTool.ChnageDefineState(SP_FB_GrapRequest_V6_Path, "FBV6_API_ENABLED", 	true);
				}
			} else {
				DisableFBAPI();
			}
		} else {
			DisableFBAPI();
		}
	}

	public static void DisableFBAPI() {
		SA_EditorTool.ChnageDefineState(SP_FB_API_v6_Path, "FBV6_API_ENABLED", 	false);
		SA_EditorTool.ChnageDefineState(SP_FB_GrapRequest_V6_Path, "FBV6_API_ENABLED", 	false);
		
		SA_EditorTool.ChnageDefineState(SP_FB_API_v7_Path, "FBV7_API_ENABLED", 	false);
		SA_EditorTool.ChnageDefineState(SP_FB_GrapRequest_V7_Path, "FBV7_API_ENABLED", 	false);
	}

	public override void OnInspectorGUI() {


		
		#if UNITY_WEBPLAYER
		EditorGUILayout.HelpBox("Editing Mobile Social Settins not avaliable with web player platfrom. Please swith to any other platfrom under Build Seting menu", MessageType.Warning);
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.Space();
		if(GUILayout.Button("Switch To Android",  GUILayout.Width(130))) {
			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);
		}

		if(GUILayout.Button("Switch To IOS",  GUILayout.Width(130))) {

			#if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6
			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iPhone);
			#else
			EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);
			#endif

		}
		EditorGUILayout.EndHorizontal();
		
		if(Application.isEditor) {
			return;
		}

		#endif

		GUI.changed = false;



		GeneralOptions();
		GeneralSettings();
		EditorGUILayout.Space();
		SocialPlatfromSettingsHelper.FacebookSettings();
		EditorGUILayout.Space();
		SocialPlatfromSettingsHelper.TwitterSettings();
		EditorGUILayout.Space();

		AboutGUI();
	



		if(GUI.changed) {
			DirtyEditor();
		}
	}




	public static bool IsInstalled {
		get {
			return SA_VersionsManager.Is_MSP_Installed;
		}
	}


	
	public static bool IsUpToDate {
		get {

			if(CurrentVersion == SA_VersionsManager.MSP_Version) {
				return true;
			} else {
				return false;
			}
		}
	}

	public static int CurrentVersion {
		get {
			return SA_VersionsManager.ParceVersion(SocialPlatfromSettings.VERSION_NUMBER);
		}
	}
	
	public static int CurrentMagorVersion {
		get {
			return SA_VersionsManager.ParceMagorVersion(SocialPlatfromSettings.VERSION_NUMBER);
		}
	}
	
	

	private void GeneralOptions() {
		
		if(!IsInstalled) {
			EditorGUILayout.HelpBox("Install Required ", MessageType.Error);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			Color c = GUI.color;
			GUI.color = Color.cyan;
			if(GUILayout.Button("Install Plugin",  GUILayout.Width(120))) {
				PluginsInstalationUtil.Android_InstallPlugin();
				PluginsInstalationUtil.IOS_InstallPlugin();
				UpdateVersionInfo();
			}
			GUI.color = c;
			EditorGUILayout.EndHorizontal();
		}
		
		if(IsInstalled) {
			if(!IsUpToDate) {
				EditorGUILayout.HelpBox("Update Required \nResources version: " + SA_VersionsManager.MSP_StringVersionId + " Plugin version: " + SocialPlatfromSettings.VERSION_NUMBER, MessageType.Warning);

				
				
				
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				Color c = GUI.color;
				GUI.color = Color.cyan;


				if(CurrentMagorVersion != SA_VersionsManager.MSP_MagorVersion) {
					if(GUILayout.Button("How to update",  GUILayout.Width(250))) {
						Application.OpenURL("https://goo.gl/ZI66Ub");
					}
				} else {
					if(GUILayout.Button("Upgrade Resources",  GUILayout.Width(250))) {
						PluginsInstalationUtil.Android_InstallPlugin();
						PluginsInstalationUtil.IOS_InstallPlugin();
						UpdateVersionInfo();
					}
				}

				GUI.color = c;
				EditorGUILayout.Space();
				EditorGUILayout.EndHorizontal();
				
				EditorGUILayout.Space();
				Actions();
			} else {
				EditorGUILayout.HelpBox("Mobile Social Plugin v" + SocialPlatfromSettings.VERSION_NUMBER + " is installed", MessageType.Info);
				PluginSettings();
			}
		}

		EditorGUILayout.Space();
		
	}


	public static void DrawAPIsList() {
		EditorGUILayout.BeginHorizontal();
		GUI.enabled = false;
		EditorGUILayout.Toggle("Facebook",  PluginsInstalationUtil.IsFacebookInstalled);
		GUI.enabled = true;
		SocialPlatfromSettings.Instance.TwitterAPI = EditorGUILayout.Toggle("Twitter",  SocialPlatfromSettings.Instance.TwitterAPI);
		EditorGUILayout.EndHorizontal();
		
		
		EditorGUILayout.BeginHorizontal();
		SocialPlatfromSettings.Instance.NativeSharingAPI = EditorGUILayout.Toggle("Native Sharing",  SocialPlatfromSettings.Instance.NativeSharingAPI);
		SocialPlatfromSettings.Instance.InstagramAPI = EditorGUILayout.Toggle("Instagram",  SocialPlatfromSettings.Instance.InstagramAPI);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		SocialPlatfromSettings.Instance.EnableImageSharing = EditorGUILayout.Toggle("Image Sharing",  SocialPlatfromSettings.Instance.EnableImageSharing);
		EditorGUILayout.EndHorizontal();
	}

	public static void UpdateManifest() {
		
		if(!SocialPlatfromSettings.Instance.KeepManifestClean) {
			return;
		}
		
		AN_ManifestManager.Refresh();
		
		AN_ManifestTemplate Manifest =  AN_ManifestManager.GetManifest();
		AN_ApplicationTemplate application =  Manifest.ApplicationTemplate;
		AN_ActivityTemplate launcherActivity = application.GetLauncherActivity();

		AN_ActivityTemplate AndroidNativeProxy = application.GetOrCreateActivityWithName("com.androidnative.AndroidNativeProxy");
		AndroidNativeProxy.SetValue("android:launchMode", "singleTask");
		AndroidNativeProxy.SetValue("android:label", "@string/app_name");
		AndroidNativeProxy.SetValue("android:configChanges", "fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen");
		AndroidNativeProxy.SetValue("android:theme", "@android:style/Theme.Translucent.NoTitleBar");
			
		// Remove VIEW intent filter from AndroidNativeProxy activity
		if(AndroidNativeProxy != null) {
			AN_PropertyTemplate intent_filter = AndroidNativeProxy.GetOrCreateIntentFilterWithName("android.intent.action.VIEW");
			AndroidNativeProxy.RemoveProperty(intent_filter);
		}
			
		AN_ActivityTemplate SocialProxyActivity = application.GetOrCreateActivityWithName("com.androidnative.features.social.common.SocialProxyActivity");
		SocialProxyActivity.SetValue("android:launchMode", "singleTask");
		SocialProxyActivity.SetValue("android:label", "@string/app_name");
		SocialProxyActivity.SetValue("android:configChanges", "fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen");
		SocialProxyActivity.SetValue("android:theme", "@android:style/Theme.Translucent.NoTitleBar");
			
		if(launcherActivity.Name == "com.androidnative.AndroidNativeBridge") {
			launcherActivity.SetName("com.unity3d.player.UnityPlayerNativeActivity");
		}
			
			
		////////////////////////
		//TwitterAPI
		////////////////////////
			
			
		foreach(KeyValuePair<int, AN_ActivityTemplate> entry in application.Activities) {
			//TODO get intents array
			AN_ActivityTemplate act = entry.Value;
			AN_PropertyTemplate intent = act.GetIntentFilterWithName("android.intent.action.VIEW");
			if(intent != null) {
				AN_PropertyTemplate data = intent.GetOrCreatePropertyWithTag("data");
				if(data.GetValue("android:scheme") == "oauth") {
					act.RemoveProperty(intent);
				}
			}
		} 
			
		if(SocialPlatfromSettings.Instance.TwitterAPI) {
			if(SocialProxyActivity != null) {
				AN_PropertyTemplate intent_filter = SocialProxyActivity.GetOrCreateIntentFilterWithName("android.intent.action.VIEW");
				intent_filter.GetOrCreatePropertyWithName("category", "android.intent.category.DEFAULT");
				intent_filter.GetOrCreatePropertyWithName("category", "android.intent.category.BROWSABLE");
				AN_PropertyTemplate data = intent_filter.GetOrCreatePropertyWithTag("data");
				data.SetValue("android:scheme", "oauth");
				data.SetValue("android:host", PlayerSettings.applicationIdentifier);
			} 
		} else {
			if(SocialProxyActivity != null) {
				AN_PropertyTemplate intent_filter = SocialProxyActivity.GetOrCreateIntentFilterWithName("android.intent.action.VIEW");
				SocialProxyActivity.RemoveProperty(intent_filter);
			}
		}

		////////////////////////
		//FB API
		////////////////////////
		AN_PropertyTemplate ApplicationId_meta = application.GetOrCreatePropertyWithName("meta-data", "com.facebook.sdk.ApplicationId");
		AN_ActivityTemplate LoginActivity = application.GetOrCreateActivityWithName("com.facebook.LoginActivity");
		AN_ActivityTemplate FBUnityLoginActivity = application.GetOrCreateActivityWithName("com.facebook.unity.FBUnityLoginActivity");
		AN_ActivityTemplate FBUnityDeepLinkingActivity = application.GetOrCreateActivityWithName("com.facebook.unity.FBUnityDeepLinkingActivity");
		AN_ActivityTemplate FBUnityDialogsActivity = application.GetOrCreateActivityWithName("com.facebook.unity.FBUnityDialogsActivity");

		//This activity used for Facebook SDK v7.x
		AN_ActivityTemplate FacebookActivity = application.GetOrCreateActivityWithName("com.facebook.FacebookActivity");

		if(PluginsInstalationUtil.IsFacebookInstalled) {



			#if UNITY_ANDROID

			string FBAppId = "0";
			try {
				ScriptableObject FB_Resourse = 	Resources.Load("FacebookSettings") as ScriptableObject;

				if(FB_Resourse != null) {
					Type fb_settings = FB_Resourse.GetType();
					System.Reflection.PropertyInfo propert  = fb_settings.GetProperty("AppId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
					FBAppId = (string) propert.GetValue(null, null);
				}

			} catch(Exception ex) {
				Debug.LogError("AndroidNative. FBSettings.AppId reflection failed: " + ex.Message);
			}



			ApplicationId_meta.SetValue("android:value", "\\ " + FBAppId);

			LoginActivity.SetValue("android:label", "@string/app_name");
			LoginActivity.SetValue("android:theme", "@android:style/Theme.Translucent.NoTitleBar");
			LoginActivity.SetValue("android:configChanges", "keyboardHidden|orientation");


			FBUnityLoginActivity.SetValue("android:theme", "@android:style/Theme.Translucent.NoTitleBar.Fullscreen");
			FBUnityLoginActivity.SetValue("android:configChanges", "fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen");

			FBUnityDialogsActivity.SetValue("android:theme", "@android:style/Theme.Translucent.NoTitleBar.Fullscreen");
			FBUnityDialogsActivity.SetValue("android:configChanges", "fontScale|keyboard|keyboardHidden|locale|mnc|mcc|navigation|orientation|screenLayout|screenSize|smallestScreenSize|uiMode|touchscreen");

			FBUnityDeepLinkingActivity.SetValue("android:exported", "true");

			if (SA_ModulesInfo.FB_SDK_MajorVersionCode >= 7) {
				FacebookActivity.SetValue("android:configChanges", "keyboard|keyboardHidden|screenLayout|screenSize|orientation");
				FacebookActivity.SetValue("android:theme", "@android:style/Theme.Translucent.NoTitleBar");
				FacebookActivity.SetValue("android:label", "@string/app_name");
			} else {
				application.RemoveActivity(FacebookActivity);
			}

			#endif

			
		} else {
			application.RemoveProperty(ApplicationId_meta);
			application.RemoveActivity(LoginActivity);
			application.RemoveActivity(FBUnityLoginActivity);
			application.RemoveActivity(FBUnityDeepLinkingActivity);
			application.RemoveActivity(FBUnityDialogsActivity);
			application.RemoveActivity(FacebookActivity);
		}
		
		
		////////////////////////
		//NativeSharingAPI
		////////////////////////
		AN_PropertyTemplate provider = application.GetOrCreatePropertyWithName("provider", "android.support.v4.content.FileProvider");
		if(SocialPlatfromSettings.Instance.NativeSharingAPI) {

#if !(UNITY_4_0	|| UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6)
			//Remove FileProvider description from AndroidManifest.xml in Unity 5
			application.RemoveProperty (provider);
#else
			provider.SetValue("android:authorities", PlayerSettings.bundleIdentifier + ".fileprovider");
			provider.SetValue("android:exported", "false");
			provider.SetValue("android:grantUriPermissions", "true");
			AN_PropertyTemplate provider_meta = provider.GetOrCreatePropertyWithName("meta-data", "android.support.FILE_PROVIDER_PATHS");
			provider_meta.SetValue("android:resource", "@xml/file_paths");		
#endif
		} else {
			application.RemoveProperty(provider);
		}	

		
		List<string> permissions = GetRequiredPermissions();
		foreach(string p in permissions) {
			Manifest.AddPermission(p);
		}
		
		AN_ManifestManager.SaveManifest();
	}

	public static List<string> GetRequiredPermissions() {
		List<string> permissions =  new List<string>();
		permissions.Add("android.permission.INTERNET");

		if(SocialPlatfromSettings.Instance.NativeSharingAPI || SocialPlatfromSettings.Instance.InstagramAPI) {
			permissions.Add("android.permission.WRITE_EXTERNAL_STORAGE");
		}
		
		return permissions;
	}

	private void PluginSettings() {
		EditorGUILayout.Space();
			
			
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField("Keep Android Mnifest Clean");
			
		EditorGUI.BeginChangeCheck();
		SocialPlatfromSettings.Instance.KeepManifestClean = EditorGUILayout.Toggle(SocialPlatfromSettings.Instance.KeepManifestClean);
		if(EditorGUI.EndChangeCheck()) {
			UpdateManifest();
		}
			
		if(GUILayout.Button("[?]",  GUILayout.Width(25))) {
			Application.OpenURL("http://goo.gl/018lnQ");
		}
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		
		EditorGUILayout.EndHorizontal();
			
			
		SocialPlatfromSettings.Instance.ShowAPIS = EditorGUILayout.Foldout(SocialPlatfromSettings.Instance.ShowAPIS, "Mobile Social Plugin APIs");
		if(SocialPlatfromSettings.Instance.ShowAPIS) {
			EditorGUI.indentLevel++;
			
			EditorGUI.BeginChangeCheck();
			DrawAPIsList();
			if(EditorGUI.EndChangeCheck()) {
				UpdateManifest();
			}
				
				
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
					
		Actions();
	}

	private void Actions() {
		SocialPlatfromSettings.Instance.ShowActions = EditorGUILayout.Foldout(SocialPlatfromSettings.Instance.ShowActions, "More Actions");
		if(SocialPlatfromSettings.Instance.ShowActions) {
				
			if(!PluginsInstalationUtil.IsFacebookInstalled) {
				GUI.enabled = false;
			}	
				
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
				
			if(GUILayout.Button("Remove Facebook SDK",  GUILayout.Width(160))) {
				PluginsInstalationUtil.Remove_FB_SDK_WithDialog();
			}

			if(GUILayout.Button("Reset Settings",  GUILayout.Width(160))) {
				ResetSettings();
			}
				
			GUI.enabled = true;


			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
				
				
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			
				
			if(GUILayout.Button("Load Example Settings",  GUILayout.Width(160))) {
				LoadExampleSettings();
			}
				

			if(GUILayout.Button("Reinstall",  GUILayout.Width(160))) {
				PluginsInstalationUtil.Android_UpdatePlugin();
				PluginsInstalationUtil.IOS_UpdatePlugin();
				UpdateVersionInfo();
				
			}

				
			EditorGUILayout.EndHorizontal();	
			EditorGUILayout.Space();


			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space();

			if(GUILayout.Button("Remove",  GUILayout.Width(160))) {
				SA_RemoveTool.RemovePlugins();
			}
				
			EditorGUILayout.EndHorizontal();	
		}
	}

	public static void LoadExampleSettings() {

		SocialPlatfromSettings.Instance.TWITTER_CONSUMER_KEY = "wEvDyAUr2QabVAsWPDiGwg";
		SocialPlatfromSettings.Instance.TWITTER_CONSUMER_SECRET = "igRxZbOrkLQPNLSvibNC3mdNJ5tOlVOPH3HNNKDY0";

	}

	public static void ResetSettings() {
		FileStaticAPI.DeleteFile("Extensions/GooglePlayCommon/Resources/SocialSettings.asset");
		SocialPlatfromSettings.Instance.ShowActions = true;
		Selection.activeObject = SocialPlatfromSettings.Instance;
	}
	
	public static void GeneralSettings(){
		EditorGUILayout.HelpBox("General Settings", MessageType.None);
		
		SocialPlatfromSettings.Instance.ShowImageSharingSettings = EditorGUILayout.Foldout(SocialPlatfromSettings.Instance.ShowImageSharingSettings, "Image Sharing");
		if (SocialPlatfromSettings.Instance.ShowImageSharingSettings) {
			SocialPlatfromSettings.Instance.SaveImageToGallery = EditorGUILayout.Toggle("Save Image to Gallery", SocialPlatfromSettings.Instance.SaveImageToGallery);
		}
	}
	




	private void AboutGUI() {


		EditorGUILayout.HelpBox("About Mobile Social Plugin", MessageType.None);
		EditorGUILayout.Space();
		
		SA_EditorTool.SelectableLabelField(SdkVersion, SocialPlatfromSettings.VERSION_NUMBER);
		SA_EditorTool.FBSdkVersionLabel();
		SelectableLabelField(SupportEmail, "stans.assets@gmail.com");

		
	}
	
	private static void SelectableLabelField(GUIContent label, string value) {
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(label, GUILayout.Width(180), GUILayout.Height(16));
		EditorGUILayout.SelectableLabel(value, GUILayout.Height(16));
		EditorGUILayout.EndHorizontal();
	}

	public static void UpdateVersionInfo() {
		FileStaticAPI.Write(SA_VersionsManager.MSP_VERSION_INFO_PATH, SocialPlatfromSettings.VERSION_NUMBER);
		UpdateManifest();
	}



	public static void DirtyEditor() {
		#if UNITY_EDITOR
		EditorUtility.SetDirty(SocialPlatfromSettings.Instance);
		#endif
	}
	
	
}
