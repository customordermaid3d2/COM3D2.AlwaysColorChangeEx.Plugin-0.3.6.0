﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityInjector;
using UnityInjector.Attributes;
using CM3D2.AlwaysColorChangeEx.Plugin.Data;
using CM3D2.AlwaysColorChangeEx.Plugin.TexAnim;
using CM3D2.AlwaysColorChangeEx.Plugin.UI;
using CM3D2.AlwaysColorChangeEx.Plugin.UI.Helper;
using CM3D2.AlwaysColorChangeEx.Plugin.Util;

//[assembly: AssemblyVersion("0.3.6.0")]
namespace CM3D2.AlwaysColorChangeEx.Plugin {
#if COM3D2
    [PluginFilter("COM3D2x64"),
#else
    [PluginFilter("CM3D2x64"),
     PluginFilter("CM3D2VRx64"),
     PluginFilter("CM3D2OHx64"),
     PluginFilter("CM3D2OHVRx64"),
#endif
     PluginName("COM3D25_ACCex"),
     PluginVersion("0.3.6.0")]
    class AlwaysColorChangeEx : PluginBase {

        public static volatile string PluginName;
        public static volatile string Version;
        static AlwaysColorChangeEx() {
            // 属性クラスからプラグイン名/バージョン番号を取得
            try {
                var attr = Attribute.GetCustomAttribute( typeof(AlwaysColorChangeEx), typeof( PluginNameAttribute ) ) as PluginNameAttribute;
                if( attr != null ) PluginName = attr.Name;
            } catch( Exception e ) {
                LogUtil.Error( e );
            }
            try {
                var attr = Attribute.GetCustomAttribute( typeof(AlwaysColorChangeEx), typeof( PluginVersionAttribute ) ) as PluginVersionAttribute;
                if( attr != null ) Version = attr.Version;
            } catch( Exception e ) {
                LogUtil.Error( e );
            }
        }
        internal MonoBehaviour plugin;

        private readonly CM3D2SceneChecker checker = new CM3D2SceneChecker();

        private enum MenuType {
            None,
            Main,
            Color,
            NodeSelect,
            MaskSelect,
            Save,
            PresetSelect,
            Texture,
            MaidSelect,
            BoneSlotSelect,
            PartsColor,
        }
        private const string TITLE_LABEL = "ACCex : ";
        private const int WIN_ID_MAIN   = 20201;
        private const int WIN_ID_DIALOG = WIN_ID_MAIN+1;
        private const int WIN_ID_TIPS   = WIN_ID_MAIN+2;

        private const EventModifiers modifierKey = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt;

        #region Variables
        private float fPassedTime;
        private float fLastInitTime;
        private bool initialized;

        private bool bUseStockMaid;
        private bool mouseDowned;

        private readonly Settings settings = Settings.Instance;
        private readonly UIParams uiParams = UIParams.Instance;
        private readonly MaidHolder holder = MaidHolder.Instance;
        private readonly PresetManager presetMgr = new PresetManager();

        private MenuType menuType;

        // プリセット名
        private readonly List<string> presetNames = new List<string>();
        // 操作情報
        private readonly Dictionary<string, bool> dDelNodes     = new Dictionary<string, bool>();
        // 表示中状態
        private Dictionary<string, bool> dDelNodeDisps = new Dictionary<string, bool>();
        private readonly Dictionary<TBody.SlotID, MaskInfo> dMaskSlots = new Dictionary<TBody.SlotID, MaskInfo>();
        PresetData currentPreset;
        private string presetName = string.Empty;
        private bool bPresetCastoff = true;
        private bool bPresetApplyNode;
        private bool bPresetApplyMask;
        private bool bPresetApplyBody = true;
        private bool bPresetApplyWear = true;
        private bool bPresetApplyBodyProp   = true;
        private bool bPresetApplyPartsColor = true;
        private bool bPresetSavable;
        private Maid toApplyPresetMaid;

        private bool isSavable;
        private bool isActive;
        private bool texSliderUpped;

        private const int applyDelayFrame = 10;
        private const int tipsSecond = 2;
        private readonly IntervalCounter changeCounter = new IntervalCounter(15);
        // ゲーム上の表示データの再ロード間隔
        private readonly IntervalCounter refreshCounter = new IntervalCounter(60);
        private readonly MaidChangeDetector changeDetector = new MaidChangeDetector();
        private readonly AnimTargetDetector animDetector = new AnimTargetDetector();
        private Vector2 scrollViewPosition = Vector2.zero;
        // 表示名切り替え
        private bool nameSwitched;

        // thumcache等
        private readonly Dictionary<int, GUIContent> _contentDic = new Dictionary<int, GUIContent>();

        private readonly List<SelectMaidData> _maidList = new List<SelectMaidData>();
        private readonly List<SelectMaidData> _manList = new List<SelectMaidData>();

        // テクスチャ変更用
        //  現在のターゲットのslotに関するメニューが変更されたらGUIを更新。それ以外は更新しない
        private int targetMenuId;
        private bool slotDropped;
        private Material[] targetMaterials;
        private readonly Material[] EMPTY_ARRAY = new Material[0];
        private List<ACCMaterialsView> materialViews;
        private List<ACCTexturesView> texViews;
        private ACCSaveMenuView saveView;
        private ACCBoneSlotView boneSlotView;
        private ACCPartsColorView partsColorView;
        private readonly List<BaseView> views = new List<BaseView>();

        // 選択画面の一時選択状態のメイド情報
        private Maid selectedMaid;
        private string selectedName;

        private GUIContent title;
        // TIPS
        private const int TIPS_MARGIN = 24;
        private bool displayTips;
        private Rect tipRect;
        private string tips;

        private readonly UIHelper uiHelper = new UIHelper();
        private ColorPresetManager colorPresetMgr;
        private SliderHelper sliderHelper;
        private CheckboxHelper cbHelper;
        #endregion

        public AlwaysColorChangeEx() {
            mouseDowned = false;
            plugin = this;
            
        }

        #region MonoBehaviour methods
        public void Awake() {
            DontDestroyOnLoad(this);
        
            // リダイレクトで存在しないパスが渡されてしまうケースがあるため、
            // Sybarisチェックを先に行う (リダイレクトによるパスではディレクトリ作成・削除が動作しない）
            var dllPath = Path.Combine(DataPath, @"..\..\opengl32.dll");
            var dirPath = Path.Combine(DataPath, @"..\..\Sybaris");
            if (File.Exists(dllPath) && Directory.Exists(dirPath)) {
                dirPath = Path.GetFullPath(dirPath);
                settings.presetDirPath = Path.Combine(dirPath, @"Plugins\UnityInjector\Config\ACCPresets");
            } else {
                settings.presetDirPath = Path.Combine(DataPath, "ACCPresets");
            }

            ReloadConfig();
            settings.Load(key => Preferences["Config"][key].Value);
            LogUtil.Log("PresetDir:", settings.presetDirPath);

            checker.Init();

            LoadPresetList();
            uiParams.Update();

            // Initialize
            ShaderPropType.Initialize();
            
            sliderHelper = new SliderHelper(uiParams);
            cbHelper = new CheckboxHelper(uiParams);
            colorPresetMgr = ColorPresetManager.Instance;
            var colorPresetFile = Path.Combine(settings.presetDirPath, "_ColorPreset.csv");
            colorPresetMgr.Count = 40;
            colorPresetMgr.SetPath(colorPresetFile);
            
#if UNITY_5_5_OR_NEWER
            SceneManager.sceneLoaded += SceneLoaded;
#endif
            saveView = new ACCSaveMenuView(uiParams);
            boneSlotView = new ACCBoneSlotView(uiParams, sliderHelper);
            views.Add(boneSlotView);
            partsColorView = new ACCPartsColorView(uiParams, sliderHelper);
            views.Add(partsColorView);

            uiParams.Add(UpdateUIParams);

            if (settings.enableTexAnim) changeDetector.Add(animDetector.ChangeMenu);
        }

        private void UpdateUIParams(UIParams uParams) {
            colorPresetMgr.BtnStyle.fontSize = uParams.fontSizeS;
            colorPresetMgr.BtnWidth = GUILayout.Width(colorPresetMgr.BtnStyle.CalcSize(new GUIContent("Update")).x);

            foreach (var view in views) {
                view.UpdateUI(uParams);
            }
        }

        public void Start() {
        }

        public void OnDestroy() {
            uiHelper.SetCameraControl(true);
            Dispose();
            presetNames.Clear();

            uiParams.Remove(UpdateUIParams);
            //detector.Clear();
#if UNITY_5_5_OR_NEWER
            SceneManager.sceneLoaded -= SceneLoaded;
#endif
            LogUtil.Debug("Destroyed");
        }

#if UNITY_5_5_OR_NEWER
        public void SceneLoaded(Scene scene, LoadSceneMode sceneMode) {
            LogUtil.Debug(scene.buildIndex, ": ", scene.name);
            
            OnSceneLoaded(scene.buildIndex);
        }
#else
        public void OnLevelWasLoaded(int level) {
            // Log.Debug("OnLevelWasLoaded ", level);
            OnSceneLoaded(level);
        }
#endif
        public void OnSceneLoaded(int level) {
            fPassedTime = 0f;
            bUseStockMaid = false;
            foreach (var view in views) {
                view.Clear();
            }
            changeDetector.Clear();

            if ( !checker.IsTarget(level) ) {
                if (!isActive) return;

                uiHelper.SetCameraControl(true);
                initialized = false;
                isActive = false;
                return;
            }

            bUseStockMaid = checker.IsStockTarget(level);
            menuType = MenuType.None;
            mouseDowned    = false;
            uiHelper.cursorContains = false;
            isActive = true;
        }

        public void Update() {
            fPassedTime += Time.deltaTime;
            if (!isActive) return;

            if (!initialized) {
                if (fPassedTime - fLastInitTime <= 1f) return;

                fLastInitTime = fPassedTime;
                initialized = Initialize();
                LogUtil.Debug("Initialized ", initialized);
                if (!initialized) return;
            }
            if (settings.enableTexAnim) changeDetector.Detect(bUseStockMaid);

            if (InputModifierKey() && Input.GetKeyDown(settings.toggleKey)) {
                SetMenu( (menuType == MenuType.None)? MenuType.Main : MenuType.None );
                mouseDowned = false;
            }
            UpdateCameraControl();

            // 選択中のメイドが有効でなければ何もしない
            if (!holder.CurrentActivated()) return;
            boneSlotView.Update();

            if (toApplyPresetMaid != null && !toApplyPresetMaid.IsBusy) {
                var targetMaid = toApplyPresetMaid;
                toApplyPresetMaid = null;
                plugin.StartCoroutine(DelayFrameRecall(applyDelayFrame, () => !ApplyPresetProp(targetMaid,currentPreset)) );
            }
            if (ACCTexturesView.fileBrowser != null) {
                if (Input.GetKeyDown(settings.prevKey)) {
                    ACCTexturesView.fileBrowser.Prev();
                }
                if (Input.GetKeyDown(settings.nextKey)) {
                    ACCTexturesView.fileBrowser.Next();
                }

                ACCTexturesView.fileBrowser.Update();
            }
            // テクスチャエディットの反映
            if (menuType == MenuType.Texture) {
                // マウスが離されたタイミングでのみテクスチャ反映
                if (texSliderUpped || Input.GetMouseButtonUp(0)) {
                    if (ACCTexturesView.IsChangeTarget()) {
                        ACCTexturesView.UpdateTex(holder.CurrentMaid, targetMaterials);
                    }
                    texSliderUpped = false;
                }
            } else {
                // テクスチャモードでなければ、テクスチャ変更対象を消す
                ACCTexturesView.ClearTarget();
            }
        }

        private void UpdateSelectMaid() {
            InitMaidList();
            if (_maidList.Count == 1) {
                var maid = _maidList[0].maid;
                var maidName = _maidList[0].content.text;
                holder.UpdateMaid(maid, maidName, ClearMaidData);

                SetMenu(MenuType.Main);
            } else {
                SetMenu(MenuType.MaidSelect);
                uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoSelectMaid, Version, uiParams.winStyle);
            }
        }

        public void OnGUI() {
            if (!isActive || menuType == MenuType.None) return;
            if (settings.SSWithoutUI && !uiHelper.IsEnabledUICamera()) return; // UI無し撮影

            try {
                if (Event.current.type == EventType.Repaint) return;
                if (!holder.CurrentActivated()) {
                    // メイド未選択、あるいは選択中のメイドが無効化された場合
                    UpdateSelectMaid();
    
                } else if (ACCTexturesView.fileBrowser != null) {
                    uiParams.fileBrowserRect = GUI.Window(WIN_ID_DIALOG, uiParams.fileBrowserRect, DoFileBrowser, Version, uiParams.winStyle);

                } else if (saveView.showDialog) {
                    uiParams.modalRect = GUI.ModalWindow(WIN_ID_MAIN, uiParams.modalRect, DoSaveModDialog, "menuエクスポート", uiParams.dialogStyle);

                } else {
                    switch (menuType) {
                    case MenuType.Main:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoMainMenu, Version, uiParams.winStyle);
                        break;
                    case MenuType.Color:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoColorMenu, Version, uiParams.winStyle);
                        break;
                    case MenuType.MaskSelect:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoMaskSelectMenu, Version, uiParams.winStyle);
                        break;
                    case MenuType.NodeSelect:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoNodeSelectMenu, Version, uiParams.winStyle);
                        break;
                    case MenuType.Save:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoSaveMenu, Version, uiParams.winStyle);
                        break;
                    case MenuType.PresetSelect:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoSelectPreset, Version, uiParams.winStyle);
                        break;
                    case MenuType.Texture:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoSelectTexture, Version, uiParams.winStyle);
                        break;
                    case MenuType.BoneSlotSelect:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoSelectBoneSlot, Version, uiParams.winStyle);
                        break;
                    case MenuType.PartsColor:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoEditPartsColor, Version, uiParams.winStyle);
                        break;
                    case MenuType.MaidSelect:
                        uiParams.winRect = GUI.Window(WIN_ID_MAIN, uiParams.winRect, DoSelectMaid, Version, uiParams.winStyle);
                        break;
                    default:
                        break;
                    }
                    OnTips();
                }

                // 領域内でマウスダウン => マウスアップ 操作の場合に入力をリセット
                if (Input.GetMouseButtonUp(0)) {
                    if (mouseDowned)  {
                        Input.ResetInputAxes();
                        texSliderUpped = (menuType == MenuType.Texture);
                    }
                    mouseDowned = false;
                }
                mouseDowned |= uiHelper.cursorContains && Input.GetMouseButtonDown(0);
            } finally {
            }
        }
        #endregion

        private void OnTips() {
            if (displayTips && tips != null) {
                GUI.Window(WIN_ID_TIPS, tipRect, DoTips, tips, uiParams.tipsStyle);
            }
        }

        public void SetTips(string message) {
        
            var lineNum = 1;
            foreach (var chr in message) {
                if (chr == '\n') lineNum++;
            }
            if (lineNum == 1) lineNum += (message.Length / 15);
            float height = lineNum*uiParams.fontSize*19/14 + 30;

            if (height > 400) height = 400;
            tipRect = new Rect(uiParams.winRect.x+TIPS_MARGIN, uiParams.winRect.yMin+150,
                               uiParams.winRect.width-TIPS_MARGIN*2, height);
            displayTips = true;
            tips = message;

            plugin.StartCoroutine(DelaySecond(tipsSecond, () => {
                 displayTips = false;
                 tips = null;
            }) );
        }

        public void DoTips(int winID) {
            GUI.BringWindowToFront(winID);
        }

        private bool InputModifierKey() {
            var em = Event.current.modifiers;
            if (settings.toggleModifiers == EventModifiers.None) {
                // 修飾キーが押されていない事を確認(Shift/Alt/Ctrl)
                return (em & modifierKey) == EventModifiers.None;
            }
            // 修飾キーが指定されている場合、そのキーの入力チェック
            return (em & settings.toggleModifiers) != EventModifiers.None;
        }

        /// <summary>
        /// カーソル位置のチェックを行い、カメラコントロールの有効化/無効化を行う
        /// </summary>
        private void UpdateCameraControl() {
            var cursorContains = false;
            if (ACCTexturesView.fileBrowser != null || menuType != MenuType.None) {
                var cursor = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                var rect = ACCTexturesView.fileBrowser != null ? uiParams.fileBrowserRect : uiParams.winRect;
                cursorContains = rect.Contains(cursor);
                if (!cursorContains) {
                    if (saveView.showDialog) {
                        cursorContains = uiParams.modalRect.Contains(cursor);
                    }
                }
            }
            uiHelper.UpdateCameraControl(cursorContains);
        }

        #region Private methods
        private void Dispose() {
            ClearMaidData();
            uiHelper.SetCameraControl(true);
            mouseDowned    = false;

            // テクスチャキャッシュを開放する
            ACCTexturesView.Clear();
            ResourceHolder.Instance.Clear();
            //OnDestroy();

            foreach (var view in views) {
                view.Dispose();
            }
            initialized = false;
        }

        private bool Initialize() {
            InitMaidInfo();

            uiParams.Update();
            ACCTexturesView.Init(uiParams);
            ACCMaterialsView.Init(uiParams);

            return true;
            //return holder.currentMaid != null;
        }

        private void InitMaidInfo() {
            // ここでは、最初に選択可能なメイドを選択
            holder.UpdateMaid(ClearMaidData);
        }

        // http://qiita.com/toRisouP/items/e402b15b36a8f9097ee9
        IEnumerator DelayFrameRecall(int delayFrame, Func<bool> func) {
            do {
                for (var i = 0; i < delayFrame; i++) {
                    yield return null;
                }
            } while (func());
        }

        IEnumerator DelayFrame(int delayFrame, Action act) {
            for (var i = 0; i < delayFrame; i++) {
                yield return null;
            }

            act();
        }

        IEnumerator DelaySecond(int second, Action act) {
            yield return new WaitForSeconds(second);
            act();
        }

        private void ClearMaidData() {
            ACCMaterialsView.Clear();
            dDelNodes.Clear();
            dDelNodeDisps.Clear();
            dMaskSlots.Clear();
        }

        private void SetMenu(MenuType type) {
            if (menuType == type) return;
            menuType = type;

            uiParams.Update();
        }
        #endregion

        private GUIContent GetOrAddMaidInfo(Maid m, int idx=-1) {
            GUIContent content;
            var id = m.gameObject.GetInstanceID();
            if (_contentDic.TryGetValue(id, out content)) return content;
            LogUtil.Debug("maid:", m.name);

            var maidName = !m.boMAN ? MaidHelper.GetName(m) : "男"+ (idx+1);
            var icon = m.GetThumIcon();
            content = new GUIContent(maidName, icon);
            _contentDic[id] = content;
            return content;
        }

        private bool IsEnabled(Maid m) {
            return m.isActiveAndEnabled && m.Visible ;// && m.body0.Face != null;
        }

        internal class SelectMaidData {
            public readonly Maid maid;
            public readonly GUIContent content;
            internal SelectMaidData(Maid maid0, GUIContent content0) {
                maid = maid0;
                content = content0;
            }
        }

        private void InitMaidList() {
            _maidList.Clear();
            _manList.Clear();
            var chrMgr = GameMain.Instance.CharacterMgr;
        
            if (bUseStockMaid) {
                AddMaidList(_maidList, chrMgr.GetStockMaid, chrMgr.GetStockMaidCount());
            } else {
                AddMaidList(_maidList, chrMgr.GetMaid, chrMgr.GetMaidCount());
            }

            if (bUseStockMaid) {
                AddMaidList(_manList, chrMgr.GetStockMan, chrMgr.GetStockManCount());
            } else {
                AddMaidList(_manList, chrMgr.GetMan, chrMgr.GetManCount());
            }
        }

        private void AddMaidList(ICollection<SelectMaidData> list, Func<int, Maid> GetMaid, int count) {
            var idx = 0;
            for (var i=0; i< count; i++) {
                var m = GetMaid(i);
                if (m == null || !IsEnabled(m)) continue;
                
                string maidName;
                if (!m.boMAN) {
                    maidName = MaidHelper.GetName(m);
                } else {
                    maidName = "男"+ (idx+1);
                    idx++;
                }
                var icon = m.GetThumIcon();
                var content = new GUIContent(maidName, icon);
                list.Add(new SelectMaidData(m, content));
            }      
        }

        private void DoSelectMaid(int winID) {
            if (selectedMaid == null) selectedMaid = holder.CurrentMaid;

            GUILayout.BeginVertical();
            GUILayout.Label("メイド選択", uiParams.lStyleB);
            scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, uiParams.optSubConWidth, uiParams.optSubConHeight);
            // var chrMgr = GameMain.Instance.CharacterMgr;
            var hasSelected = false;
            try {
                foreach (var maidData in _maidList) {
                    GUI.enabled = IsEnabled(maidData.maid);
                    var selected = (selectedMaid == maidData.maid);
                    if (GUI.enabled && selected) hasSelected = true;

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(uiParams.marginL);
                    var changed = GUILayout.Toggle(selected, maidData.content, uiParams.tStyleL);
                    GUILayout.Space(uiParams.marginL);
                    GUILayout.EndHorizontal();
                    if (changed == selected) continue;

                    selectedMaid = maidData.maid;
                    selectedName = maidData.content.text;
                }
                GUI.enabled = true;
                if (!_maidList.Any()) GUILayout.Label("　なし", uiParams.lStyleB);

                GUILayout.Space(uiParams.marginL);

                if (_manList.Any()) {
                    // LogUtil.Debug("manList:", manList.Count);
                    GUILayout.Label("男選択", uiParams.lStyleB);
                    foreach (var manData in _manList) {
                        var m = manData.maid;
                        GUI.enabled = IsEnabled(m);
                        var selected = (selectedMaid == m);
                        if (GUI.enabled && selected) hasSelected = true;

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(uiParams.marginL);
                        var changed = GUILayout.Toggle(selected, manData.content, uiParams.tStyleL);
                        GUILayout.Space(uiParams.marginL);
                        GUILayout.EndHorizontal();
                        if (changed == selected) continue;
                        selectedMaid = m;
                        selectedName = manData.content.text;
                    }
                    GUI.enabled = true;
                    //if (!manList.Any()) GUILayout.Label("　なし", uiParams.lStyleB);
                }

            } finally {
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                GUI.enabled = hasSelected;
                if (GUILayout.Button( "選択", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                    SetMenu(MenuType.Main);
                    holder.UpdateMaid(selectedMaid, selectedName, ClearMaidData);
                    selectedMaid = null;
                    selectedName = null;
                    _contentDic.Clear();
                }
                GUI.enabled = true;

                if (GUILayout.Button( "一覧更新", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                    InitMaidList();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUI.DragWindow(uiParams.titleBarRect);
        }

        private void DoMainMenu(int winID) {
            GUILayout.BeginVertical();
            try {
                var maid = holder.CurrentMaid;
                GUILayout.Label(TITLE_LABEL + holder.MaidName, uiParams.lStyle);

                if (GUILayout.Button("メイド/男 選択", uiParams.bStyle)) {
                    InitMaidList();
                    SetMenu(MenuType.MaidSelect);
                }
                GUI.enabled = !maid.boMAN;
                GUILayout.BeginHorizontal();
                try {
                    if (GUILayout.Button("マスク選択", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                        SetMenu(MenuType.MaskSelect);
                        InitMaskSlots();
                    }
                    if (GUILayout.Button("表示ノード選択", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                        // 初期化済の場合のみ
                        if (dDelNodes.Any()) {
                            dDelNodeDisps = GetDelNodes();
                        }
                        SetMenu(MenuType.NodeSelect);
                    }

                } finally {
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                try {
                    GUI.enabled &= (holder.isOfficial) && (toApplyPresetMaid == null);
                    if (GUILayout.Button("プリセット保存", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                        SetMenu(MenuType.Save);
                    }
                    if (presetNames.Any()) {
                        if (GUILayout.Button("プリセット適用", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                            SetMenu(MenuType.PresetSelect);
                        }
                    }
                } finally {
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                try {
                    if (GUILayout.Button("ボーン表示", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                        SetMenu(MenuType.BoneSlotSelect);
                    }
                    if (GUILayout.Button("パーツカラー変更", uiParams.bStyle, uiParams.optSubConHalfWidth)) {
                        SetMenu(MenuType.PartsColor);
                    }
                } finally {
                    GUILayout.EndHorizontal();
                }
                GUI.enabled = true;

                GUILayout.Space(uiParams.margin);
                GUILayout.BeginHorizontal();
                GUILayout.Label("マテ情報変更 スロット選択", uiParams.lStyleC);
                nameSwitched = GUILayout.Toggle(nameSwitched, "表示切替", uiParams.tStyleS, uiParams.optToggleSWidth);
                GUILayout.EndHorizontal();
                scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, 
                                                               GUILayout.Width(uiParams.mainRect.width),
                                                               GUILayout.Height(uiParams.mainRect.height));
                try {
                    var currentBody = holder.CurrentMaid.body0;
                    if (holder.isOfficial) {
                        foreach (var slot in ACConstants.SlotNames.Values) {
                            if (!slot.enable) continue;

                            // 身体からノード一覧と表示状態を取得
                            if (!currentBody.GetSlotLoaded(slot.Id)) continue;

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(uiParams.marginL);

                            if (GUILayout.Button(!nameSwitched?slot.DisplayName: slot.Name, uiParams.bStyleL, GUILayout.ExpandWidth(true))) {
                                holder.CurrentSlot = slot;
                                SetMenu(MenuType.Color);
                            }
                            if (settings.displaySlotName) {
                                GUILayout.Label(slot.Name, uiParams.lStyleS, uiParams.optCategoryWidth);
                            }
                            GUILayout.Space(uiParams.marginL);
                            GUILayout.EndHorizontal();
                        }                    
                    } else {
                        // 下記処理は、公式のスロットIDと異なるスロットを設定するプラグイン等が導入されていた場合のため
                        var idx = 0;
                        var count = currentBody.goSlot.Count;
                        for (var i=0; i< count; i++) {
                            var tbodySlot = currentBody.goSlot[i];
                            if (!settings.enableMoza && i == count-1) {
                                if (tbodySlot.Category == "moza") continue;
                            }
                            // if (!slot.enable) continue;

                            // slot loaded
                            if (tbodySlot.obj != null) {
                                GUILayout.BeginHorizontal();
                                GUILayout.Space(uiParams.marginL);

                                if (GUILayout.Button(tbodySlot.Category, uiParams.bStyleL, GUILayout.ExpandWidth(true))) {
                                    holder.CurrentSlot = ACConstants.SlotNames[(TBody.SlotID)idx];
                                    SetMenu(MenuType.Color);
                                }
                                GUILayout.Space(uiParams.marginL);
                                GUILayout.EndHorizontal();
                            }
                            idx++;
                        }
                    }
                } finally {
                    GUI.enabled = true;
                    GUILayout.EndScrollView();
                }

            } finally {
                GUILayout.EndVertical();
            }
            GUI.DragWindow(uiParams.titleBarRect);
        }

        private List<ACCMaterialsView> InitMaterialView(Renderer r, string menufile, int slotIdx) {
            var materials = r.materials;
            var idx = 0;
            var ret = new List<ACCMaterialsView>(materials.Length);
            foreach (var material in materials) {
                var view = new ACCMaterialsView(r, material, slotIdx, idx++, sliderHelper, cbHelper) {
                    tipsCall = SetTips
                };
                ret.Add(view);

                // マテリアル数が少ない場合はデフォルトで表示
                view.expand = (materials.Length <= 2);
            }
            return ret;
        }

        private void DoColorMenu(int winID) {
            var slot = holder.GetCurrentSlot();
            if (title == null) {
                title = new GUIContent("マテリアル情報変更: " + (holder.isOfficial ? holder.CurrentSlot.DisplayName : slot.Category));
            }

            GUILayout.Label(title, uiParams.lStyleB);
            // TODO 選択アイテム名、説明等を表示 可能であればアイコンも
            if (holder.CurrentMaid.IsBusy) {
                GUILayout.Space(100);
                GUILayout.Label("変更中...", uiParams.lStyleB);
                return;
            }

            // **_del.menuを選択の状態が続いたらメインメニューへ
            // 衣装セットなどは内部的に一旦_del.menuが選択されるため、一時的に選択された状態をスルー
            var menuId = holder.GetCurrentMenuFileID();
            if (menuId != 0) {
                if (slotDropped) {
                    if (!changeCounter.Next()) return;
                    SetMenu(MenuType.Main);
                    LogUtil.Debug("select slot item dropped. return to main menu.", menuId);
                    slotDropped = false;
                    return;
                }
            }

            // ターゲットのmenuファイルが変更された場合にビューを更新
            if (targetMenuId != menuId) {
                title = null;
                // .modファイルは未対応
                var menufile = holder.GetCurrentMenuFile();
                // LogUtil.Debug("menufile changed.", targetMenuId, "=>", menuId, " : ", menufile);

                isSavable = (menufile != null) && !(menufile.ToLower().EndsWith(FileConst.EXT_MOD, StringComparison.Ordinal));

                targetMenuId = menuId;
                var renderer1 = holder.GetRenderer(slot);
                if (renderer1 != null) {
                    targetMaterials = renderer1.materials;
#if COM3D2
                    materialViews = InitMaterialView(renderer1, menufile, (int)slot.SlotId);
#else
                    materialViews = InitMaterialView(renderer1, menufile, slot.CategoryIdx);
#endif
                } else {
                    targetMaterials = EMPTY_ARRAY;
                }

                // slotにデータが装着されていないかを判定
                slotDropped = (slot.obj == null);
                changeCounter.Reset();

                if (isSavable) {
                    // 保存未対応スロットを判定(身体は不可)
                    isSavable &= (holder.CurrentSlot.Id != TBody.SlotID.body);
                }
            }

            if ( GUILayout.Button("テクスチャ変更", uiParams.bStyle) ) {
                texViews = InitTexView(targetMaterials);

                SetMenu(MenuType.Texture);
                return;
            }

            if (targetMaterials.Length <= 0) return;

            scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition,
                GUILayout.Width(uiParams.colorRect.width),
                GUILayout.Height(uiParams.colorRect.height));
            try {
                var reload = Event.current.type == EventType.Layout && refreshCounter.Next();
                if (reload) ClipBoardHandler.Instance.Reload();

                foreach (var view in materialViews) {
                    view.Show(reload);
                }

            } catch (Exception e) {
                LogUtil.Error("マテリアル情報変更画面でエラーが発生しました。メイン画面へ移動します", e);
                SetMenu(MenuType.Main);
                targetMenuId = 0;
            } finally {
                GUILayout.EndScrollView();

                GUI.enabled = isSavable;
                if (GUILayout.Button("menuエクスポート", uiParams.bStyle)) ExportMenu();
                GUI.enabled = true;

                if (GUILayout.Button("閉じる", uiParams.bStyle)) {
                    SetMenu(MenuType.Main);
                    targetMenuId = 0;
                }
                GUI.DragWindow(uiParams.titleBarRect);
            }
        }

        private void ExportMenu() {
            var slot = holder.GetCurrentSlot();
            if (slot.obj == null) {
                var msg = "指定スロットが見つかりません。slot=" + holder.CurrentSlot.Name;
                NUty.WinMessageBox(NUty.GetWindowHandle(), msg, "エラー", NUty.MSGBOX.MB_OK);
                return;
            }

            // propは対応するMPNを指定
            var prop = holder.CurrentMaid.GetProp(holder.CurrentSlot.mpn);
            if (prop == null) return;
            {
                // 変更可能なmenuファイルがない場合は保存画面へ遷移しない
                var targetSlots = saveView.Load(prop.strFileName);
                if (targetSlots == null) {
                    var msg = "変更可能なmenuファイルがありません " + prop.strFileName;
                    NUty.WinMessageBox(NUty.GetWindowHandle(), msg, "エラー", NUty.MSGBOX.MB_OK);
                } else {
                    // menuファイルで指定されているitemのスロットに関連するマテリアル情報を抽出
                    foreach (var targetSlot in targetSlots.Keys) {

                        List<ACCMaterial> edited;
                        if (targetSlot == holder.CurrentSlot.Id) {
                            // カレントスロットの場合は、作成済のマテリアル情報を渡す
                            edited = new List<ACCMaterial>(materialViews.Count);
                            foreach ( var matView in materialViews ) {
                                edited.Add(matView.edited);
                            }
                        } else {
                            var materials = holder.GetMaterials(targetSlot);
                            edited = new List<ACCMaterial>(materials.Length);
                            edited.AddRange(materials.Select(mat => new ACCMaterial(mat)));
                        }
                        saveView.SetEditedMaterials(targetSlot, edited);

                    }
                    //if (!saveView.CheckData()) {
                    //    var msg = "保存可能なmenuファイルではありません " + prop.strFileName;
                    //    NUty.WinMessageBox(NUty.GetWindowHandle(), msg, "エラー", NUty.MSGBOX.MB_OK);
                    //}
                }
            }
        }

        private List<ACCTexturesView> InitTexView(ICollection<Material> materials) {
            var ret = new List<ACCTexturesView>(materials.Count);
            var matNo = 0;
            foreach (var material in materials) {
                try {
                    var view = new ACCTexturesView(material, matNo++);
                    ret.Add(view);

                    // マテリアル数が少ない場合はデフォルトで表示
                    view.expand = (materials.Count <= 2);

                } catch(Exception e) {
                    LogUtil.Error(material.name, e);
                }
            }
            return ret;
        }

        private void DoSelectTexture(int winId) {
            // テクスチャ変更画面 GUILayout使用
            GUILayout.Label("テクスチャ変更", uiParams.lStyleB);
            scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, 
                                                           GUILayout.Width(uiParams.textureRect.width),
                                                           GUILayout.Height(uiParams.textureRect.height));
            try {
                var menuId = holder.GetCurrentMenuFileID();
                if (targetMenuId != menuId) {
                    LogUtil.DebugF("menu file changed. {0}=>{1}", targetMenuId, menuId);
                    targetMenuId = menuId;
                    targetMaterials = holder.GetMaterials();
                    texViews = InitTexView(targetMaterials);
                }

                foreach (var view in texViews) {
                    view.Show();
                }
            } catch(Exception e) {
                LogUtil.Debug("failed to create texture change view. ", e);
            } finally {
                GUILayout.EndScrollView();

                if (GUILayout.Button( "閉じる", uiParams.bStyle, 
                                     uiParams.optSubConWidth, uiParams.optBtnHeight)) {
                    SetMenu(MenuType.Color);
                }
                GUI.DragWindow(uiParams.titleBarRect);
            }
        }

        private void DoFileBrowser(int winId) {
            ACCTexturesView.fileBrowser.OnGUI();
            GUI.DragWindow(uiParams.titleBarRect);
        }

        private bool InitMaskSlots() {
            if (holder.CurrentMaid == null) return false;

    //        List<int> maskSlots = holder.maid.listMaskSlot;
            foreach (var si  in ACConstants.SlotNames.Values) {
                if (!si.enable || !si.maskable) continue;
                var slotNo = (int)si.Id;
                if (slotNo >= holder.CurrentMaid.body0.goSlot.Count) continue;

                var slot = holder.CurrentMaid.body0.GetSlot(slotNo);
                MaskInfo mi;
                if (!dMaskSlots.TryGetValue(si.Id, out mi)) {
                    mi = new MaskInfo(si, slot);
                    dMaskSlots[si.Id] = mi;
                } else {
                    mi.slot = slot;
                }
                mi.value = slot.boVisible;
            }
            return true;
        }

        private void DoMaskSelectMenu(int winID) {
            var bWidth  = GUILayout.Width(uiParams.subConWidth*0.32f);
            var bWidthS = GUILayout.Width(uiParams.subConWidth*0.24f);
            var lStateWidth = GUILayout.Width(uiParams.fontSize*4f);
            var titleWidth = GUILayout.Width(uiParams.fontSize*10f);
            GUILayout.BeginVertical();
            try {
                // falseがマスクの模様
                GUILayout.BeginHorizontal();
                GUILayout.Label("マスクアイテム選択", uiParams.lStyleB, titleWidth);
                nameSwitched = GUILayout.Toggle(nameSwitched, "表示切替", uiParams.tStyleS, uiParams.optToggleSWidth);
                GUILayout.EndHorizontal();

                if (holder.CurrentMaid == null) return ;

                // 身体からノード一覧と表示状態を取得
                if (!dMaskSlots.Any()) {
                    InitMaskSlots();
                }

                GUILayout.BeginHorizontal();
                try {
                    if (GUILayout.Button("同期", uiParams.bStyle, uiParams.optBtnHeight, bWidth)) { 
                        InitMaskSlots();
                    }
                    if (GUILayout.Button("すべてON", uiParams.bStyle, uiParams.optBtnHeight, bWidth)) {
                        var keys = new List<TBody.SlotID>(dMaskSlots.Keys);
                        foreach (var key in keys) {
                            dMaskSlots[key].value = false;
                        }
                    }
                    if (GUILayout.Button("すべてOFF", uiParams.bStyle, uiParams.optBtnHeight, bWidth)) { 
                        var keys = new List<TBody.SlotID>(dMaskSlots.Keys);
                        foreach (var key in keys) {
                            dMaskSlots[key].value = true;
                        }
                    }
                    // 下着モード,水着モード,ヌードモード
                } finally {
                    GUILayout.EndHorizontal();
                }
                scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, 
                                                               GUILayout.Width(uiParams.nodeSelectRect.width),
                                                               GUILayout.Height(uiParams.nodeSelectRect.height));
                var labelStyle = uiParams.lStyle;
                var bkColor = labelStyle.normal.textColor;
                try {
                    foreach (var pair in dMaskSlots) {
                        // if (pair.Key <= TBody.SlotID.eye) continue;

                        var maskInfo = pair.Value;
                        string state;
                        // 下着、ヌードモードなどによる非表示
                        if (!holder.CurrentMaid.body0.GetMask(maskInfo.slotInfo.Id)) {
                            state = "[非表示]";
                            labelStyle.normal.textColor = Color.magenta;
                        } else {
                            maskInfo.UpdateState();
                            switch(maskInfo.state) {
                                case SlotState.NotLoaded:
                                    //continue;
                                    state = "[未読込]";
                                    labelStyle.normal.textColor = Color.red;
                                    GUI.enabled = false;
                                    break;
                                case SlotState.Masked:
                                    state = "[マスク]";
                                    labelStyle.normal.textColor = Color.cyan;
                                    break;
                                case SlotState.Displayed:
                                    state = "[表示中]";
                                    labelStyle.normal.textColor = bkColor;
                                    break;
                                default:
                                    state = "unknown";
                                    labelStyle.normal.textColor = Color.red;
                                    GUI.enabled = false;
                                    break;
                            }
                        }
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(state, labelStyle, lStateWidth);
                        // dMaskSlotsはCM3D2のデータと合わせてマスクオン=falseとし、画面上はマスクオン=選択(true)とする
                        maskInfo.value = !GUILayout.Toggle( !maskInfo.value, maskInfo.Name(!nameSwitched),
                                                           uiParams.tStyle, uiParams.optContentWidth
                                                           ,GUILayout.ExpandWidth(true)
                                                          );
                        //GUILayout.Label( maskInfo.slotInfo.Name, uiParams.lStyleRS, uiParams.optSlotWidth);
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                } finally {
                    labelStyle.normal.textColor = bkColor;
                    GUI.EndScrollView();
                }
            } finally {
                GUILayout.EndVertical();
            }

            GUILayout.BeginHorizontal();
            try {
                if (GUILayout.Button("一時適用", uiParams.bStyle, bWidthS, uiParams.optBtnHeight)) {
                    holder.SetSlotVisibles(holder.CurrentMaid, dMaskSlots, true);
                }
                if (GUILayout.Button("適用", uiParams.bStyle, bWidthS, uiParams.optBtnHeight)) {
                    holder.SetSlotVisibles(holder.CurrentMaid, dMaskSlots, false);
                    holder.FixFlag();
                }
                if (GUILayout.Button("全クリア", uiParams.bStyle, bWidthS, uiParams.optBtnHeight)) {
                    holder.SetAllVisible();
                }
                if (GUILayout.Button("戻す", uiParams.bStyle, bWidthS, uiParams.optBtnHeight)) {
                    holder.FixFlag();
                }
            } finally {
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button( "閉じる", uiParams.bStyle, uiParams.optSubConWidth, uiParams.optBtnHeight)) {
                SetMenu(MenuType.Main);
            }

            GUI.DragWindow(uiParams.titleBarRect);
        }

        private bool InitDelNodes(TBodySkin body) {
            if (body == null) {
                if (holder.CurrentMaid == null) return false;

                const int slotNo = (int)TBody.SlotID.body;
                // 身体からノード一覧と表示状態を取得
                if (slotNo >= holder.CurrentMaid.body0.goSlot.Count) return false;
                body = holder.CurrentMaid.body0.GetSlot(slotNo);
            }
            var dic = body.m_dicDelNodeBody;
            foreach (var key in ACConstants.NodeNames.Keys) {
                bool val;
                if (dic.TryGetValue(key, out val)){
                    dDelNodes[key] = val;
                }
            }
            return true;
        }

        /// <summary>
        /// 現在のノードの表示状態を表すDictionaryを取得する
        /// </summary>
        /// <returns>現在のノードの表示状態Dic</returns>
        private Dictionary<string, bool> GetDelNodes() {
            if (!dDelNodes.Any()) InitDelNodes(null);

            var keys = new List<string>(dDelNodes.Keys);
            var delNodeDic = new Dictionary<string, bool>(dDelNodes);
            foreach (var key in keys) {
                delNodeDic[key] = true;
            }
            //foreach(var slot in holder.CurrentMaid.body0.goSlot) {
            for (int Index = 0; Index < holder.CurrentMaid.body0.goSlot.Count; Index++)
            {
                var slot = holder.CurrentMaid.body0.goSlot[Index];
                if (slot.obj == null || !slot.boVisible) continue;

                var slotNodes = slot.m_dicDelNodeBody;
                // 1つでもFalseがあったら非表示とみなす
                foreach (var key in keys) {
                    bool v;
                    if (slotNodes.TryGetValue(key, out v)) {
                        delNodeDic[key] &= v;
                    }
                }
                if (!slot.m_dicDelNodeParts.Any()) continue;

                foreach(var sub in slot.m_dicDelNodeParts.Values) {
                    foreach(var pair in sub) {
                        if (delNodeDic.ContainsKey(pair.Key)) {
                            delNodeDic[pair.Key] &= pair.Value;
                        }
                    }
                }
            }
            return delNodeDic;
        }

        private void DoNodeSelectMenu(int winID) {
            GUILayout.BeginVertical();
            var titleWidth = GUILayout.Width(uiParams.fontSize*10f);
            var lStateWidth = GUILayout.Width(uiParams.fontSize*4f);
            try {
                GUILayout.BeginHorizontal();
                GUILayout.Label("表示ノード選択", uiParams.lStyleB, titleWidth);
                GUILayout.Space(uiParams.margin);
                nameSwitched = GUILayout.Toggle(nameSwitched, "表示切替", uiParams.tStyleS, uiParams.optToggleSWidth);
                GUILayout.EndHorizontal();

                if (holder.CurrentMaid == null) return ;

                const int slotNo = (int)TBody.SlotID.body;
                // 身体からノード一覧と表示状態を取得
                if (slotNo >= holder.CurrentMaid.body0.goSlot.Count) return;
                var body = holder.CurrentMaid.body0.GetSlot(slotNo);
                if (!dDelNodes.Any()) {
                    InitDelNodes(body);
                    dDelNodeDisps = GetDelNodes();
                    // 表示ノード状態をUI用データに反映
                    foreach (var nodes in dDelNodeDisps) {
                        dDelNodes[nodes.Key] = nodes.Value;
                    }
                }

                GUILayout.BeginHorizontal();
                try {
                    var bWidth = GUILayout.Width(uiParams.subConWidth*0.33f);
                    if (GUILayout.Button("同期", uiParams.bStyle, uiParams.optBtnHeight, bWidth)) {
                        SyncNodes();
                    }
                    if (GUILayout.Button("すべてON", uiParams.bStyle, uiParams.optBtnHeight, bWidth)) {
                        var keys = new List<string>(dDelNodes.Keys);
                        foreach (var key in keys) {
                            dDelNodes[key] = true;
                        }
                    }
                    if (GUILayout.Button("すべてOFF", uiParams.bStyle, uiParams.optBtnHeight, bWidth)) { 
                        var keys = new List<string>(dDelNodes.Keys);
                        foreach (var key in keys) {
                            dDelNodes[key] = false;
                        }
                    }

                } finally {
                    GUILayout.EndHorizontal();
                }
                scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, 
                                                               GUILayout.Width(uiParams.nodeSelectRect.width),
                                                               GUILayout.Height(uiParams.nodeSelectRect.height));
                var labelStyle = uiParams.lStyle;
                var bkColor = labelStyle.normal.textColor;
                try {
                    foreach (var pair in ACConstants.NodeNames) {
                        var nodeItem = pair.Value;
                        bool delNode;
                        if (!dDelNodes.TryGetValue(pair.Key, out delNode)) {
                            LogUtil.Debug("node name not found.", pair.Key);
                            continue;
                        }
                        GUILayout.BeginHorizontal();
                        try {
                            string state;
                            var isValid = true;
                            bool bDel;
                            if (dDelNodeDisps.TryGetValue(pair.Key, out bDel)) {
                                if (bDel) {
                                    state = "[表示中]";
                                    labelStyle.normal.textColor = bkColor;
                                } else {
                                    state = "[非表示]";
                                    labelStyle.normal.textColor = Color.magenta;
                                }
                            } else {
                                state = "[不　明]";
                                labelStyle.normal.textColor = Color.red;
                                isValid = false;
                            }
                            GUILayout.Label(state, labelStyle, lStateWidth);

                            if (nodeItem.depth != 0) {
                                GUILayout.Space(uiParams.margin * nodeItem.depth*3);
                            }
                            GUI.enabled = isValid;
                            dDelNodes[pair.Key] = GUILayout.Toggle( delNode, !nameSwitched?nodeItem.DisplayName: pair.Key,
                                                                   uiParams.tStyle, uiParams.optContentWidth);
                            GUI.enabled = true;
                        } finally {
                            GUILayout.EndHorizontal();
                        }
                    }
                } finally {
                    uiParams.lStyle.normal.textColor = bkColor;
                    GUI.EndScrollView();
                }

            } finally {
                GUILayout.EndVertical();
            }
            GUILayout.BeginHorizontal();
            try {
                if (GUILayout.Button("適用", uiParams.bStyle, uiParams.optSubConHalfWidth, uiParams.optBtnHeight)) {
                    holder.SetDelNodes(dDelNodes, true);
                    //dDelNodeDisps = new Dictionary<string, bool>(body.m_dicDelNodeBody);
                    plugin.StartCoroutine(DelayFrame(3, SyncNodes));
                }
                GUILayout.Space(uiParams.margin);
                if (GUILayout.Button("強制適用", uiParams.bStyle, uiParams.optSubConHalfWidth, uiParams.optBtnHeight)) {
                    holder.SetDelNodesForce(dDelNodes, true);
                    //dDelNodeDisps = new Dictionary<string, bool>(body.m_dicDelNodeBody);
                    plugin.StartCoroutine(DelayFrame(3, SyncNodes));
                }
            } finally {
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button( "閉じる", uiParams.bStyle, uiParams.optSubConWidth, uiParams.optBtnHeight)) {
                SetMenu(MenuType.Main);
            }
            GUI.DragWindow(uiParams.titleBarRect);
        }

        private void SyncNodes() {
            dDelNodeDisps = GetDelNodes();
            foreach (var nodes in dDelNodeDisps) {
                dDelNodes[nodes.Key] = nodes.Value;
            }
        }

        private void DoSaveMenu(int winID) {
            GUILayout.BeginVertical();
            try {
                GUILayout.Label("プリセット保存", uiParams.lStyleB);

                GUILayout.Label("プリセット名", uiParams.lStyle);
                var editText = GUILayout.TextField(presetName, uiParams.textStyle, GUILayout.ExpandWidth(true));
                if (editText != presetName) {
                    bPresetSavable = !FileConst.HasInvalidChars(editText);
                    presetName = editText;
                }
                if (bPresetSavable) {
                    bPresetSavable &= editText.Trim().Length != 0;
                }

                scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, uiParams.optSubConWidth, uiParams.optSubCon6Height);
                GUILayout.BeginHorizontal();
                GUILayout.Space(uiParams.marginL);
                GUILayout.Label("《保存済みプリセット一覧》", uiParams.lStyle);
                GUILayout.EndHorizontal();
                foreach (var presetName1 in presetNames) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(uiParams.marginL);
                    if (GUILayout.Button(presetName1, uiParams.lStyleS)) {
                        presetName = presetName1;
                        bPresetSavable = !FileConst.HasInvalidChars(presetName);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                GUI.enabled = bPresetSavable;
                if (GUILayout.Button("保存", uiParams.bStyle, GUILayout.ExpandWidth(true))) {
                    SavePreset(presetName);
                }
                GUI.enabled = true;
                if (GUILayout.Button("閉じる", uiParams.bStyle, GUILayout.ExpandWidth(true))) {
                    SetMenu(MenuType.Main);
                }
            } finally {
                GUILayout.EndHorizontal();
                GUI.DragWindow(uiParams.titleBarRect);
            }
        }

        private void DoSelectPreset(int winId) {
            GUILayout.BeginVertical();
            try {
                GUILayout.Label("プリセット適用", uiParams.lStyleB);

                GUILayout.BeginHorizontal();
                GUILayout.Space(uiParams.marginL);
                GUILayout.Label("《適用項目》", uiParams.lStyle);
                GUILayout.Space(uiParams.marginL);
                bPresetApplyBodyProp = GUILayout.Toggle(bPresetApplyBodyProp, "身体設定値", uiParams.tStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(uiParams.marginL*2);
                bPresetApplyMask = GUILayout.Toggle(bPresetApplyMask, "マスク", uiParams.tStyle);
                bPresetApplyNode = GUILayout.Toggle(bPresetApplyNode, "ノード表示", uiParams.tStyle);
                bPresetApplyPartsColor = GUILayout.Toggle(bPresetApplyPartsColor, "無限色", uiParams.tStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(uiParams.marginL*2);
                bPresetApplyBody = GUILayout.Toggle(bPresetApplyBody, "身体", uiParams.tStyle);
                bPresetApplyWear = GUILayout.Toggle(bPresetApplyWear, "衣装", uiParams.tStyle);
                bPresetCastoff   = GUILayout.Toggle(bPresetCastoff,   "衣装外し", uiParams.tStyle);
                GUILayout.EndHorizontal();

                scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, uiParams.optSubConWidth, uiParams.optSubCon6Height);
                try {
                    foreach (var presetName1 in presetNames) {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(presetName1, uiParams.bStyleL)) {
                            if (ApplyPreset(presetName1)) {
                                SetMenu(MenuType.Main);
                            }
                        }
                        if (GUILayout.Button("削除", uiParams.bStyle, uiParams.optDBtnWidth)) {
                            DeletePreset(presetName1);
                        }
                        GUILayout.EndHorizontal();
                    }
                } finally {
                    GUILayout.EndScrollView();
                }
                if (GUILayout.Button("閉じる", uiParams.bStyle)) {
                    SetMenu(MenuType.Main);
                }
            } finally {
                GUILayout.EndHorizontal();
                GUI.DragWindow(uiParams.titleBarRect);
            }
        }

        private void DeletePreset(string presetName1) {
            if (!Directory.Exists(settings.presetDirPath)) return;

            var filepath = presetMgr.GetPresetFilepath(presetName1);
            if (!File.Exists(filepath)) return;
            File.Delete(filepath);
            LoadPresetList();
        }

        private void SavePreset(string presetName1) {
            if (!Directory.Exists(settings.presetDirPath)) Directory.CreateDirectory(settings.presetDirPath);

            try {
                var filepath = presetMgr.GetPresetFilepath(presetName1);
                dDelNodeDisps = GetDelNodes();
                presetMgr.Save(filepath, presetName1, dDelNodeDisps);
                SetMenu(MenuType.Main);

                // 一覧を更新
                LoadPresetList();
            } catch(Exception e) {
                LogUtil.Error(e);
            }        
        }

        private bool ApplyPreset(string presetName1) {
            LogUtil.Debug("Applying Preset. ", presetName1);
            var filename = presetMgr.GetPresetFilepath(presetName1);
            if (!File.Exists(filename)) return false;

            currentPreset = presetMgr.Load(filename);
            if (currentPreset == null) return false;

            ApplyPreset(currentPreset);
            return true;
        }

        private void ApplyPreset(PresetData preset) {
            if (preset == null) return;

            var maid = holder.CurrentMaid;
            // 衣装チェンジ
            if (preset.mpns.Any()) {
                presetMgr.ApplyPresetMPN(maid, preset, bPresetApplyBody, bPresetApplyWear, bPresetCastoff);
            }
            // 身体設定値
            if (bPresetApplyBodyProp & preset.mpnvals.Any()) {
                presetMgr.ApplyPresetMPNProp(maid, preset);
            }

            // 一旦、衣装や身体情報を適用⇒反映待ちをして、Coroutineにて残りを適用
            maid.AllProcPropSeqStart();
            toApplyPresetMaid = maid;

            // ApplyPresetProp(preset);は後で実行する
            // toApplyPresetMaid を指定することでメイド情報のロード完了後に実行
        }

        // ACCの変更情報を適用する
        private bool ApplyPresetProp(Maid targetMaid, PresetData preset) {
            try {
                // 準備ができていない場合に、再度呼び出してもらうためにfalseを返す (ありえないはず)
                if (targetMaid.boAllProcPropBUSY) {
                    LogUtil.Debug("recall apply preset");
                    return false;
                }

                // 対象メイドが変更された場合はスキップ
                if (holder.CurrentMaid != targetMaid) return true;

                if (bPresetApplyWear) {
                    presetMgr.ApplyPresetMaterial(targetMaid, preset);
                }

                if (bPresetApplyNode && preset.delNodes != null) {
                    // 表示ノードを反映 (プリセットで未定義のノードは変更されない）
                    foreach (var node in preset.delNodes) {
                        dDelNodes[node.Key] = node.Value;
                    }
                    holder.SetDelNodes(targetMaid, preset, false);
                }

                if (bPresetApplyMask) {
                    holder.SetMaskSlots(targetMaid, preset);
                }
                holder.FixFlag(targetMaid);

                // freeColor
                if (bPresetApplyPartsColor && preset.partsColors.Any()) {
                    presetMgr.ApplyPresetPartsColor(targetMaid, preset);
                }

            } finally {
                LogUtil.Debug("Preset applied");
            }

            return true;
        }

        private void LoadPresetList() {
            try {
                if (!Directory.Exists(settings.presetDirPath)) {
                    presetNames.Clear();
                    return;
                }

                var files = Directory.GetFiles(settings.presetDirPath, "*.json", SearchOption.AllDirectories);
                var fileNum = files.Count();
                if (fileNum == 0) {
                    presetNames.Clear();
                } else {
                    Array.Sort(files);
                    presetNames.Clear();
                    presetNames.Capacity = fileNum;
                    foreach (var file in files) {
                        presetNames.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
            } catch(Exception e) {
                LogUtil.Debug(e);
            }
        }

        private void DoSaveModDialog(int winId) {
            try {
                saveView.Show();
            } catch(Exception e) {
                LogUtil.Debug("failed to display save dialog.", e);
            }

            GUI.DragWindow(uiParams.titleBarRect);
        }

        private void DoSelectBoneSlot(int winId) {
            try {
                boneSlotView.Show();
            } finally {
                if (GUILayout.Button("閉じる", uiParams.bStyle, uiParams.optSubConWidth, uiParams.optBtnHeight)) {
                    SetMenu(MenuType.Main);
                }
                GUI.DragWindow(uiParams.titleBarRect);
            }
        }

        private void DoEditPartsColor(int winId) {
            try {
                partsColorView.Show();
            } finally {
                if (GUILayout.Button("閉じる", uiParams.bStyle, uiParams.optSubConWidth, uiParams.optBtnHeight)) {
                    SetMenu(MenuType.Main);
                }
                GUI.DragWindow(uiParams.titleBarRect);
            }
        }
   }
}
