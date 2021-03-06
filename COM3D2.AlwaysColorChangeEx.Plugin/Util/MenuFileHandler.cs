using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CM3D2.AlwaysColorChangeEx.Plugin.Util {
    public class MenuFileHandler {

        public List<ChangeInfo> Parse(string filename) {
            if (!GameUty.FileSystem.IsExistentFile(filename)) return null;

            using (var reader = new BinaryReader(FileUtilEx.Instance.GetStream(filename), Encoding.UTF8)) {
                var header = reader.ReadString();
                if (header != "CM3D2_MENU") {
                    throw new Exception("header invalid. " + filename);
                }
                reader.ReadInt32();  // ver
                reader.ReadString(); // txtpath

                reader.ReadString(); // header name
                reader.ReadString(); // header category
                reader.ReadString(); // header desc

                reader.ReadInt32(); // length

                var changeItems = new List<ChangeInfo>();

//                string category;
                var loop = true;
                while (loop) {
                    var size = (int) reader.ReadByte();
                    if (size == 0) break;

                    var key = reader.ReadString();
                    var param = new string[size-1];
                    for (var i = 0; i < size-1; i++) {
                        param[i] = reader.ReadString();
                    }
                    key = key.ToLower();
                    switch(key) {
                        case "end":
                            loop = false;
                            break;
                        case "name":
                            // param[0] name
                            break;
                        case "setumei":
                            // parma[0] desc //γζΉθ‘γ
                            break;
                        case "color_set":
                            // parma[0] MPN (ToLower())
                            // param[1] m_strMenuNameInColorSet  [option](ToLower())
                            break;
                        case "icon":
                        case "icons":
                            // parma[0] file (tex)
                            break;
                        case "priority":
                            // param[0] float
                            break;
                        case "unsetitem":
                            // mi.m_boDelOnly = true
                            break;
                        case "γ‘γγ₯γΌγγ©γ«γ":
                            // param[0] == "man" ToLower()
                            // mi.m_bMan = true
                            break;
                        case "γ’γ€γγ ":
                            // var filename = param[0]; // menu file
                            break;
                        case "γ’γ€γγ ζ‘δ»Ά":
                            // param[0] : slotname

                            // param[1] : γ«δ½γ
                              // param[2] : ζγ/η‘γ
                              // param[3] : γͺγ

                            // param[1] : γ
                              // param[2] : modelγγ‘γ€γ« (ε­ε¨γγͺγγ¨γγγγβ¦οΌ
                              // param[3] : γͺγ
                              // param[4] : menu file

                            // param[1] : γ?γ’γ€γγ γγ©γ‘γΌγΏγ?
                              // param[2] : tag . ToLower()
                              // param[3] : γ
                              // param[4] : value
                              // param[5] : γͺγ
                              // param[6] : menu file
                            break;
                        case "γ’γ€γγ γγ©γ‘γΌγΏ":
                            if (param.Length == 3) {
                                // slot, tag, value
                                // ex) wear  ηΉζ?θ‘£θ£³_γ‘γ€γζ 1
                            }
                            break;
                        case "εθ±γ":
                            // key= εθ±γ
                            // param[0] value (menu file)
                            break;
                        case "γͺγ½γΌγΉεη§":
                            // param[0] key
                            // param[1] value (menu file)
                            break;
                        case "setslotitem":
                            // param[0] tag
                            // param[1] num(uint)
                            // maid.SetProp(tag, num, false)
                            break;
                        case "prop":
                            // param[0] tag
                            // param[1] num(int)
                            // maid.SetProp(tag, num, false)
                            break;
                        case "additem":
                            // param[0] model file
                            // param[1] slot

                            // param[2] γ’γΏγγ
                            // param[3] attachSlot
                            // param[4] attachName

                            // param[2] γγΌγ³γ«γ’γΏγγ
                            // param[3] attachName [option]
                            // slotγhanditemr handitemlγ?γγγγγ§γγγ°
                            // attachSlotγ¨attachNameγθͺεγ§ζ±Ίε?
//                            var modelitem = param[0].ToLower();
//                            var slot = param[1].ToLower();
//                            var model = fileMgr.GetOrAdd(modelitem, TargetExt.model);
//                            menu.AddChild(model, key + " (" + slot + ")");
//
//                            if (slot == "head") {
//                                var baseName = Path.GetFileNameWithoutExtension(model.filename);
//                                CheckSkinTex(baseName, model);
//                            }
//                            // slotγ¨model fileγ?ι’δΏγδΏζ => texγ?Regex *γ?θ§£ζ±Ίγ«ε©η¨
//                            modelDic[slot] = modelitem;

                            Add(changeItems, param[0]);
                            break;
                        case "saveitem":
                            // param[0] logεΊεγ?δ»₯εγ?θ‘
                            break;
                        case "category":
                            // param[0] category
                            //   SceneEditγ§γ―ToLower()γ«ε―Ύγγ¦γMPN.Parseγθ‘γ
//                            category = param[0];
                            break;
                        case "maskitem":
                            // param[0] maskSlot
                            // body.AddMask(category, maskSlot)
                            break;
                        case "delitem":
                            // param[0] slot [option] param[0]γγͺγε ΄εγ―categoryγ?γ’γ€γγ γει€
                            // mi.m_boDelOnly = true(sceneEdit)
                            Add(changeItems, param[0]);
                            break;
                        case "nodeζΆε»":
                        case "nodeθ‘¨η€Ί":
                            // param[0] nodeSlot
                            break;
                        case "γγΌγnodeζΆε»":
                        case "γγΌγnodeθ‘¨η€Ί":
                            // param[0] nodeSlot
                            // param[1] bone name
                            break;
                        case "color":
                            // param[0] name
                            // param[1] matNo
                            // param[2] propName
                            // param[3] color.R (float)
                            // param[4] color.G (float)
                            // param[5] color.B (float)
                            // param[6] color.A (float)
                            break;
                        case "mancolor":
                            // param[0] color.R (float)
                            // param[1] color.G (float)
                            // param[2] color.B (float)
                            break;

                        case "tex":
                        case "γγ―γΉγγ£ε€ζ΄":
                            // param[0] slot
                            // param[1] matNo
                            // param[2] propName
                            // param[3] filename (tex)
                            // param[4] η‘ιθ²ID [option]
                            //  NONE/EYE_L/EYE_R/HAIR/EYE_BROW/UNDER_HAIR/SKIN/NIPPLE/MAX
                            //  NONEζε?γ― param[4]η‘γγ¨εζ§
//                            var texfile = param[3];
//                            if (!texfile.Contains("*")) {
//                                var tex1 = fileMgr.GetOrAdd(texfile, TargetExt.tex);
//                                menu.AddChild(tex1, key);
//                            } else {
//                                regTexes.Add(new RegTex(param[0], texfile, key));
//                                // γγΉγ¦θ΅°ζ»γη΅γγ£γζ?΅ιγ§γ’γγ«γγ‘γ€γ«γ¨γ?ε―ΎεΏδ»γγη’Ίθͺ
//                            }
                            Add(changeItems, param[0], int.Parse(param[1]), param[2]);
                            break;
                        case "γγ―γΉγγ£εζ":
                        case "γγ―γΉγγ£γ»γγεζ":
                            // param[0] slot
                            // param[1] matNo(int)
                            // param[2] propName
                            // param[3] layerNo(int)
                            // param[4] file (tex)
                            // param[5] εζζε?(blendMode)
                            //     Alpha/Multiply/InfinityColor/TexTo8bitTex/Max
//                            var tex = fileMgr.GetOrAdd(param[4], TargetExt.tex);
//                            menu.AddChild(tex, key + " (" + param[0] + "[" + param[1] + "] : " + param[2] + ")");
//                            Add(changeItems, param[0], int.Parse(param[1]), param[2]);

                            break;
                        case "γγγͺγ’γ«ε€ζ΄":
                            // param[0] slot
                            // param[1] matNo(int)
                            // param[2] file (mate)
                            Add(changeItems, param[0], int.Parse(param[1]));

//                            var mate = fileMgr.GetOrAdd(param[2], TargetExt.mate);
//                            menu.AddChild(mate, key);
                            break;
                        case "shader":
                            // param[0] slot
                            // param[1] matNo
                            // param[2] shaderFileName
                            // TODO
                            Add(changeItems, param[0], int.Parse(param[1]));

                            break;
                        case "γ’γΏγγγγ€γ³γγ?θ¨­ε?":
                            // param[0] γ’γΏγγγγ€γ³γε
                            // param[1] vec.x (float)
                            // param[2] vec.y (float)
                            // param[3] vec.z (float)
                            // param[4] q.x (float)
                            // param[5] q.y (float)
                            // param[6] q.z (float)
                            // additemγ§ζε?γγγΉγ­γγγΈγ?γ’γΏγγ
                            break;
                        case "blendset":
                            // param[0] blendsetε
                            // param... γγ¬γ³γγ»γγε
                            break;
                        case "paramset":
                            // param[0] key
                            //
                            // body.Face.NewParamSet("param[0]" "param[1]" ...);
                            break;
                        case "commenttype":
                            // param[0] key
                            // param[1] val
                            break;
                        case "useredit":
                            // param[0] (unused)
                            // param[1] "material" γ§γγγ°γγγͺγ’γ«γγ­γγγ£γθ¨­ε?
                            // param[2] slot
                            // param[3] matNo
                            // param[4] propName
                            // param[5] typeName
                            // param[6] value
                            // body.SetMaterialProperty(category,  slot, mateNo, propName, typeName, value, bool)
                            var slot = param[2];
                            Add(changeItems, slot, int.Parse(param[3]), param[4]);
                            break;
                        case "bonemorph":
                            // param[0] propName
                            // param[1] boneName
                            // param[2] min.x
                            // param[3] min.y
                            // param[4] min.z
                            // param[5] max.x
                            // param[6] max.y
                            // param[7] max.z
                            // body.bonemorph.ChangeMorphPosValue(propName, boneName, vec3Min, vec3Max)
                            break;
                        case "length":
                            // param[0] slot
                            // param[1] groupName
                            // param[2] boneSearchType
                            // param[3] boneName
                            // param[4] min.x
                            // param[5] mix.y
                            // param[6] min.z
                            // param[7] max.x
                            // param[8] max.y
                            // param[9] max.z
                            // body.SetHairLengthDataList(slot, groupName, boneSearchType, boneName, scaleMin, scaleMax)
                            break;
                        case "anime":
                            // param[0] slot
                            // param[1] anm file name
                            // (param[2] "loop" option)
//                            var anim = param[1];
//                            if (!anim.ToLower().EndsWith(".anm")) {
//                                anim += ".anm";
//                            }
//                            var anm = fileMgr.GetOrAdd(anim, TargetExt.anm);
//                            menu.AddChild(anm, key);
                            break;
                        // below: COM GP01?
                        case "param2":
                            // param[1] slot
                            // param[2] tagName
                            // param[3] tagValue
                            break;
                        case "animematerial":
                            // param[0] slot
                            // param[1] mateNo (int)
                            break;
                        case "ver":
                            // param[0] slot (unused)
                            // param[1] ver (int)
                            break;
                        case "if":
                            // param[0] maidprop[... or
                            // param[1] ==
                            // param[2] nothing
                            // param[3] ?
                            // param[4] setprop[...
                            // param[5] =
                            // param[6] getprop[...
                            break;
                        case "set":
                            break;
                        case "nofloory":
                            // param[0] slot

                            break;
                    }
                }
                return changeItems;
            }
        }
        public void Add(List<ChangeInfo> items, string slot, int matNo=-1, string propName=null) {

            foreach (var item in items) {
                if (item.slot == slot) {
                    item.Add(matNo, propName);
                    return;
                }
            }
            items.Add(new ChangeInfo(slot, matNo, propName));
        }

        public class ChangeInfo {
            public string slot;
            public List<MateInfo> matInfos;

            public ChangeInfo(string slot, int matNo=-1, string propName=null) {
                this.slot = slot;
                if (matNo != -1) {
                    matInfos = new List<MateInfo> {new MateInfo(matNo, propName)};
                }
            }

            public void Add(int matNo, string propName = null) {
                if (matInfos == null) return;

                if (matNo == -1) {
                    matInfos = null;
                    return;
                }

                foreach (var mate in matInfos) {
                    if (mate.matNo == matNo) {
                        mate.Add(propName);
                        return;
                    }
                }
                matInfos.Add(new MateInfo(matNo, propName));
            }
        }

        public class MateInfo {
            public int matNo;
            public List<string> propNames;
            public MateInfo(int matNo, string propName = null) {
                this.matNo = matNo;
                if (propName != null) {
                    propNames = new List<string> {propName};
                }
            }

            public void Add(string propName) {
                if (propNames == null) return;

                if (propName == null) {
                    propNames = null;
                } else {
                    if (propNames == null) {
                        propNames = new List<string>();
                    }
                    propNames.Add(propName);
                }
            }

            public override string ToString() {
                var sb = new StringBuilder("matNo=");
                sb.Append(matNo).Append(", propNames=").Append(propNames);
                return sb.ToString();
            }

        }
    }
}
