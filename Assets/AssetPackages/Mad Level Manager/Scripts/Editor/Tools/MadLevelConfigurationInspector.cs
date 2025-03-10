/*
* Mad Level Manager by Mad Pixel Machine
* http://www.madpixelmachine.com
*/

using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using MadLevelManager;
using System.Xml;
using System;
using System.Linq;
using System.Text.RegularExpressions;

#if !UNITY_3_5
namespace MadLevelManager {
#endif

[CustomEditor(typeof(MadLevelConfiguration))]
public class MadLevelConfigurationInspector : Editor {

    // ===========================================================
    // Constants
    // ===========================================================

    // ===========================================================
    // Fields
    // ===========================================================
    
    MadLevelConfiguration configuration;
    
    MadGUI.ScrollableList<LevelItem> list;
    List<LevelItem> items;

    private List<MadGUI.RunnableVoid0> executionQueue = new List<MadGUI.RunnableVoid0>();

    private List<IMadLevelConfigurationInspectorAddon> inspectorAddons = new List<IMadLevelConfigurationInspectorAddon>();

    //int extensionIndex = 0;
    
    MadLevelConfiguration.Group currentGroup {
        get {
            return IndexToGroup(currentGroupIndex);
        }
        
        set {
            currentGroupIndex = GroupToIndex(value);
        }
    }
    
    string GUID {
        get {
            var path = AssetDatabase.GetAssetPath(configuration);
            return AssetDatabase.AssetPathToGUID(path);
        }
    }
    
    int currentGroupIndex {
        get {
            if (configuration.groups.Count == 0) {
                return 0;
            } else {
                // not Count - 1 because the default group is not on the list
                return Mathf.Clamp(_currentGroupIndex, 0, configuration.groups.Count);
            }
        }
        
        set {
            bool reset = false;
            if (_currentGroupIndex != value) {
                reset = true;
            }
            _currentGroupIndex = value;
            
            if (reset) {
                items.Clear();
                list.selectedItem = null;
            }
        }
    }
    int _currentGroupIndex {
        get {
            return EditorPrefs.GetInt(GUID + "_currentGroupIndex", 0);
        }
        
        set {
            EditorPrefs.SetInt(GUID + "_currentGroupIndex", value);
        }
    }

    private bool multiSelection {
        get { return list.selectedItems.Count > 1; }
    }
    
    static bool texturesLoaded;
    static Texture textureOther;
    static Texture textureLevel;
    static Texture textureExtra;
    static Texture textureError;
    static Texture textureStar;
    static Texture textureLock;
    static Texture textureLockUnlocked;

    // ===========================================================
    // Methods for/from SuperClass/Interfaces
    // ===========================================================

    // ===========================================================
    // Methods
    // ===========================================================
    
    void OnEnable() {
        configuration = target as MadLevelConfiguration;
    
        items = new List<LevelItem>();
    
        list = new MadGUI.ScrollableList<LevelItem>(items);
        list.height = 0; // expand
        list.spaceAfter = 150;
        list.label = "Level List";
        list.selectionEnabled = true;
        
        list.selectionCallback += (levelItems) => ItemSelected(levelItems);
        list.doubleClickCallback += (item) => OpenScene(item);

        list.acceptDropTypes.Add(typeof(UnityEngine.Object));
        list.dropCallback += (index, obj) => {
            if (CheckAssetIsScene(obj)) {
                executionQueue.Add(() => {
                    var item = AddLevel();
                    item.level.sceneObject = obj;
                    item.level.type = MadLevel.Type.Other;

                    item.level.name = "";
                    item.level.name = UniqueLevelName(obj.name);

                    configuration.SetDirty();
                });
            }
        };

        InitInspectorAddons();

        // Hi! Please do not remove this method call. See below.
        PCheck();
    }

    // This is a message for those who've got Mad Level Manager from different sources than Unity Asset Store.
    //
    // Hi! Please do not remove this method. I made it that way so the message will not be
    // very intrusive, so you will still be able to use the MLM.
    // Unity scripts is what I am doing for living. If you will find it useful, please consider
    // purchasing it from the Unity Asset Store. You will get the support, free updates, and
    // you can ask me for new features! :-)
    //
    // If you're not interseted, that's OK!
    //
    // Thanks!
    // Piotr
    private void PCheck() {
        if (configuration.flag == 0) {
            return;
        }

        var range = UnityEngine.Random.Range(0, 10);
        if (range == 0) {
            var start = new DateTime(2000, 1, 1);
            var now = DateTime.Now;
            int days = (int) (now - start).TotalDays;

            int lastPopupDay = EditorPrefs.GetInt("LastPopupDay", days);

            // display popup ony if there's a 3 day difference
            if (days - lastPopupDay >= 3) {
                bool purchase = EditorUtility.DisplayDialog(
                    "Mad Level Manager", "Thank you for using Mad Level Manager! " +
                                         "Please consider purchasing the full version from the Unity Asset Store :-)",
                    "Well... OK!", "Nah...");
                if (purchase) {
                    Application.OpenURL("http://u3d.as/52x");
                }
                EditorPrefs.SetInt("LastPopupDay", days);
            }
        }
    }

    private void InitInspectorAddons() {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();
        foreach (var type in types) {
            if (typeof (IMadLevelConfigurationInspectorAddon).IsAssignableFrom(type) && !type.IsAbstract) {
                var instance = (IMadLevelConfigurationInspectorAddon) Activator.CreateInstance(type);
                inspectorAddons.Add(instance);
            }
        }

        inspectorAddons.Sort((a, b) => a.GetOrder().CompareTo(b.GetOrder()));
    }

    private void OpenScene(LevelItem item) {
        if (item.level.sceneObject != null) {
            EditorApplication.delayCall += () => {
                if (EditorApplication.SaveCurrentSceneIfUserWantsTo()) {
                    EditorApplication.OpenScene(item.level.scenePath);
                }
            };
        }
    }

    void LoadTextures() {
        if (texturesLoaded) {
            return;
        }
        
        textureOther = Resources.Load("MadLevelManager/Textures/icon_other") as Texture;
        textureLevel = Resources.Load("MadLevelManager/Textures/icon_level") as Texture;
        textureExtra = Resources.Load("MadLevelManager/Textures/icon_extra") as Texture;
        textureError = Resources.Load("MadLevelManager/Textures/icon_error") as Texture;
        textureStar = Resources.Load("MadLevelManager/Textures/icon_star") as Texture;
        textureLock = Resources.Load("MadLevelManager/Textures/icon_lock") as Texture;
        textureLockUnlocked = Resources.Load("MadLevelManager/Textures/icon_lock_unlocked") as Texture;
        
        texturesLoaded = true;
    }
    
    void ItemSelected(List<LevelItem> items) {
        Repaint();
        var focusedControl = GUI.GetNameOfFocusedControl();
        if (!string.IsNullOrEmpty(focusedControl)) {
            GUI.SetNextControlName("");
            GUI.FocusControl("");
        }
    }
    
    public override void OnInspectorGUI() {
        if (MadTrialEditor.isTrialVersion && MadTrialEditor.expired) {
            MadTrialEditor.OnEditorGUIExpired("Mad Level Manager");
            return;
        }

        LoadTextures(); // loading textures with delay to prevent editor errors
        CheckAssetLocation();
        ActiveInfo();
        
        GUIGroupPopup();

        LoadItems();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(1));
        GUILayout.Space(200);
        EditorGUILayout.EndVertical();
        list.Draw();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add")) {
            AddLevel();
        }
        GUI.backgroundColor = Color.white;
        
        GUI.enabled = list.selectedItem != null;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove") || GUI.enabled && Event.current.keyCode == KeyCode.Delete) {
            RemoveLevel();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.FlexibleSpace();

        GUILayout.Label("Move");
        
        if (GUILayout.Button("Down")) {
            MoveDown();
            configuration.SetDirty();
        }
        
        if (GUILayout.Button("Up")) {
            MoveUp();
            configuration.SetDirty();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Bottom")) {
            MoveToBottom();
            configuration.SetDirty();
        }

        if (GUILayout.Button("Top")) {
            MoveToTop();
            configuration.SetDirty();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        MadGUI.IndentBox("Level Properties", () => {
            var item = list.selectedItem;
            var items = list.selectedItems;

            if (item == null) {
                item = new LevelItem(configuration);
                GUI.enabled = false;
            }
            
            MadUndo.RecordObject(configuration, "Edit '" + item.level.name + "'");
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            UnityEngine.Object sceneValue = null;
            if (!multiSelection) {
                MadGUI.Validate(() => item.level.sceneObject != null, () => {
                    sceneValue =
                        EditorGUILayout.ObjectField("Scene", item.level.sceneObject, typeof (UnityEngine.Object), false);
                });
            } else {
                bool unified = (from i in items select i.level.sceneObject).Distinct().Count() == 1;
                if (unified) {
                    sceneValue =
                        EditorGUILayout.ObjectField("Scene", item.level.sceneObject, typeof (UnityEngine.Object), false);
                } else {
                    sceneValue = EditorGUILayout.ObjectField("Scene", null, typeof(UnityEngine.Object), false);
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                MadUndo.RecordObject2(target, "Changed Level Scene");
                foreach (var levelItem in items) {
                    levelItem.level.sceneObject = sceneValue;
                }
            }


            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Set Current", GUILayout.Width(85))) {
                MadUndo.RecordObject2(target, "Change Scene");
                var obj = AssetDatabase.LoadAssetAtPath(EditorApplication.currentScene, typeof(UnityEngine.Object));
                if (obj != null) {
                    foreach (var levelItem in items) {
                        levelItem.level.sceneObject = obj;
                    }
                } else {
                    EditorUtility.DisplayDialog("Scene not saved", "Current scene is not saved. Please save it first (CTRL+S).", "OK");
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            if (!CheckAssetIsScene(item.level.sceneObject)) {
                item.level.sceneObject = null;
            }

            MadGUI.Validate(() => !string.IsNullOrEmpty(item.level.name), () => {
                GUI.SetNextControlName("level name"); // needs names to unfocus
                using (MadGUI.EnabledIf(!multiSelection)) {
                    if (!multiSelection) {
                        EditorGUI.BeginChangeCheck();
                        var value = EditorGUILayout.TextField("Level Name", item.level.name);
                        if (EditorGUI.EndChangeCheck()) {
                            MadUndo.RecordObject2(target, "Changed Level Name");
                            item.level.name = value;
                        }
                    } else {
                        EditorGUILayout.TextField("Level Name", "-");
                    }
                }
            });


            // level type
            MadLevel.Type typeValue = default(MadLevel.Type);
            EditorGUI.BeginChangeCheck();
            if (!multiSelection) {
                typeValue = (MadLevel.Type) EditorGUILayout.EnumPopup("Type", item.level.type);
            } else {
                bool unified = (from i in items select i.level.type).Distinct().Count() == 1;
                if (unified) {
                    typeValue = (MadLevel.Type) EditorGUILayout.EnumPopup("Type", item.level.type);
                } else {
                    int val = EditorGUILayout.Popup("Type", -1, Enum.GetNames(typeof(MadLevel.Type)));
                    if (val != -1) {
                        typeValue = (MadLevel.Type) val;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                MadUndo.RecordObject2(target, "Changed Level Type");
                foreach (var levelItem in items) {
                    levelItem.level.type = typeValue;
                }
            }


            GUI.SetNextControlName("arguments"); // needs names to unfocus
            if (!multiSelection) {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUILayout.TextField("Arguments", item.level.arguments);
                if (EditorGUI.EndChangeCheck()) {
                    MadUndo.RecordObject2(target, "Changed Level Arguments");
                    item.level.arguments = value;
                }
            } else {
                bool unified = (from i in items select i.level.arguments).Distinct().Count() == 1;
                EditorGUI.BeginChangeCheck();
                string value = "";
                if (unified) {
                    value = EditorGUILayout.TextField("Arguments", item.level.arguments);
                } else {
                    value = EditorGUILayout.TextField("Arguments", "-");
                }
                if (EditorGUI.EndChangeCheck()) {
                    MadUndo.RecordObject2(target, "Changed Level Arguments");
                    foreach (var levelItem in items) {
                        levelItem.level.arguments = value;
                    }
                }
            }

            if (MadGUI.Foldout("Locking", false)) {
                using (MadGUI.Indent()) {
                    bool lockedByDefultState = false;
                    if (multiSelection) {
                        bool unified = (from i in items select i.level.lockedByDefault).Distinct().Count() == 1;
                        if (unified && (item.level.lockedByDefault)) {
                            lockedByDefultState = true;
                        }
                    } else {
                        lockedByDefultState = item.level.lockedByDefault;
                    }

                    EditorGUI.BeginChangeCheck();
                    GUI.SetNextControlName("locked by default"); // needs names to unfocus
                    bool lockedByDefaultValue = EditorGUILayout.Toggle("Locked By Default", lockedByDefultState);
                    if (EditorGUI.EndChangeCheck()) {
                        MadUndo.RecordObject2(target, "Changed Locked By Default");
                        foreach (var levelItem in items) {
                            levelItem.level.lockedByDefault = lockedByDefaultValue;
                        }
                    }
                }
                EditorGUILayout.Space();
            }

            if (MadGUI.Foldout("Extensions", false)) {
                using (MadGUI.Indent()) {

                    EditorGUI.BeginChangeCheck();
                    int extensionIndex = -1;

                    if (!multiSelection) {
                        extensionIndex = configuration.extensions.FindIndex((e) => e == item.level.extension) + 1;
                    } else {
                        bool unified = (from i in items select i.level.extension).Distinct().Count() == 1;
                        if (unified) {
                            extensionIndex = configuration.extensions.FindIndex((e) => e == item.level.extension) + 1;
                        }
                    }
                    
                    extensionIndex = MadGUI.DynamicPopup(extensionIndex, "Extension", configuration.extensions.Count + 1,
                        (index) => {
                            if (index == 0) {
                                return "(none)";
                            } else {
                                return configuration.extensions[index - 1].name;
                            }
                        });
                    if (EditorGUI.EndChangeCheck()) {
                        MadUndo.RecordObject2(target, "Changed Extension For Level");

                        foreach (var levelItem in items) {
                            if (extensionIndex == 0) {
                                levelItem.level.extension = null;
                            } else {
                                levelItem.level.extension = configuration.extensions[extensionIndex - 1];
                            }
                        }

                        configuration.SetDirty();
                    }

                    bool enabledState = GUI.enabled;
                    GUI.enabled = true;
                    if (MadGUI.Button("Open Extension Editor", Color.magenta)) {
                        MadLevelExtensionEditor.Show(configuration);
                    }
                    GUI.enabled = enabledState;

                    EditorGUILayout.Space();
                }
            }

            EditorGUI.BeginChangeCheck();
            int groupIndex = GroupToIndex(item.level.group);
            groupIndex = EditorGUILayout.Popup("Move To Group:", groupIndex, GroupNames());
            if (EditorGUI.EndChangeCheck()) {
                var @group = IndexToGroup(groupIndex);
                MadUndo.RecordObject2(target, "Move Levels To Group " + @group.name);
                foreach (var levelItem in items) {
                    levelItem.level.group = @group;
                }
            }


            if (EditorGUI.EndChangeCheck()) {
                configuration.SetDirty();
            }

            if (inspectorAddons.Count > 0) {
                EditorGUILayout.Space();
            }

            foreach (var addon in inspectorAddons) {
                addon.OnInspectorGUI(item.level);
            }
            
            GUI.enabled = true;
            
        });
        
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Help")) {
            Help.BrowseURL(MadLevelHelp.LevelConfigurationHelp);
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        if (!configuration.IsValid()) {
            MadGUI.Error("Configuration is invalid. Please make sure that:\n"
                + "- There's no levels with \"!!!\" icon. These levels may have duplicated name or missing scene.\n"
                + "- All your extensions have no missing scenes (in Extension Editor)"
            );
        }

        if (configuration.active && !MadLevelConfigurationEditor.CheckBuildSynchronized(configuration)) {
            if (MadGUI.ErrorFix(
                "Build configuration is not in synch with level configuration.",
                "Synchronize Now")) {
                    MadLevelConfigurationEditor.SynchronizeBuild(configuration);
            }
        }

        ExecuteDelayed();
    }

    private void ExecuteDelayed() {
        foreach (var ex in executionQueue) {
            ex();
        }

        executionQueue.Clear();
    }

    void GUIGroupPopup() {
        MadGUI.Box("Groups", () => {

            EditorGUILayout.BeginHorizontal();
            MadGUI.LookLikeControls(75);
            currentGroupIndex = EditorGUILayout.Popup("Group", currentGroupIndex, GroupNames());
            MadGUI.LookLikeControls(0);

            if (MadGUI.Button("Edit Groups", Color.green, GUILayout.Width(100))) {
                MadLevelGroupsEditTool.Display(configuration);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        });
    }

    private void RenameGroup(MadLevelConfiguration.Group currentGroup) {
        var builder = new MadInputDialog.Builder("Enter Group Name", "Enter a new name for group \"" + currentGroup.name + "\".", (name) => {
            if (!string.IsNullOrEmpty(name)) {
                currentGroup.name = name;
                EditorUtility.SetDirty(configuration);
            }
        });
        builder.defaultValue = currentGroup.name;
        builder.allowEmpty = false;
        builder.BuildAndShow();
    }
    
    string[] GroupNames() {
        var groups = configuration.groups;
        var groupNames = new List<string>();
        groupNames.Add(configuration.defaultGroup.name);
        
        foreach (var g in groups) {
            groupNames.Add(g.name);
        }
        
        return groupNames.ToArray();
    }
    
    MadLevelConfiguration.Group IndexToGroup(int index) {
        if (index == 0) {
            return configuration.defaultGroup;
        } else {
            return configuration.groups[index - 1];
        }
    }
    
    int GroupToIndex(MadLevelConfiguration.Group group) {
        if (group == configuration.defaultGroup) {
            return 0;
        } else {
            if (configuration.groups.Contains(group)) {
                return configuration.groups.IndexOf(group) + 1;
            } else {
                Debug.LogError("Group not found: " + group);
                return 0;
            }
        }
    }
    
    bool CheckAssetIsScene(UnityEngine.Object obj) {
        if (obj == null) {
            return false;
        }
    
        string path = AssetDatabase.GetAssetPath(obj);
        if (!path.EndsWith(".unity")) {
            EditorUtility.DisplayDialog(
                "Only scene allowed",
                "You can only drop scene files into this field.",
                "OK");
                
            return false;
        }
        
        return true;
    }
    
    LevelItem AddLevel() {
        MadUndo.RecordObject2(configuration, "Add Level");
        
        var levelItem = new LevelItem(configuration);
        levelItem.level.group = currentGroup;
        
        LevelItem template = null;
        
        if (list.selectedItem != null) {
            template = list.selectedItem;
        } else if (items.Count > 0) {
            template = items.Last();
        }
        
        if (template != null) {
            levelItem.level.order = template.level.order + 1;
            
            levelItem.level.name = UniqueLevelName(template.level.name);
            levelItem.level.sceneObject = template.level.sceneObject;
            levelItem.level.type = template.level.type;

            levelItem.level.extension = template.level.extension;
        } else {
            levelItem.level.order = 0;
            
            levelItem.level.name = "New Level";
            levelItem.level.type = MadLevel.Type.Level;
        }

        // check if there is a level that is not locked by default
        // if there isn't one, then set this one to be
        bool hasLockedByDefault = items.Find((item) => item.level.type == MadLevel.Type.Level && !item.level.lockedByDefault) != null;
        if (!hasLockedByDefault) {
            levelItem.level.lockedByDefault = false;
        }
        
        items.Add(levelItem);
        configuration.levels.Add(levelItem.level);
        
        Reorder();
        
        configuration.SetDirty();
        
        list.selectedItem = levelItem;
        list.ScrollToItem(levelItem);

        return levelItem;
    }
    
    string UniqueLevelName(string name) {
        int num = 1;

        while (items.Find((i) => i.level.name == name) != null) {
            var match = Regex.Match(name, @".* \(([0-9]+)\)$");
            if (match.Success) {
                var numStr = match.Groups[1].Value;
                num = int.Parse(numStr) + 1;
                name = name.Substring(0, name.Length - (3 + numStr.Length));
            }

            name = name + " (" + num + ")";

            num++;
        }

        return name;
    }
    
    void RemoveLevel() {
        MadUndo.RecordObject2(configuration, "Remove Level");

        foreach (var item in list.selectedItems) {
            configuration.levels.Remove(item.level);
            items.Remove(item);
        }

        Reorder();
        configuration.SetDirty();
        
        list.selectedItem = null;
    }
    
    void ActiveInfo() {
        bool assetLocationRight = IsAssetLocationRight();
        GUI.enabled = assetLocationRight;
    
        var active = MadLevelConfiguration.FindActive();
        if (active == configuration) {
            MadGUI.Info("This is the active configuration.");
        } else {
            string additional = "";
            if (!assetLocationRight) {
                additional = " (But first you must relocate this asset. Please look at the other error.)";
            }
        
            int choice = MadGUI.MessageWithButtonMulti("This configuration is not active. "
                                                  + "It's not currently used to manage your scenes." + additional, MessageType.Warning, "Where is active?", "Activate");
            
            if (choice == 0) {
                var currentlyActive = MadLevelConfiguration.FindActive();
                if (currentlyActive != null) {
                    EditorGUIUtility.PingObject(currentlyActive);
                } else {
                    EditorUtility.DisplayDialog("Not Found", "No level configuration is active at the moment", "OK");
                }
            } else if (choice == 1) {
                configuration.active = true;
            }
        }
        GUI.enabled = true;
    }
    
    void CheckAssetLocation() {
        if (!IsAssetLocationRight()) {
            if (MadGUI.ErrorFix(
                "Configuration should be placed in Resources/LevelConfig directory", "Where it is now?")) {
                EditorGUIUtility.PingObject(configuration);   
            }
        }
    }
    
    bool IsAssetLocationRight() {
        var configurationPath = AssetDatabase.GetAssetPath(configuration);
        return configurationPath.EndsWith(string.Format("Resources/LevelConfig/{0}.asset", configuration.name));
    }
    
    void LoadItems() {
        var configurationLevels = currentGroup.GetLevels();
        if (items.Count != configurationLevels.Count) {
        
            items.Clear();
            foreach (var level in configurationLevels) {
                var item = new LevelItem(level);
                items.Add(item);
            }
            
        }
        
        Reorder();
    }
    
    void Reorder() {
        items.Sort((a, b) => {
            return a.level.order.CompareTo(b.level.order);
        });
    }
    
    void MoveUp() {
        MadUndo.RecordObject2(configuration, "Move '" + list.selectedItem.level.name + "' Up");
        MoveWith(-1);
    }
    
    void MoveDown() {
        MadUndo.RecordObject2(configuration, "Move '" + list.selectedItem.level.name + "' Down");
        MoveWith(1);
    }

    void MoveToBottom() {
        MadUndo.RecordObject2(configuration, "Move '" + list.selectedItem.level.name + "' To Bottom");
        list.selectedItem.level.order = int.MaxValue;
        Reorder();
    }

    void MoveToTop() {
        MadUndo.RecordObject2(configuration, "Move '" + list.selectedItem.level.name + "' To Top");
        list.selectedItem.level.order = int.MinValue;
        Reorder();
    }
    
    void MoveWith(int delta) {
        int index = items.IndexOf(list.selectedItem);
        int otherIndex = index + delta;
        
        if (otherIndex >= 0 && otherIndex < items.Count) {
            int order = list.selectedItem.level.order;
            list.selectedItem.level.order = items[otherIndex].level.order;
            items[otherIndex].level.order = order;
            
            Reorder();
        }
    }

    // ===========================================================
    // Static Methods
    // ===========================================================
    
    [MenuItem("Assets/Create/Level Configuration")]
    public static void CreateAsset() {
        string currentPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(currentPath) && currentPath.EndsWith("Resources/LevelConfig")) {
            MadAssets.CreateAsset<MadLevelConfiguration>("New Configuration");
        } else {
            MadAssets.CreateAsset<MadLevelConfiguration>("New Configuration", "Assets/Resources/LevelConfig");
        }
        
    }
    
    // ===========================================================
    // Inner and Anonymous Classes
    // ===========================================================
    
    private class LevelItem : MadGUI.ScrollableListItem {
        public MadLevelConfiguration.Level level;
    
        public LevelItem(MadLevelConfiguration configuration) {
            this.level = configuration.CreateLevel();
        }
    
        public LevelItem(MadLevelConfiguration.Level level) {
            this.level = level;
        }
        
        public override void OnGUI() {
            var rect = EditorGUILayout.BeginHorizontal();
            
            GUILayout.Space(45);
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(level.name);

            Color origColor = GUI.color;
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, 0.5f);
            EditorGUILayout.LabelField(level.sceneName);
            GUI.color = origColor;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            
            Texture texture = textureOther;

            switch (level.type) {
                case MadLevel.Type.Other:
                    texture = textureOther;
                    break;
                case MadLevel.Type.Level:
                    texture = textureLevel;
                    break;
                case MadLevel.Type.Extra:
                    texture = textureExtra;
                    break;
                    
                default:
                    Debug.LogError("Unknown level type: " + level.type);
                    break;
            }
            
            if (!level.IsValid()) {
                texture = textureError;
            }
            
            GUI.DrawTexture(new Rect(rect.x, rect.y, 28, 34), texture);
            if (level.hasExtension) {
                GUI.DrawTexture(new Rect(rect.x, rect.y, 28, 34), textureStar);
            }

            // draw lock
            if (level.type == MadLevel.Type.Level) {
                Texture lockTexture = level.lockedByDefault ? textureLock : textureLockUnlocked;
                GUI.DrawTexture(new Rect(rect.x + 22, rect.y, 28, 34), lockTexture);
            }
            

            EditorGUILayout.EndHorizontal();
            
        }
    }

}

#if !UNITY_3_5
} // namespace
#endif
