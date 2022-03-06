﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using CM3D2.AlwaysColorChangeEx.Plugin.Data;
using CM3D2.AlwaysColorChangeEx.Plugin.Util;

namespace CM3D2.AlwaysColorChangeEx.Plugin.UI {
    /// <summary>
    /// Exportメニューのビュークラス
    /// </summary>
    public class ACCSaveMenuView {
        private static readonly Settings settings = Settings.Instance;
        private static readonly FileUtilEx fileUtil = FileUtilEx.Instance;

        public static void Init(UIParams uiparams) {
            if (uiParams != null) return;
            uiParams = uiparams;
            uiParams.Add(updateUI);

            InitUIParams(uiparams);
        }

        public static void Clear() {
            if (uiParams != null) uiParams.Remove(updateUI);
        }

        private static UIParams uiParams;

        private static int _fontSize;
        private static int _fontSizeS;
        private static float _indentWidth;
        private static GUILayoutOption _optLabelWidth;
        private static GUILayoutOption _modalHalfWidth;
        private static GUILayoutOption _optSubLabelWidth;
        private static GUILayoutOption _optSubItemHeight;
        private static GUILayoutOption _optExtLabelWidth;
        private static GUILayoutOption _optShaderWidth;
        private static GUILayoutOption _optPropNameWidth;
        private static GUILayoutOption _optScrlWidth;
        private static GUILayoutOption _optScrlHeight;
        private static GUILayoutOption _optTwoLineHeight;

        private static void InitUIParams(UIParams uiparam) { }

        private static Action<UIParams> updateUI = (uiparams) => {
            // opt
            _optLabelWidth    = GUILayout.Width(uiparams.modalRect.width * 0.16f);
            _modalHalfWidth   = GUILayout.Width(uiparams.modalRect.width * 0.34f);
            _optSubLabelWidth = GUILayout.Width(uiparams.modalRect.width * 0.22f);
            _optSubItemHeight = GUILayout.MaxHeight(uiparams.itemHeight*0.8f);

            _fontSize  = uiparams.fontSize;
            _fontSizeS = uiparams.fontSizeS;
            _optExtLabelWidth = GUILayout.Width(_fontSizeS*3);
            _optShaderWidth   = GUILayout.Width(_fontSizeS*ShaderType.MaxNameLength()*0.68f);
            _optPropNameWidth = GUILayout.Width(_fontSizeS*14*0.68f);
            _indentWidth = uiparams.margin*8f;

            _optScrlWidth  = GUILayout.Width(uiparams.modalRect.width-20);
            _optScrlHeight = GUILayout.Height(uiparams.modalRect.height-55);
            
            _optTwoLineHeight = GUILayout.MinHeight(uiparams.unitHeight*2.5f);

        };

        public ComboBox shaderCombo;
        public bool showDialog;

        private Vector2 scrollViewPosition = Vector2.zero;
        private bool nameInterlocked;
        private bool nameChanged;
        private bool ignoreExist;
        private Color changedColor = Color.red;

        public ACCMenu trgtMenu;
        public ACCSaveMenuView(UIParams up) {
            Init(up);
        }

        public Dictionary<TBody.SlotID, Item> Load(string filename) {
            trgtMenu = ACCMenu.Load(filename);
            nameInterlocked = false;
            if (trgtMenu != null) {
                showDialog = true;
                return trgtMenu.itemSlots;
            } else {
                // エラー確認用処理
                var aFileBase = GameUty.FileOpen(filename);
                if (!aFileBase.IsValid()) return null;

                //const int BUFFER_SIZE = 8192;
                    
                var dir = OutputUtil.Instance.GetExportDirectory();
                var outfile = Path.Combine(dir, filename);
                LogUtil.Error("MENUファイルを出力します。", outfile);
                using ( var writer = new BinaryWriter(File.OpenWrite(outfile)) ) {
                    writer.Write(aFileBase.ReadAll());
                }
            }
            return null;
        }

        public void SetEditedMaterials(TBody.SlotID slot, List<ACCMaterial> edited) {
            LogUtil.DebugF("Set edited Materials. slot={0}, edited count={1}", slot, edited.Count);
            
            var slotName = slot.ToString();
            trgtMenu.InitMaterials(slotName, edited);
        }

        //public bool CheckData() {
        //    return true;
        //}

        // TODO 複数のマテリアルのmateファイルが同一の場合
        //        設定、textureが同じであるかを判定して、同一にしてよいかを判定⇒変更前のファイルが同一であれば一緒。
        // 複数のスロットで同一のmodelファイルを指定している場合、
        //   現状は、他方へリンクを張り、同一modelとして出力
        //   シェーダが異なる場合は別modelとして出力する必要がある(TODO)

        // TODO 汚すぎるため、要リファクタ
        public void Show() {
            if (trgtMenu == null) return;

            scrollViewPosition = GUILayout.BeginScrollView(scrollViewPosition, _optScrlWidth, _optScrlHeight);

            GUILayout.BeginVertical();
            GUILayout.Space(uiParams.unitHeight);
            var txtColr = uiParams.textStyle.normal.textColor;
            var errColr = Color.red;
            try {
                GUILayout.BeginHorizontal();
                GUILayout.Label("メニュー", uiParams.lStyle, _optLabelWidth);
                var before = trgtMenu.editfile;
                if (trgtMenu.editfileExist) uiParams.textStyle.normal.textColor = errColr;
                trgtMenu.editfile = GUILayout.TextField(trgtMenu.editfile, uiParams.textStyle);
                if (trgtMenu.editfileExist) uiParams.textStyle.normal.textColor = txtColr;
                nameChanged |= (trgtMenu.editfile != before);
                GUILayout.Label(FileConst.EXT_MENU, uiParams.lStyleS, _optExtLabelWidth);

                var src = nameInterlocked;
                nameInterlocked = GUILayout.Toggle(nameInterlocked, "名前連動", uiParams.tStyleS, uiParams.optToggleSWidth);
                if (nameInterlocked && src != nameInterlocked) {
                    nameChanged = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("内部パス", uiParams.lStyle, _optLabelWidth);
                trgtMenu.txtpath = GUILayout.TextField(trgtMenu.txtpath, uiParams.textStyle);
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("優先度", uiParams.lStyle, _optLabelWidth);
                var editedPriority = GUILayout.TextField(trgtMenu.priority, 10, uiParams.textStyle, _modalHalfWidth);
                if (trgtMenu.priority != editedPriority ) {
                    // float?
                    int v;
                    if (int.TryParse(editedPriority, out v)) {
                        if (v >= 0) {
                            trgtMenu.priority = v.ToString();
                        }
                    }
                }
                GUILayout.Space(_indentWidth);
                GUILayout.Label("カテゴリ", uiParams.lStyleS);
                GUILayout.Label(trgtMenu.category, uiParams.lStyleS);

                if (GUILayout.Button("↑ パス自動設定", uiParams.bStyle)) {
                    trgtMenu.txtpath = settings.txtPrefixTex + trgtMenu.editfile;
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("アイコン", uiParams.lStyle, _optLabelWidth);
                GUI.enabled = !nameInterlocked;
                if (nameInterlocked && nameChanged) {
                    if (!trgtMenu.editfile.ToLower().EndsWith(settings.iconSuffix, StringComparison.Ordinal)) {
                        trgtMenu.editicon = trgtMenu.editfile + settings.iconSuffix;
                    } else {
                        trgtMenu.editicon = trgtMenu.editfile;
                    }
                }
                if (trgtMenu.editfileExist) uiParams.textStyle.normal.textColor = errColr;
                trgtMenu.editicon = GUILayout.TextField(trgtMenu.editicon, uiParams.textStyle);
                if (trgtMenu.editfileExist) uiParams.textStyle.normal.textColor = txtColr;
                GUI.enabled = true;

                GUILayout.Label(FileConst.EXT_TEXTURE, uiParams.lStyleS, _optExtLabelWidth);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("名前", uiParams.lStyle, _optLabelWidth);
                trgtMenu.name = GUILayout.TextField(trgtMenu.name, uiParams.textStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(_optTwoLineHeight);
                GUILayout.Label("説明", uiParams.lStyle, _optLabelWidth);
                trgtMenu.desc = GUILayout.TextArea(trgtMenu.desc, uiParams.textAreaStyleS, _optTwoLineHeight);
                GUILayout.EndHorizontal();

                try {
                    foreach (var pair in trgtMenu.slotMaterials) {
                        GUILayout.Label("マテリアル情報 (" + pair.Key + ")", uiParams.lStyle);
                        foreach (var trgtMat in pair.Value.materials) {
                            if (trgtMat == null) continue;

                            GUILayout.BeginHorizontal();
                            try {
                                GUILayout.Space(_indentWidth);
                                if (trgtMat.onlyModel) uiParams.lStyleS.normal.textColor = Color.cyan;
                                GUILayout.Label("マテリアル" + trgtMat.matNo, uiParams.lStyleS, _optSubLabelWidth);
                                if (trgtMat.onlyModel) uiParams.lStyleS.normal.textColor = txtColr;
    
                                // マテリアルファイル名
                                if (trgtMat.onlyModel) {
                                    GUILayout.Label("(.mateファイル無し)", uiParams.lStyleS);
                                } else {
                                    if (trgtMat.editfileExist) uiParams.textStyle.normal.textColor = errColr;
                                    GUI.enabled = !nameInterlocked;
                                    if (nameInterlocked && nameChanged) {
                                        trgtMat.editfile = trgtMenu.editfile + trgtMat.matNo;
                                    }
                                    trgtMat.editfile = GUILayout.TextField(trgtMat.editfile, uiParams.textStyle);
                                    if (trgtMat.editfileExist) uiParams.textStyle.normal.textColor = txtColr;
                                    GUI.enabled = true;
                                    GUILayout.Label(FileConst.EXT_MATERIAL, uiParams.lStyleS, _optExtLabelWidth);
                                    if (trgtMat.needPmat && trgtMat.needPmatChange) {
                                        GUILayout.Label("|"+FileConst.EXT_PMAT, uiParams.lStyleS, _optExtLabelWidth);
                                    }
                                }
    
                                // マテリアル名
                                if (!trgtMat.needPmat) {
                                    // 名前は任意
                                    trgtMat.editname = GUILayout.TextField(trgtMat.editname, uiParams.textStyle);    
                                } else {
                                    if (trgtMat.needPmatChange) {
                                        // pmat出力する場合
                                        trgtMat.editname = trgtMat.editfile;
                                        trgtMat.editname = GUILayout.TextField(trgtMat.editname, uiParams.textStyle);
                                    } else {
                                        GUI.enabled = false;
                                        trgtMat.editname = GUILayout.TextField(trgtMat.editname, uiParams.textStyle);
                                        GUI.enabled = true;
                                    }
                                }

                            } catch(Exception e) {
                                LogUtil.Debug("failed to display material name:", trgtMat.editname,  e);
                            } finally {
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            try {
                                GUILayout.Space(_indentWidth*2);
                                var blabel = trgtMat.uiTexViewed? "－": "＋";
                                if (GUILayout.Button(blabel, uiParams.optBtnWidth)) {
                                    trgtMat.uiTexViewed = !trgtMat.uiTexViewed;
                                }
                                var shaderName = trgtMat.ShaderNameOrDefault("不明");
                                
                                GUILayout.Label("シェーダ : " + shaderName, uiParams.lStyleS, _optShaderWidth);
                                GUILayout.Space(_indentWidth);
                                uiParams.lStyleS.normal.textColor = changedColor;
                                GUILayout.Label(trgtMat.shaderChanged? "変更有":"" , uiParams.lStyleS);
                                uiParams.lStyleS.normal.textColor = txtColr;
                                // TODO pmat出力の有無指定
                                if (!trgtMat.needPmat) {
                                    GUILayout.Label("pmat不要(透過無)", uiParams.lStyleS, _optLabelWidth);
                                } else {
                                    if (trgtMat.needPmatChange) {                                        
                                        GUI.enabled = false;
                                        trgtMat.pmatExport = GUILayout.Toggle(trgtMat.pmatExport, "pmat出力", uiParams.lStyleS, _optLabelWidth);
                                        GUI.enabled = true;
                                    } else {
                                        GUILayout.Label("既存pmat利用", uiParams.lStyleS, _optLabelWidth);
                                    }
                                }
                            } catch(Exception e) {
                                LogUtil.Debug("failed to display shader info:", trgtMat.editname, e);
                            } finally {
                                GUILayout.EndHorizontal();
                            }

                            if (trgtMat.uiTexViewed) {
                                GUILayout.BeginVertical();
                                try {
                                    // 現在のマテリアルからテクスチャ取得
                                    // var mat = trgtMat.editedMat.material;
                                    //foreach (var propName in trgtMat.editedMat.type.texPropNames) {
                                    foreach (var texProp in trgtMat.editedMat.type.texProps) {
                                        
                                        TargetTexture trgtTex;
                                        if (!trgtMat.texDic.TryGetValue(texProp.key, out trgtTex)) {
                                            continue;
                                        }
                                        if (trgtTex.tex == null) continue;

                                        GUILayout.BeginHorizontal(_optSubItemHeight);
                                        GUILayout.Space(_indentWidth*4);
                                        var propName = texProp.keyName;
                                        GUILayout.Label(propName, uiParams.lStyleS, _optPropNameWidth);
                                        if (trgtTex.needOutput) {
                                            GUI.enabled = !nameInterlocked;
                                            if (nameInterlocked && nameChanged) {
                                                trgtTex.editname = trgtMat.editfile + FileConst.GetTexSuffix(propName);
                                            }
                                        } else {
                                            GUI.enabled = false;
                                        }
                                        if (!trgtMat.onlyModel) {
                                            if (trgtTex.editnameExist) uiParams.textStyle.normal.textColor = errColr;
                                            trgtTex.editname = GUILayout.TextField(trgtTex.editname, uiParams.textStyle);
                                            if (trgtTex.editnameExist) uiParams.textStyle.normal.textColor = txtColr;
                                            GUI.enabled = true;

                                            if (trgtTex.colorChanged) {
                                                uiParams.lStyleS.normal.textColor = changedColor;
                                                GUILayout.Label("色変更", uiParams.lStyleS, _optExtLabelWidth);
                                                uiParams.lStyleS.normal.textColor = txtColr;
                                            } else {
                                                if (trgtTex.fileChanged) {
                                                    uiParams.lStyleS.normal.textColor = changedColor;
                                                    GUILayout.Label("変更有" , uiParams.lStyleS, _optExtLabelWidth);
                                                    uiParams.lStyleS.normal.textColor = txtColr;
                                                }
                                            }
                                            // texは色変更があった場合にのみ出力

                                        } else {
                                            // modelファイルのマテリアルを使用している場合
                                            if (trgtTex.editnameExist) uiParams.lStyle.normal.textColor = errColr;
                                            GUILayout.Label(trgtTex.editname, uiParams.lStyleS);
                                            if (trgtTex.editnameExist) uiParams.lStyle.normal.textColor = txtColr;
                                        }
                                        GUI.enabled = true;
                                        
                                        GUILayout.EndHorizontal();
                                    }
                                } catch(Exception e) {
                                    LogUtil.Debug("failed to display tex info.", e);
                                } finally {
                                    GUI.enabled = true;
                                    GUILayout.EndVertical();
                                }
                            } else if (nameInterlocked && nameChanged) {
                                // 非表示でもデータは更新
                                //foreach (var propName in trgtMat.editedMat.type1.texPropNames) {
                                foreach (var texProp in trgtMat.editedMat.type.texProps) {
                                    TargetTexture trgtTex;
                                    if (!trgtMat.texDic.TryGetValue(texProp.key, out trgtTex)) continue;
                                    if (trgtTex.tex == null) continue;

                                    if (trgtTex.needOutput) {
                                        trgtTex.editname = trgtMat.editfile + FileConst.GetTexSuffix(texProp.keyName);
                                    }
                                }
                            }
                            // GUILayout.EndHorizontal();
                        }
                    }
                } catch(Exception e) {
                    LogUtil.Error("failed to display material", e);
                }

                if (trgtMenu.addItems.Any()) {
                    GUILayout.Label("additem (model)", uiParams.lStyle);
                    foreach (var item in trgtMenu.addItems) {
    
                        try {
                            if (!item.HasSlot()) continue;
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(_indentWidth);
                            GUILayout.Label(item.slot, uiParams.lStyleS, _optLabelWidth);

                            if (item.editnameExist) {
                                uiParams.textStyle.normal.textColor = errColr;
                                uiParams.lStyleS.normal.textColor   = errColr;
                            }
                            // 同一ファイル名のモデルを参照するリンクがあった場合は変更できないラベルとする
                            if (item.HasLink()) {
                                GUILayout.Label(item.link.EditFileName(), uiParams.lStyleS);
                            } else if (!item.needUpdate) {
                                GUILayout.Label(item.EditFileName(), uiParams.lStyleS);
                            } else {

                                GUI.enabled = !nameInterlocked;
                                if (nameInterlocked && nameChanged) {
                                    var suffix = FileConst.GetModelSuffix(item.slot);
                                    if (trgtMenu.editfile.Contains(suffix)) {
                                        item.editname = trgtMenu.editfile ;
                                    } else {
                                        item.editname = trgtMenu.editfile + suffix;
                                    }
                                }
                                item.editname = GUILayout.TextField(item.editname, uiParams.textStyle);
                                GUI.enabled = true;

                                GUILayout.Label(FileConst.EXT_MODEL, uiParams.lStyleS, _optExtLabelWidth);
                            }
                            if (item.editnameExist) {
                                uiParams.textStyle.normal.textColor = txtColr;
                                uiParams.lStyleS.normal.textColor   = txtColr;
                            }
                            GUILayout.EndHorizontal();

                            if (item.info.Length < 3) continue;
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(_indentWidth);
                            GUILayout.Label("追加情報", uiParams.lStyleS, _optLabelWidth);
                            var sb = new StringBuilder();
                            for (var i=2; i< item.info.Length; i++) {
                                sb.Append(item.info[i]).Append(", ");
                            }
                            GUILayout.Label(sb.ToString(), uiParams.lStyleS);
                            GUILayout.EndHorizontal();
                        } catch(Exception e) {
                            LogUtil.Debug("failed to display item info:", item.slot, e);
                        }
                    }
                }
                if (trgtMenu.resources.Any()) {
                    try {
                        GUILayout.Label("リソース参照", uiParams.lStyle);
                        foreach (var resRef in trgtMenu.resources) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(_indentWidth);
                            GUILayout.Label(resRef.key, uiParams.lStyleS, _optSubLabelWidth);
                            if (nameInterlocked && nameChanged) {
                                resRef.editname = trgtMenu.editfile + resRef.suffix;
                            }
                            GUI.enabled = !nameInterlocked;
                            if (resRef.editfileExist) uiParams.textStyle.normal.textColor = errColr;
                            resRef.editname = GUILayout.TextField(resRef.editname, uiParams.textStyle);
                            if (resRef.editfileExist) uiParams.textStyle.normal.textColor = txtColr;
                            GUI.enabled = true;
                            GUILayout.Label(FileConst.EXT_MENU, uiParams.lStyleS, _optExtLabelWidth);
                            GUILayout.EndHorizontal();
                        }

                    } catch(Exception e) {
                        LogUtil.Debug("failed to display resource info.", e);
                    }
                }

                nameChanged = false;
            } finally {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }

            
            GUILayout.BeginHorizontal();
            GUILayout.Space(uiParams.marginL);
            ignoreExist = !GUILayout.Toggle(!ignoreExist, "登録済確認", uiParams.tStyleS, uiParams.optToggleSWidth);
            if (GUILayout.Button("保存", uiParams.bStyle)) {
                if (IsWritable(trgtMenu, ignoreExist)) {
                    if (SaveFiles(trgtMenu)) {
                        var logmsg = "エクスポートが完了しました。出力先=" +  fileUtil.GetACCDirectory(trgtMenu.editfile);
                        NUty.WinMessageBox(NUty.GetWindowHandle(), logmsg, "情報", NUty.MSGBOX.MB_OK);
                        //showDialog = false;
                    }
                } else {
                    const string logmsg = "出力ファイルが登録済みか重複が存在するため、保存処理を行いませんでした。";
                    NUty.WinMessageBox(NUty.GetWindowHandle(), logmsg, "エラー", NUty.MSGBOX.MB_OK);
                }
            }
            if (GUILayout.Button("閉じる", uiParams.bStyle)) {
                showDialog = false;
            }
            GUILayout.Space(uiParams.marginL);
            GUILayout.EndHorizontal();
        }

        // TODO ファイル名となりうるフィールドの文字列検証（入力フィールドの変更時に行う）
        //private bool isSavable;
        // private void CheckFileName(string filename) {
        //     isSavable &= filename.IndexOfAny(INVALID_FILENAMECHARS) < 0;
        //}

        // ファイルの重複確認
        private bool IsWritable(ACCMenu menu, bool ignoreExists) {
            if (menu.editfile.Length == 0) return false;

            var registed = false;

            var outDir = fileUtil.GetACCDirectory();
            outDir = Path.Combine(outDir, trgtMenu.editfile);
            if (!ignoreExists && Directory.Exists(outDir)) {
                LogUtil.Debug("output directory already exist :", outDir);
                menu.editfileExist = true;
                return false;
            }
            
            var hasDuplicate = false;
            // 別々の情報ファイルが重複しないか + 登録済みファイルが存在するかをチェック 
            var writeFiles = new HashSet<string> ();

            var menufile = menu.EditFileName();
            writeFiles.Add(menufile);
            menu.editfileExist = false;
            if (fileUtil.Exists(menufile)) {
                LogUtil.Debug("already exist:", menufile);
                registed = true;
                menu.editfileExist = true;
            }

            // icon
            menu.editiconExist = false;
            var iconfilepath = menu.EditIconFileName();
            if (fileUtil.Exists(iconfilepath)) {
                LogUtil.Debug("already exist:", iconfilepath);
                registed = true;
                menu.editiconExist = true;
            }
            writeFiles.Add(iconfilepath);

            // modelファイル
            foreach (var pair in menu.itemFiles) {
                var item = pair.Value;
                if (!item.needUpdate) continue;
                item.editnameExist = false; // clear
                var filename = item.EditFileName();
                if (HasAlreadyWritten(writeFiles, filename)) {
                    hasDuplicate = true;
                    item.editnameExist = true; // フラグを分けるべきか？
                    continue;
                }

                if (!fileUtil.Exists(filename)) continue;
                LogUtil.Debug("already exist:", filename);
                registed = true;
                item.editnameExist = true;
            }
            // mateファイル
            foreach (var tm in menu.slotMaterials.Values) {
                foreach(var trgtMat in tm.materials) {
                    // modelファイルのみの場合は好み ⇒変更する場合はmenuにマテリアル変更を追加する必要あり
                    if (trgtMat.onlyModel) continue;

                    trgtMat.editfileExist = false;
                    var filename = trgtMat.EditFileName();
                    if (HasAlreadyWritten(writeFiles, filename)) {
                        hasDuplicate = true;
                        trgtMat.editfileExist = true; // フラグを分けるべきか？
                        continue;                        
                    }
                    if (fileUtil.Exists(filename)) {
                        LogUtil.Debug("already exist:", filename);
                        registed = true;
                        trgtMat.editfileExist = true;
                    }
                    // pmat
                    if (trgtMat.needPmatChange) {
                        var name = Path.GetFileNameWithoutExtension(filename);
                        var pmatfile = name + FileConst.EXT_PMAT;
                        if (HasAlreadyWritten(writeFiles, pmatfile)) {
                            hasDuplicate = true;
                            trgtMat.editfileExist = true; // フラグを分けるべきか？
                            continue;                        
                        }
                        if (fileUtil.Exists(pmatfile)) {
                            LogUtil.Debug("already exist:", pmatfile);
                            registed = true;
                            trgtMat.editfileExist = true;
                        }
                    }

                    // texファイル
                    foreach (var trgtTex in trgtMat.texDic.Values) {
                        if (!trgtTex.needOutput) continue;
                        trgtTex.editnameExist = false;

                        var texfilename = trgtTex.EditFileName();
                        if (HasAlreadyWritten(writeFiles, texfilename)) {
                            hasDuplicate = true;
                            trgtTex.editnameExist = true; // フラグを分けるべきか？
                            continue;                        
                        }

                        if (!fileUtil.Exists(texfilename)) continue;
                        LogUtil.Debug("already exist:", texfilename);
                        registed = true;
                        trgtTex.editnameExist = true;
                    }
                }
            }

            // めくれ、ずらし用menuファイルの出力
            foreach (var res in menu.resFiles.Values) {

                res.editfileExist = false;
                var filename = res.EditFileName();
                if (HasAlreadyWritten(writeFiles, filename)) {
                    hasDuplicate = true;
                    res.editfileExist = true; // フラグを分けるべきか？
                    continue;                        
                }

                if (!fileUtil.Exists(filename)) continue;
                LogUtil.Debug("already exist:", filename);
                registed = true;
                res.editfileExist = true;
            }
            // 出力ファイルの重複は無視できない
            if (hasDuplicate) return false;
            return ignoreExists || !registed;
        }

        public bool SaveFiles(ACCMenu menu) {
            var outDir = fileUtil.GetACCDirectory(trgtMenu.editfile);
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            LogUtil.Debug("output path:", outDir);
            var filepath = Path.Combine(outDir, menu.EditFileName());

            // menu出力
            ACCMenu.WriteMenuFile(filepath, menu);

            var writeFiles = new HashSet<string>();

            // filterがあれば、適用しアイコンを変更
            var iconfilepath = Path.Combine(outDir, menu.EditIconFileName());
            writeFiles.Add(iconfilepath);
            var icontxt = settings.txtPrefixTex + iconfilepath;
            fileUtil.CopyTex(menu.icon, iconfilepath, icontxt, null);
            LogUtil.Debug("tex file:", iconfilepath);

            // model 出力 (additem)
            foreach (var pair in menu.itemFiles) {
                var infile = pair.Key;
                var item = pair.Value;
                if (!item.needUpdate) continue;
                var filename = item.EditFileName();
                if (HasAlreadyWritten(writeFiles, filename)) continue;

                var modelfilepath = Path.Combine(outDir, filename);
    
                // modelファイルのスロットのマテリアル/テクスチャ情報を抽出
                var slotMat = menu.slotMaterials[item.slot];
                // onlyModelでない場合はshader変更のみとしておく
                // material slotとmatNoで特定
                // texture  propName
                // 必要に応じてtex出力
                fileUtil.WriteModelFile(infile, modelfilepath, slotMat);
                LogUtil.Debug("model file:", modelfilepath);
            }

            // mate出力
            foreach (var tm in menu.slotMaterials.Values) {
                foreach(var trgtMat in tm.materials) {
                    // マテリアル変更が指定された場合 => TODO 切り替え可能とする場合：menuにマテリアル変更を追加する必要あり
                    if (!trgtMat.onlyModel) {
                        var filename = trgtMat.EditFileName();
                        if (HasAlreadyWritten(writeFiles, filename)) continue;
    
                        var matefilepath = Path.Combine(outDir, filename);
                        fileUtil.WriteMateFile(trgtMat.filename, matefilepath, trgtMat);
                        LogUtil.Debug("mate file:", matefilepath);

                        if (trgtMat.needPmatChange) {
                            var name = Path.GetFileNameWithoutExtension(filename);
                            var pmatfile = name + FileConst.EXT_PMAT;
                            var pmatfilepath = Path.Combine(outDir, pmatfile);
                            fileUtil.WritePmat(pmatfilepath, trgtMat.editname, 
                                              trgtMat.RenderQueue(), trgtMat.ShaderName());
                            LogUtil.Debug("pmat file:", pmatfilepath);
                        }
                    }

                    // テクスチャ出力
                    foreach (var trgtTex in trgtMat.texDic.Values) {
                        if (!trgtTex.needOutput) continue;

                        var tex2D = trgtTex.tex as Texture2D;
                        if (tex2D == null) {
                            LogUtil.Debug("tex is not Texture2D", trgtTex.editname);
                            continue;
                            // TODO RenderTexの場合は無理やりTexture2Dに変換も可能だが…
                        }
                        var texfilename = trgtTex.EditFileName();
                        if (HasAlreadyWritten(writeFiles, texfilename)) continue;
                            
                        var texfilepath = Path.Combine(outDir, texfilename);
                        fileUtil.WriteTexFile(texfilepath, trgtTex.EditTxtPath(), tex2D.EncodeToPNG());
                        LogUtil.Debug("tex file:", texfilepath);
                    }
                }
            }

            // めくれ、ずらし用menuファイルの出力
            foreach (var res in menu.resFiles.Values) {

                // 各モデルファイルで出力が必要となるファイルについて、
                // 元が同名のファイルを参照している場合でも関係なく出力
                // 設定が違う場合もある上、editnameはすべて別名になるはず
                var filename = res.EditFileName();
                if (HasAlreadyWritten(writeFiles, filename)) continue;


                var menufilepath = Path.Combine(outDir, filename);
                var toCreateFiles = fileUtil.WriteMenuFile(res.filename, menufilepath, res);
                LogUtil.Debug("menu file:", menufilepath);

                foreach (var toCreate in toCreateFiles) {
                    // modelを出力
                    if (toCreate.item != null) {
                        var filename0 = toCreate.replaced;
                        if (HasAlreadyWritten(writeFiles, filename0)) continue;

                        var modelfilepath = Path.Combine(outDir, filename0);
                        var slotMat = menu.slotMaterials[toCreate.item.slot];
                        // TODO リプレースが想定される情報であるかチェック 
                        fileUtil.WriteModelFile(toCreate.source, modelfilepath, slotMat);
                        
                    // .mate出力
                    } else if (toCreate.material != null) {
                        var trgtMat = toCreate.material;
                        var filename0 = toCreate.replaced;
                        if (HasAlreadyWritten(writeFiles, filename0)) continue;

                        // mate出力==別のtexファイルを出力する可能性有り
                        var matefilepath = Path.Combine(outDir, filename0);
                        fileUtil.WriteMateFile(toCreate.source, matefilepath, trgtMat);
                        // マテリアル名は上位と同じにして、同一pmatを使用する
                        //if (trgtMat.needPmatChange) {
                        //    var name = Path.GetFileNameWithoutExtension(filename);
                        //    var pmatfile = name + FileConst.EXT_PMAT;
                        //    string pmatfilepath = Path.Combine(outDir, pmatfile);
                        //    outUtil.WritePmat(pmatfilepath, trgtMat.editname, 
                        //                      trgtMat.RenderQueue(), trgtMat.ShaderName());
                        //    LogUtil.DebugLog("pmatファイルを出力しました。", pmatfilepath);
                        //}

                        foreach (var tex in trgtMat.texDic.Values) {
                            if (!tex.needOutput) continue;

                            var tex2D = tex.tex as Texture2D;
                            if (tex2D == null) {
                                LogUtil.Debug("tex is not 2D", tex.editname);
                                continue;
                            }
                            var texfilename = tex.EditFileName();
                            if (HasAlreadyWritten(writeFiles, texfilename)) continue;

                            // テクスチャをロードし、フィルタを適用
                            Texture2D loadedTex   = null;
                            Texture2D filteredTex = null;
                            if (!tex.fileChanged) { // texファイル変更済の場合はロードされたデータ済みから出力（そのままtex2dを使用)
                                var texfile = tex.workfilename + FileConst.EXT_TEXTURE;
                                if (!fileUtil.Exists(texfile)) {
                                    LogUtil.LogF("リソース参照で使用されているtexファイル({0})が見つかりません。texファイルを出力できません。", texfile);
                                    continue;
                                }
                                loadedTex = TexUtil.Instance.Load(texfile);
                                tex2D = loadedTex;
                            }

                            if (tex.colorChanged) {
                                filteredTex = ACCTexturesView.Filter(tex2D, tex.filter);
                                tex2D = filteredTex;
                            }
                            var texfilepath = Path.Combine(outDir, texfilename);
                            fileUtil.WriteTexFile(texfilepath, tex.EditTxtPath(), tex2D.EncodeToPNG());
                            LogUtil.Debug("tex file:", texfilepath);

                            if (loadedTex != null)   UnityEngine.Object.DestroyImmediate(loadedTex);
                            if (filteredTex != null) UnityEngine.Object.DestroyImmediate(filteredTex);
                        }
                    }
                }
            }
            return true;
        }

        private bool HasAlreadyWritten(ICollection<string> writtenFiles, string filename) {
            if (writtenFiles.Contains(filename)) {
                LogUtil.DebugF("{0} has already been written.", filename);
                return true;
            }
            writtenFiles.Add(filename);
            return false;
        }
    }
}
