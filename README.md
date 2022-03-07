CM3D2.AlwaysColorChangeEx.Plugin
---
Visual Studio 2022 Converting version


---

パーツごとに色変更やパラメータ調整を可能とするプラグインです。  
不具合が残っている可能性もありますし、本来設定しない値に設定できてしまう可能性もあります。  
本プラグインの使用は自己責任でお願いします。

オリジナル作者である[kf-cm3d2][] さんとは別人による改造版です。  
[オリジナル][Original]から変更しすぎたため、別プロジェクト化しました。  

---
## ■ 導入
#### ◇前提条件  
UnityInjectorが導入済みであること

#### ◇動作確認環境

| env    | version | plugin version    |
| -------| ------- | ----------------- |
| CM3D2  | 1.58    |  - 0.3.1.3        |
| COM3D2 | 1.21.1  |  - 0.3.1.3        |
|        | 1.23-   | 0.3.1.4-          |
|        | 1.61.1  | 0.3.4.0-          |
|COM3D2_5| 3.2.1   | 0.3.5.0-          |

  - 前提プラグイン：UnityInjector/Sybaris  

プラグインバージョン 
0.3.6.0 
Githubから再フォーク
カスタムオーダーメイド3D2.5 Ver3.2.1対応
名前空間COM3D2> CM3D2を修正しました
0.3.5.0 カスタムオーダーメイド3D2.5 Ver3.2.1対応
0.3.4.0 カスタムオーダーメイド3D2 Ver1.62.1対応、v4.0.30319再コンパイル
0.3.1.4～は COM3D2 1.23以降のみサポートとなります。

#### ◇インストール  

[Releases][]ページから、対応するバージョンのzipファイルをダウンロードし、  
解凍後、UnityInjectorフォルダ以下をプラグインフォルダにコピーしてください。

#### ◇自分でコンパイルする場合  (制限有り)
compile.batをサンプルで用意していますが、現状ではアイコンが配布不可のため配布物と同等のdllは生成できません。  
もしdllを直接生成したい場合は個別に相談ください。  

レジストリが正しく設定されていればそのままコンパイルできます。  
もし、レジストリの状態と異なるCM3D2ディレクトリを使いたい場合は、  
cmpile_base.batファイルの以下の環境変数のパスを設定して、バッチを実行してください。  
* BASE_DIR_=C:\KISS\%NAMEKEY%\  
NAME_KEYはcompile.bat (or compile_cm3d2.bat)内で指定  

## ■ 機能概要
#### ◇対象シーン
 * 日常
 * メイドエディット
 * 男エディット
 * 夜伽
 * ダンス
 * ADVパート
 * イベント回想
 * 撮影モード
 * 他

で動作します。

★ 撮影モードや他プラグインで操作対象のメイドを非表示にした場合、  
  強制的に **メイド選択画面** になります。

##### ◇機能説明（概要）
* **[メイド選択][maid_Select]**  
  操作対象のメイドを選択します。  
  撮影モードや他プラグインで複数メイドを表示した場合に利用してください。
* **[マスク選択][mask_Select]**  
  maskItemで消去されているアイテムを表示させることができます。  
   マスクアイテム選択画面で、各スロットのマスクの有無を指定してください。
  「適用」ボタンで反映します。
* **[表示ノード選択][node_Select]**   
  ノード（身体の部位）の表示・非表示を切り替えられます。  
  「適用」ボタンで反映します。
* **[プリセット保存][preset_Save]**  
  現在の衣装・設定値をプリセットとして保存します。  
  プリセット適用で呼び出すことがます。  
* **[プリセット適用][preset_Apply]**  
  保存したプリセットを選択・適用します。
  適用時に、プリセットから適用する項目を選択できます。
  - マスク
  - ノード表示
  - 身体
  - 衣装
  - 衣装外し  
    これがチェックされている場合、プリセットで衣装を設定していないスロットは衣装を外します  
* **[マテリアル情報変更][mate_Change]**  
  選択されたスロットのマテリアル情報を変更します。  
	スライダーで各パラメータを変更できます。  
* **[menuエクスポート][menu_Export]**  
  現在の選択スロットのアイテムをエクスポートします。  
  関連するめくれやずらしmodelなどにも対応
* **[テクスチャ変更][tex_Change]**  
  テクスチャファイル選択による変更と、スライダーによる色変更が可能です。  
  - ファイル選択によるテクスチャ変更  
    CM3D2のarcに同梱されているtexファイル、あるいはMODに登録されているtexファイルは、    
    テキストフィールドにファイル名を指定する事で適用できます(.tex省略可能)  
    それ以外のファイルは、「...」ボタンから参照してください。  
    「...」ボタンからファイル参照画面が表示されるので、対象ファイルを選択して、「選択」ボタンを押下することでテクスチャが適用されます。  

    ※ texファイルはMODフォルダ以下など、CM3D2から読み込めるパスに存在する必要あり。  
    pngはダイアログで選択できればパスは任意  
    *対応形式* :.tex, .png
  - スライダーによる色変更  
    freeカラー選択時は変更できません。  
   「保存」ボタンでテクスチャ出力に対応(0.0.5.1～)
* **ボーン表示**  
  選択されたスロットのボーンを表示します。(0.2.10.1～)  
    選択されたメイドから、スロットを選択してボーンを表示します。  
    選択済み一体のメイドにしか対応していません。  
    以下の条件を満たすボーンのみ表示されます。  
     * 子ボーンを有する
     * ボーン名が___で始まらない
* **[Texアニメーション][tex_Anime]**
  テクスチャのパラパラアニメ、スクロールアニメ化を行います。 (0.3.2.0～)


## ■ 使用方法
##### ◇メニュー操作
F12キーでメニューが開きます。  
キーを変更したい場合は、
* Config/AlwaysColroChangeEx.ini

のToggleWindowキーで指定してください。  
iniファイルの詳細は[wiki][wiki_ini]を参照

![GUI](http://i.imgur.com/Qyo9FqB.png, "TOP")
### ■ [画面説明][desc_1]

### ■ [既知の問題][issue]

### ■ [TODO][]


## ■ 権利・規約

* オリジナルの作者である[kf-cm3d2][]さんの意向である以下に従ってください。  
(オリジナルの[プロジェクト][Original] READMEに記載)
~~~
ソースの改変・再配布等は自由にしていただいてかまいません。  
むしろやりたいこと・それを超える機能を実装してくれる人歓迎。  
~~~

* CustomJsonWriter.csは、[JsonFx][]のソースを元にしています。  

* また、KISS公式の[利用規約][KISS_Rule]から逸脱した利用はお控えください  

## ■ その他
オリジナルの作者[kf-cm3d2]様とこれまで不具合報告をくださった方々に感謝。  

## ■ 更新履歴
 * [Releases][] を参照
 * プロジェクトリネーム( **0.1.0.0** )以前の履歴は、commit logを参照
 
[maid_Select]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E3%83%A1%E3%82%A4%E3%83%89%E9%81%B8%E6%8A%9E
[mask_Select]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E3%83%9E%E3%82%B9%E3%82%AF%E3%82%A2%E3%82%A4%E3%83%86%E3%83%A0%E9%81%B8%E6%8A%9E
[node_Select]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E8%A1%A8%E7%A4%BA%E3%83%8E%E3%83%BC%E3%83%89%E9%81%B8%E6%8A%9E
[preset_Save]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E3%83%97%E3%83%AA%E3%82%BB%E3%83%83%E3%83%88%E4%BF%9D%E5%AD%98
[preset_Apply]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E3%83%97%E3%83%AA%E3%82%BB%E3%83%83%E3%83%88%E9%81%A9%E7%94%A8
[menu_Export]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#menu%E3%82%A8%E3%82%AF%E3%82%B9%E3%83%9D%E3%83%BC%E3%83%88
[mate_Change]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E3%83%9E%E3%83%86%E3%83%AA%E3%82%A2%E3%83%AB%E6%83%85%E5%A0%B1%E5%A4%89%E6%9B%B4
[tex_Change]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E#%E3%83%86%E3%82%AF%E3%82%B9%E3%83%81%E3%83%A3%E5%A4%89%E6%9B%B4
[tex_Anime]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/TexAnimator
[wiki_ini]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/ini%E3%83%95%E3%82%A1%E3%82%A4%E3%83%AB
[desc_1]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/%E7%94%BB%E9%9D%A2%E8%AA%AC%E6%98%8E
[issue]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/Known-Issue
[TODO]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/wiki/TODO
[KISS_Rule]:http://kisskiss.tv/kiss/diary.php?no=558
[JsonFx]:http://www.jsonfx.net/license/
[Releases]:https://github.com/trzr/CM3D2.AlwaysColorChangeEx.Plugin/releases
[Original]:https://github.com/kf-cm3d2/CM3D2.AlwaysColorChange.Plugin
[kf-cm3d2]:https://github.com/kf-cm3d2
[icon_link]:https://sozai.cman.jp/
