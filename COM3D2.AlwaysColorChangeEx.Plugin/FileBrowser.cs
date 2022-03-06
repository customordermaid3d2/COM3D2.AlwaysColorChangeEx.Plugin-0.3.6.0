﻿using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CM3D2.AlwaysColorChangeEx.Plugin {
    /*
     *   File browser for selecting files or folders at runtime.
     */
    public enum FileBrowserType {
        File,
        Directory
    }

    public class FileBrowser {
        // Called when the user clicks cancel or select
        public delegate void FinishedCallback(string path);
        // Defaults to working directory
        public string CurrentDirectory {
            get {
                return currentDir;
            }
            set {
                SetNewDirectory(value);
                SwitchDirectoryNow();
            }
        }
        protected string currentDir;
        // Optional pattern for filtering selectable files/folders. See:
        // http://msdn.microsoft.com/en-us/library/wz42302f(v=VS.90).aspx
        // and
        // http://msdn.microsoft.com/en-us/library/6ff71z1w(v=VS.90).aspx
        public string[] SelectionPatterns {
            get {
                return filePatterns;
            }
            set {
                filePatterns = value;
                ReadDirectoryContents();
            }
        }
        protected string[] filePatterns;

        // Optional image for directories
        public Texture2D DirectoryImage { get; set; }
        // Optional image for files
        public Texture2D FileImage { get; set; }
        public Texture2D NoFileImage { get; set; }
        public GUIStyle labelStyle = new GUIStyle("Label");

        // Browser type. Defaults to File, but can be set to Folder
        public FileBrowserType BrowserType {
            get {
                return browserType;
            }
            set {
                browserType = value;
                ReadDirectoryContents();
            }
        }
        protected FileBrowserType browserType;
        protected string newDir;
        protected string[] currentDirParts;

        protected string[] files;
        protected GUIContent[] filesWithImages;
        protected int selectedFile;
        protected string selectedName = string.Empty;

        protected string[] nonMatchingFiles;
        protected GUIContent[] nonMatchingFilesWithImages;
        protected int selectedNonMatchingDirs;

        protected string[] directories;
        protected GUIContent[] dirsWithImages;
        protected int selectedDir;

        protected string[] nonMatchingDirs;
        protected GUIContent[] nonMatchingDirsWithImages;

        protected bool currentDirMatches;

        protected GUIStyle CentredText {
            get {
                return centredText ?? (centredText = new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleLeft,
                    fixedHeight = GUI.skin.button.fixedHeight
                });
            }
        }
        protected GUIStyle centredText;

        protected Rect screenRect;
        protected Vector2 scrollPosition;
        protected readonly FinishedCallback callback;
        public readonly string name;
        public int historySize = 20;

        // Browsers need at least a rect, name and callback
        public FileBrowser(Rect screenRect, string name, FinishedCallback callback) {
            this.name = name;
            this.screenRect = screenRect;
            browserType = FileBrowserType.File;
            this.callback = callback;
            SetNewDirectory(Directory.GetCurrentDirectory());
            SwitchDirectoryNow();
        }

        protected void SetNewDirectory(string directory) {
            newDir = directory;
            nextDirs.Clear();
        }

        protected void SwitchDirectoryNow() {
            if (newDir == null || currentDir == newDir) return;

            if (Directory.Exists(currentDir)) {
                historyDirs.AddLast(currentDir);
                if (historyDirs.Count > historySize) {
                    historyDirs.RemoveFirst();
                }
            }

            currentDir = newDir;
            reloadDir();
        }

        private void reloadDir() {
            scrollPosition = Vector2.zero;
            selectedDir = selectedNonMatchingDirs = selectedFile = -1;
            //selectedName = string.Empty;
            ReadDirectoryContents();
        }

        public void Prev() {
            if (!historyDirs.Any()) return;

            nextDirs.AddFirst(currentDir);
            var dir = historyDirs.Last();
            historyDirs.RemoveLast();

            currentDir = dir;
            reloadDir();
        }

        public void Next() {
            if (!nextDirs.Any()) return;

            historyDirs.AddLast(currentDir);
            var dir = nextDirs.First();
            nextDirs.RemoveFirst();

            currentDir = dir;
            reloadDir();
        }

        private readonly LinkedList<string> historyDirs = new LinkedList<string>();
        private readonly LinkedList<string> nextDirs = new LinkedList<string>();

        protected void ReadDirectoryContents() {
            if (currentDir == "/") {
                currentDirParts = new[] { string.Empty };
                currentDirMatches = false;

            } else {
                currentDirParts = currentDir.Split(Path.DirectorySeparatorChar);
                if (SelectionPatterns != null) {
                    currentDirMatches = false;
                    foreach (var pattern in SelectionPatterns) {
                        var dirName = Path.GetDirectoryName(currentDir);
                        if (dirName == null) continue;

                        var generated = Directory.GetDirectories(dirName, pattern);
                        currentDirMatches = Array.IndexOf(generated, currentDir) >= 0;
                        if (currentDirMatches) break;
                    }
                } else {
                    currentDirMatches = false;
                }
            }

            if (BrowserType == FileBrowserType.File || SelectionPatterns == null) {
                directories = Directory.GetDirectories(currentDir);
                nonMatchingDirs = new string[0];
            } else {
                var list = new List<string>();
                foreach (var pattern in SelectionPatterns) {
                    var arr = Directory.GetDirectories(currentDir, pattern);
                    list.AddRange(arr);
                }
                directories = list.ToArray();
                nonMatchingDirs = Directory.GetDirectories(currentDir)
                        .Where(subDir => Array.IndexOf(directories, subDir) < 0)
                        .ToArray();
                for (var i = 0; i < nonMatchingDirs.Length; ++i) {
                    var lastSeparator = nonMatchingDirs[i].LastIndexOf(Path.DirectorySeparatorChar);
                    nonMatchingDirs[i] = nonMatchingDirs[i].Substring(lastSeparator + 1);
                }
                Array.Sort(nonMatchingDirs);
            }

            for (var i = 0; i < directories.Length; ++i) {
                directories[i] = directories[i].Substring(directories[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }

            if (BrowserType == FileBrowserType.Directory || SelectionPatterns == null) {
                files = Directory.GetFiles(currentDir);
                nonMatchingFiles = new string[0];

            } else {
                if (SelectionPatterns != null) {
                    var list = new List<string>();
                    foreach (var pattern in SelectionPatterns) {
                        var fileList = Directory.GetFiles(currentDir, pattern);
                        if (fileList.Length > 0) {
                            list.AddRange(fileList);
                        }
                    }
                    files = list.ToArray();
                } else {
                    files = new string[0];
                }

                nonMatchingFiles = Directory.GetFiles(currentDir)
                        .Where(filePath => Array.IndexOf(files, filePath) < 0)
                        .ToArray();
                for (var i = 0; i < nonMatchingFiles.Length; ++i) {
                    nonMatchingFiles[i] = Path.GetFileName(nonMatchingFiles[i]);
                }
                Array.Sort(nonMatchingFiles);
            }

            for (var i = 0; i < files.Length; ++i) {
                files[i] = Path.GetFileName(files[i]);
            }
            Array.Sort(files);
            BuildContent();
            newDir = null;
        }

        protected void BuildContent() {
            dirsWithImages = new GUIContent[directories.Length];
            for (var i = 0; i < dirsWithImages.Length; ++i) {
                dirsWithImages[i] = new GUIContent(directories[i], DirectoryImage);
            }
            nonMatchingDirsWithImages = new GUIContent[nonMatchingDirs.Length];
            for (var i = 0; i < nonMatchingDirsWithImages.Length; ++i) {
                nonMatchingDirsWithImages[i] = new GUIContent(nonMatchingDirs[i], DirectoryImage);
            }

            filesWithImages = new GUIContent[files.Length];
            for (var i = 0; i < filesWithImages.Length; ++i) {
                filesWithImages[i] = new GUIContent(files[i], FileImage);
            }

            nonMatchingFilesWithImages = new GUIContent[nonMatchingFiles.Length];
            for (var i = 0; i < nonMatchingFilesWithImages.Length; ++i) {
                nonMatchingFilesWithImages[i] = new GUIContent(nonMatchingFiles[i], NoFileImage);
            }
        }

        public void Update() {
            SwitchDirectoryNow();
        }

        public void OnGUI() {
            GUILayout.BeginArea(screenRect, name, GUI.skin.window);
            try {
                GUILayout.BeginHorizontal();
                try {
                    for (var parentIdx = 0; parentIdx < currentDirParts.Length; ++parentIdx) {
                        if (parentIdx == currentDirParts.Length - 1) {
                            GUILayout.Label(currentDirParts[parentIdx], CentredText);

                        } else if (GUILayout.Button(currentDirParts[parentIdx])) {
                            var parentDirName = currentDir;
                            for (var i = currentDirParts.Length - 1; i > parentIdx; --i) {
                                parentDirName = Path.GetDirectoryName(parentDirName);
                            }
                            SetNewDirectory(parentDirName);
                        }
                    }

                    GUILayout.FlexibleSpace();
                    GUI.enabled = historyDirs.Any();
                    if (GUILayout.Button("<")) {
                        Prev();
                    }
                    GUI.enabled = nextDirs.Any();
                    if (GUILayout.Button(">")) {
                        Next();
                    }

                } finally {
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, 
                                                             GUI.skin.horizontalScrollbar, 
                                                             GUI.skin.verticalScrollbar, GUI.skin.box );
                selectedDir = GUILayoutx.SelectionList(selectedDir, dirsWithImages, labelStyle, DirectoryClickCallback);
                if (selectedDir > -1) {
                    selectedFile = selectedNonMatchingDirs = -1;
                    //selectedName = dirsWithImages[selectedDir].text;
                }

                selectedNonMatchingDirs = GUILayoutx.SelectionList(selectedNonMatchingDirs, nonMatchingDirsWithImages, labelStyle, NonMatchingDirectoryClickCallback);
                if (selectedNonMatchingDirs > -1) {
                    selectedDir = selectedFile = -1;
                    //selectedName = string.Empty;
                }
                GUI.enabled = BrowserType == FileBrowserType.File;
                selectedFile = GUILayoutx.SelectionList(selectedFile, filesWithImages, labelStyle, FileClickCallback);
                GUI.enabled = true;
                if (selectedFile > -1) {
                    selectedDir = selectedNonMatchingDirs = -1;
                    //selectedName = filesWithImages[selectedDir].text;
                }
                GUI.enabled = false;
                GUILayoutx.SelectionList( -1, nonMatchingFilesWithImages, labelStyle );
                GUI.enabled = true;

                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal();
                GUILayout.Label(selectedName);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("キャンセル", GUILayout.Width(120))) {
                    callback(null);
                }
                if (BrowserType == FileBrowserType.File) {
                    GUI.enabled = selectedFile > -1;
                    selectedName = GUI.enabled ? filesWithImages[selectedFile].text : string.Empty;
                } else {
                    if (SelectionPatterns == null) {
                        GUI.enabled = selectedDir > -1;
                        selectedName = GUI.enabled ? dirsWithImages[selectedDir].text : string.Empty;
                    } else {
                        GUI.enabled = selectedDir > -1 ||
                            ( currentDirMatches && selectedNonMatchingDirs == -1 && selectedFile == -1 );
                        selectedName = selectedDir > -1 ? dirsWithImages[selectedDir].text : string.Empty;
                    }
                }

                if (GUILayout.Button("選択", GUILayout.Width(120))) {
                    if (BrowserType == FileBrowserType.File) {
                        callback(Path.Combine(currentDir, files[selectedFile]));
                    } else {
                        callback(selectedDir > -1 ? Path.Combine(currentDir, directories[selectedDir]) : currentDir);
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            } finally {
                GUILayout.EndArea();
            }
        }

        protected void FileClickCallback(int i) {
            if (BrowserType == FileBrowserType.File) {
                //            m_callback(Path.Combine(m_currentDirectory, m_files[i]));
            }
        }

        protected void DirectoryClickCallback(int i) {
            SetNewDirectory(Path.Combine(currentDir, directories[i]));
        }

        protected void NonMatchingDirectoryClickCallback(int i) {
            SetNewDirectory(Path.Combine(currentDir, nonMatchingDirs[i]));
        }
    }

    public static class GUILayoutx {
        public delegate void ClickCallback(int index);

        public static int SelectionList(int selected, GUIContent[] list, GUIStyle elementStyle, ClickCallback callback = null) {
            for (var i = 0; i < list.Length; ++i) {
                var elementRect = GUILayoutUtility.GetRect(list[i], elementStyle);
                var hover = elementRect.Contains(Event.current.mousePosition); 
                if (hover && Event.current.type == EventType.MouseDown) {
                    selected = i;
                    if (callback != null) callback(i);
                    Event.current.Use();

                } else if (Event.current.type == EventType.repaint) {
                    elementStyle.Draw(elementRect, list[i], hover, false, i == selected, false);
                }
            }
            return selected;
        }

        public static int SelectionList(int selected, string[] list, GUIStyle elementStyle, ClickCallback callback=null) {
            elementStyle.active.textColor = new Color(0.8f, 1f, 1f);
            for (var i = 0; i < list.Length; ++i) {
                var elementRect = GUILayoutUtility.GetRect(new GUIContent(list[i]), elementStyle);
                var hover = elementRect.Contains(Event.current.mousePosition);
                if (hover && Event.current.type == EventType.MouseDown) {
                    selected = i;
                    if (callback != null) callback(i);
                    Event.current.Use();
                } else if (Event.current.type == EventType.repaint) {
                    elementStyle.Draw(elementRect, list[i], hover, false, i == selected, false);
                }
            }
            return selected;
        }

    }
}