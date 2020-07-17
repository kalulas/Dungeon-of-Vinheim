using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AClockworkBerry {

    public class ScreenLogger : MonoBehaviour {
        public static bool IsPersistent = true;

        private static ScreenLogger instance;
        private static bool instantiated = false;

        private class LogMessage {
            public string Message;
            public LogType Type;

            public LogMessage(string msg, LogType type) {
                Message = msg;
                Type = type;
            }
        }

        public enum LogAnchor {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        public bool SaveLocal = false;
        public bool ShowLog = true;
        public bool ShowInEditor = true;

        [Tooltip("Height of the log area as a percentage of the screen height")]
        [Range(0.3f, 1.0f)]
        public float Height = 0.5f;

        [Tooltip("Width of the log area as a percentage of the screen width")]
        [Range(0.3f, 1.0f)]
        public float Width = 0.5f;

        public int Margin = 20;

        public LogAnchor AnchorPosition = LogAnchor.BottomLeft;

        public int FontSize = 14;

        [Range(0f, 01f)]
        public float BackgroundOpacity = 0.6f;
        public Color BackgroundColor = Color.black;

        public bool LogMessages = true;
        public bool LogWarnings = true;
        public bool LogErrors = true;

        public Color MessageColor = Color.white;
        public Color WarningColor = Color.yellow;
        public Color ErrorColor = new Color(1, 0.5f, 0.5f);

        public bool StackTraceMessages = false;
        public bool StackTraceWarnings = false;
        public bool StackTraceErrors = true;
        private static Queue<LogMessage> queue = new Queue<LogMessage>();
        private GUIStyle styleContainer, styleText;
        private int padding = 5;

        private bool destroying = false;
        private bool styleChanged = true;

        #region LocalLogWriter

        private static FileStream FileWriter;
        private static UTF8Encoding encoding;

        #endregion

        public static ScreenLogger Instance {
            get {
                if (!instantiated) {
                    CreateInstance();
                }
                return instance;
            }
        }


        private void OnDestroy() //关闭写入
        {
            if (FileWriter != null) {
                FileWriter.Close();
            }
        }

        public static void CreateInstance() {
            if (instantiated) {
                return;
            }
            instance = GameObject.FindObjectOfType(typeof(ScreenLogger)) as ScreenLogger;

            // Object not found, we create a new one
            if (instance == null) {
                // Try to load the default prefab
                try {
                    instance = Instantiate(Resources.Load("ScreenLoggerPrefab", typeof(ScreenLogger))) as ScreenLogger;
                } catch {
                    Debug.Log("Failed to load default Screen Logger prefab...");
                    instance = new GameObject("ScreenLogger", typeof(ScreenLogger)).GetComponent<ScreenLogger>();
                }

                // Problem during the creation, this should not happen
                if (instance == null) {
                    Debug.LogError("Problem during the creation of ScreenLogger");
                } else instantiated = true;
            } else {
                instantiated = true;
            }
        }

        public void Awake() {
            ScreenLogger[] obj = GameObject.FindObjectsOfType<ScreenLogger>();

            if (obj.Length > 1) {
                Debug.Log("Destroying ScreenLogger, already exists...");

                destroying = true;

                Destroy(gameObject);
                return;
            }

            InitStyles();

            if (IsPersistent)
                DontDestroyOnLoad(this);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            styleChanged = true;
        }

        private void InitStyles() {
            Texture2D back = new Texture2D(1, 1);
            BackgroundColor.a = BackgroundOpacity;
            back.SetPixel(0, 0, BackgroundColor);
            back.Apply();

            styleContainer = new GUIStyle();
            styleContainer.normal.background = back;
            styleContainer.wordWrap = false;
            styleContainer.padding = new RectOffset(padding, padding, padding, padding);

            styleText = new GUIStyle();
            styleText.fontSize = FontSize;

            styleChanged = false;
        }

        private void OnEnable() {
            if (!ShowInEditor && Application.isEditor) return;

            queue = new Queue<LogMessage>();

#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            Application.RegisterLogCallback(HandleLog);
#else
            Application.logMessageReceived += HandleLog;
#endif
        }

        private void OnDisable() {
            // If destroyed because already exists, don't need to de-register callback
            if (destroying) return;

#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            Application.RegisterLogCallback(null);
#else
            Application.logMessageReceived -= HandleLog;
#endif
        }

        private void Update() {
            if (!ShowInEditor && Application.isEditor) return;

            float InnerHeight = (Screen.height - 2 * Margin) * Height - 2 * padding;
            int TotalRows = (int)(InnerHeight / styleText.lineHeight);

            // Remove overflowing rows
            while (queue.Count > TotalRows)
                queue.Dequeue();
        }

        private void OnGUI() {
            if (!ShowLog) return;
            if (!ShowInEditor && Application.isEditor) return;

            if (styleChanged) InitStyles();

            float w = (Screen.width - 2 * Margin) * Width;
            float h = (Screen.height - 2 * Margin) * Height;
            float x = 1, y = 1;

            switch (AnchorPosition) {
                case LogAnchor.BottomLeft:
                    x = Margin;
                    y = Margin + (Screen.height - 2 * Margin) * (1 - Height);
                    break;

                case LogAnchor.BottomRight:
                    x = Margin + (Screen.width - 2 * Margin) * (1 - Width);
                    y = Margin + (Screen.height - 2 * Margin) * (1 - Height);
                    break;

                case LogAnchor.TopLeft:
                    x = Margin;
                    y = Margin;
                    break;

                case LogAnchor.TopRight:
                    x = Margin + (Screen.width - 2 * Margin) * (1 - Width);
                    y = Margin;
                    break;
            }

            GUILayout.BeginArea(new Rect(x, y, w, h), styleContainer);

            foreach (LogMessage m in queue) {
                switch (m.Type) {
                    case LogType.Warning:
                        styleText.normal.textColor = WarningColor;
                        break;

                    case LogType.Log:
                        styleText.normal.textColor = MessageColor;
                        break;

                    case LogType.Assert:
                    case LogType.Exception:
                    case LogType.Error:
                        styleText.normal.textColor = ErrorColor;
                        break;

                    default:
                        styleText.normal.textColor = MessageColor;
                        break;
                }

                GUILayout.Label(m.Message, styleText);
            }

            GUILayout.EndArea();
        }

        private void HandleLog(string message, string stackTrace, LogType type) {

            if (FileWriter == null && SaveLocal) {
                // 创建日志目录
                Directory.CreateDirectory(Application.persistentDataPath + "/Logs");
                string NowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "_").Replace("/", "_").Replace(":", "_");
                FileInfo fileInfo = new FileInfo(Application.persistentDataPath + "/Logs/" + NowTime + "_Log.txt");
                // 设置输出文件输出地址
                FileWriter = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                encoding = new UTF8Encoding();
            }

            if (type == LogType.Assert && !LogErrors) return;
            if (type == LogType.Error && !LogErrors) return;
            if (type == LogType.Exception && !LogErrors) return;
            if (type == LogType.Log && !LogMessages) return;
            if (type == LogType.Warning && !LogWarnings) return;

            string[] lines = message.Split(new char[] { '\n' });

            foreach (string l in lines)
                queue.Enqueue(new LogMessage(l, type));

            if (FileWriter != null) {
                string content = string.Format("{0} [{1}]: {2} \n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), type, message);
                FileWriter.Write(encoding.GetBytes(content), 0, encoding.GetByteCount(content));
                FileWriter.Flush();
            }

            if (type == LogType.Assert && !StackTraceErrors) return;
            if (type == LogType.Error && !StackTraceErrors) return;
            if (type == LogType.Exception && !StackTraceErrors) return;
            if (type == LogType.Log && !StackTraceMessages) return;
            if (type == LogType.Warning && !StackTraceWarnings) return;

            string[] trace = stackTrace.Split(new char[] { '\n' });

            foreach (string t in trace) {
                if (t.Length != 0) {
                    queue.Enqueue(new LogMessage("  " + t, type));
                }
            }

            if (FileWriter != null) {
                string content = stackTrace + "\n\n";
                FileWriter.Write(encoding.GetBytes(content), 0, encoding.GetByteCount(content));
                FileWriter.Flush();
            }
        }

        public void InspectorGUIUpdated() {
            styleChanged = true;
        }
    }
}

/*
The MIT License

Copyright © 2016 Screen Logger - Giuseppe Portelli <giuseppe@aclockworkberry.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
