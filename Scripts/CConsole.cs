using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;

namespace Arikan
{
    public struct CCmd
    {
        public double Time;
        public string Text;
        public Color Color;

        public CCmd(string text)
        {
            this.Time = CConsole.IsMainThread ? UnityEngine.Time.unscaledTime : 0;
            Text = text;
            Color = Color.white;
        }
        public CCmd(string text, Color color)
        {
            this.Time = CConsole.IsMainThread ? UnityEngine.Time.unscaledTime : 0;
            Text = text;
            Color = color;
        }
    }

    /// <summary>
    /// CommonConsole
    /// </summary>
    [AddComponentMenu("Arikan/CConsole")]
    public class CConsole : SingletonBehaviour<CConsole>
    {
        const string CalledsFromConsoleLogPrefix = "%";
        const string ShowOnConsolePrefix = "> ";
        private GameObject UIObject;
        [Header("Object References (Must Be Filled)")]
        public InputField CmdInput;
        public Button SendButton;
        public Button HideButton;
        public VerticalLayoutGroup Grid;
        public Scrollbar ScrollVertical;
        public Text ExampleText;
        public Button NoArgCmdButtonExample;
        public HorizontalLayoutGroup NoArgCmdButtonsgroup;

        [Header("Settings")]
        public bool ShowOnWarning = false;
        public bool ShowOnError = false;
        public bool ShowOnException = true;
        public int FontSize = 24;

        public int MaxVisibleCmdCount = 200;
        public int TruncateStringLength = 5000;

        public Color ErrorColor = new Color(1,0.5f,0);
        public Color ExceptionColor = new Color(1f,0,0);
        public Color WarningColor = new Color(1,1,0);
        public Color AssertColor = new Color(0,0,1);


        [Header("")]
        public string LastInput = string.Empty;

        [SerializeField]
        List<Text> AllCalledCmds = new List<Text>();

        List<CCmd> CMDsToRun = new List<CCmd>();

        static StringBuilder StrBuilder = new StringBuilder();
        static StringBuilder StrBuilderForText = new StringBuilder();

        public bool IsVisible
        {
            get
            {
                return UIObject.activeInHierarchy;
            }
        }

        static Dictionary<string, Action> ActionsNoArg = new Dictionary<string, Action>();

        static Dictionary<string, Action<string>> ActionsWithArg = new Dictionary<string, Action<string>>();

        // Do this when you start your application
        static int mainThreadId;
        // If called in the non main thread, will return false;
        public static bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == mainThreadId;

        protected override void Awake()
        {
            base.Awake();
            mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            if (!CmdInput)
                CmdInput = transform.GetChild(0).GetChild(1).GetComponent<InputField>();
            if(!SendButton)
                SendButton = transform.GetChild(0).GetChild(2).GetComponent<Button>();
            if(!HideButton)
                HideButton = transform.GetChild(0).GetChild(3).GetComponent<Button>();
            if (!Grid)
                Grid = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<VerticalLayoutGroup>();
            if (!ScrollVertical)
                ScrollVertical = transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            if (!ExampleText)
                ExampleText = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            UIObject = Instance.GetComponentInChildren<RectTransform>(true).gameObject;

            if (!Application.isPlaying)
                return;

            if (_instance == this)
            {
                Application.logMessageReceivedThreaded += Application_logMessageReceived;
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;

            AppDomain.CurrentDomain.UnhandledException += UnhandledThreadException;

            StartCoroutine(CoUpdate());

            // Help Cmd
            AddCmd("?", () =>
            {
                Log("No_Parameter_Commands:", Color.green);
                string text = "";
                foreach (string s in ActionsNoArg.Keys)
                {
                    text += s + ", ";
                }
                Log(text, Color.green);
                Log("With_Parameter_Commands:", Color.green);
                text = "";
                foreach (string s in ActionsWithArg.Keys)
                {
                    text += s + ", ";
                }
                Log(text, Color.green);
            });
            // Örnek bir parametre (argümanlı)
            AddCmd("exception", (s) =>
            {
                throw new UnityException(s);
            });
            // Clear Parametresi
            AddCmd("clear", () =>
            {
                Clear();
            });
#if !UNITY_EDITOR
            Log("For Help: \"?\"", Color.green);
#endif
        }

        void OnApplicationQuit()
        {
            AppDomain.CurrentDomain.UnhandledException -= UnhandledThreadException;
            Application.logMessageReceivedThreaded -= Application_logMessageReceived;
        }

        void Update()
        {
            if (UIObject.activeSelf)
            {
                // Enter
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    Send();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    CmdInput.text = LastInput;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    CmdInput.text = string.Empty;
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    CmdInput.Select();
                }
            }

            // "
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (IsVisible)
                    Hide();
                else
                    Show();
            }
        }

        IEnumerator CoUpdate()
        {
            while (true)
            {
                if (CMDsToRun.Count > 0)
                {
                    lock (CMDsToRun)
                    {
                        var cmd = CMDsToRun[CMDsToRun.Count - 1];
                        Instance.CreateTextLabel(cmd);
                        CMDsToRun.RemoveAt(CMDsToRun.Count - 1);
                    }
                }

                if (UIObject.activeSelf)
                {

                }

                yield return new WaitForEndOfFrame();
            }
        }

        private void UnhandledThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            //Crashlytics.logException(e);
            Debug.LogError("Catched an Exception from another Thread !!! " + sender + "\n" + (Exception)e.ExceptionObject + "\n" + ((Exception)e.ExceptionObject).StackTrace);
        }

        /// <summary>
        /// Catch All Logs
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (condition.StartsWith(CalledsFromConsoleLogPrefix))
                return;

            if(condition.Length > TruncateStringLength)
                condition = condition.Substring(0, TruncateStringLength) + "\n ---------- TRUNCATED ---------- (text > " + Instance.TruncateStringLength + ")";

            if (type == LogType.Exception)
            {
                LogFromApp(new CCmd("Exc: " + condition + ':' + stackTrace, ExceptionColor));
                if(ShowOnException)
                    StartCoroutine(ShowCo());
            }
            else if (type == LogType.Error)
            {
                LogFromApp(new CCmd("Err: " + condition , ErrorColor));
                if(ShowOnError)
                    StartCoroutine(ShowCo());
            }
            else if (type == LogType.Warning)
            {
                LogFromApp(new CCmd("Wrg: " + condition , WarningColor));
                if (ShowOnWarning)
                    StartCoroutine(ShowCo());
            }
            else if (type == LogType.Assert)
            {
                LogFromApp(new CCmd("Asr: " + condition , AssertColor));
            }
            else
            {
                LogFromApp(new CCmd(condition, Color.white));
            }
        }

        public static bool AddCmd(string cmd,Action a)
        {
            if (!ActionsNoArg.ContainsKey(cmd.ToLower()))
            {
                ActionsNoArg.Add(cmd.ToLower(), a);
                var btn = Instantiate(Instance.NoArgCmdButtonExample, Instance.NoArgCmdButtonsgroup.transform);
                btn.name = cmd;
                btn.GetComponentInChildren<Text>().text = cmd;
                btn.onClick.AddListener(new UnityEngine.Events.UnityAction(a));
                return true;
            }
            else
                return false;
        }
        public static bool AddCmd(string cmd, Action<string> a)
        {
            if (!ActionsWithArg.ContainsKey(cmd.ToLower()))
            {
                ActionsWithArg.Add(cmd.ToLower(), a);
                return true;
            }
            else
                return false;
        }
        public static void RemoveCmd(string cmd)
        {
            ActionsWithArg.Remove(cmd.ToLower());
            ActionsNoArg.Remove(cmd.ToLower());

            var btns = Instance.NoArgCmdButtonsgroup.GetComponentsInChildren<Button>();
            var btn = btns.Find(b => b.name == cmd);
            if (btn)
                Destroy(btn.gameObject);
        }

        /// <summary>
        /// Runs a saved CMD
        /// </summary>
        /// <param name="key"></param>
        /// <param name="arg"></param>
        /// <returns>If success</returns>
        bool RunCmd(string key, string arg = null)
        {
            if (arg == null)
            {
                Action value;
                if (ActionsNoArg.TryGetValue(key.ToLower(), out value))
                {
                    value.Invoke();
                    return true;
                }
            }
            else
            {
                Action<string> value;
                if (ActionsWithArg.TryGetValue(key.ToLower(), out value))
                {
                    value.Invoke(arg);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// for another threads
        /// </summary>
        /// <returns></returns>
        private IEnumerator ShowCo()
        {
            Show();
            yield return null;
        }

        /// <summary>
        /// Shows Console
        /// </summary>
        public static void Show()
        {
            Instance.UIObject.SetActive(true);
        }

        /// <summary>
        /// Hide Console
        /// </summary>
        public static void Hide()
        {
            Instance.UIObject.SetActive(false);
        }

        /// <summary>
        /// Clear all texts
        /// </summary>
        public static void Clear()
        {
            foreach (var o in Instance.AllCalledCmds)
                Destroy(o);
            Instance.AllCalledCmds.Clear();
        }

        /// <summary>
        /// InputField deki komutu gönderir
        /// </summary>
        public void Send()
        {
            if (!string.IsNullOrEmpty(CmdInput.text))
            {
                Send(CmdInput.text);
                EventSystem.current.SetSelectedGameObject(CmdInput.gameObject);
            }
        }

        /// <summary>
        /// Direk Komut çalıştırır
        /// </summary>
        /// <param name="cmd"></param>
        public static void Send(string cmd)
        {
            if(Instance != null)
            {
                string cmdAll;
                if (string.IsNullOrEmpty(cmd))
                    cmdAll = Instance.CmdInput.text;
                else
                    cmdAll = cmd;

                // Son girilen komutu kaydet
                Instance.LastInput = cmdAll;

                string cmdHeader = cmd.Split(' ')[0].ToLower();
                // 1 den fazla boşlukta sonraki parçaların hepsini birleştiriyo
                string cmdArg =
                    cmd.Split(' ').Length > 1
                    ? string.Join(" ", cmd.Split(' '), 1, cmd.Split(' ').Length - 1)
                    : null;

                Log("< " + cmd, Color.white);
                Instance.CmdInput.text = "";

                bool success = Instance.RunCmd(cmdHeader, cmdArg);
                if (!success)
                    Log("> Cmd Not Found !!!", Color.yellow);
            }
        }


        /// <summary>
        /// Sadece metni konsola ekler, herhangi bi komut çalıştırmaz
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="c"></param>
        public static void Log(object cmd) => Log(cmd, Color.white, null);
        public static void Log(object cmd, Color color) => Log(cmd, color, null);
        public static void Log(object cmd, Color color, UnityEngine.Object obj)
        {
            if (Instance != null && cmd != null )
            {
                string cmdStr = cmd.ToString();
                if (string.IsNullOrEmpty(cmdStr))
                    return;

                lock (Instance.CMDsToRun)
                {
                    // Başka Threadlerdende komutu çağırabilmek için komutlar sıraya konuluyor
                    Instance.CMDsToRun.Insert(0, new CCmd(cmdStr, color));
                }

                if (cmdStr.StartsWith(CalledsFromConsoleLogPrefix))
                    return;
                // Send Log With PREFIX
                StrBuilder.Append(CalledsFromConsoleLogPrefix);
                StrBuilder.Append(cmdStr);
                if(obj)
                    Debug.Log(StrBuilder.ToString(),obj);
                else
                    Debug.Log(StrBuilder.ToString());
                StrBuilder.Clear();
            }
        }

        /// <summary>
        /// Application_logMessageReceived dan buraya yönlendirilir
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="c"></param>
        static void LogFromApp(CCmd cmd)
        {
            if (Instance != null)
            {
                if (string.IsNullOrEmpty(cmd.Text))
                    return;

                lock (Instance.CMDsToRun)
                {
                    Instance.CMDsToRun.Insert(0, cmd);
                }
            }
        }

        /// <summary>
        /// Console sayfasına Label ekler
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        protected virtual void CreateTextLabel(CCmd cmd)
        {
            string cmdText = cmd.Text;

            Text t;
            if (AllCalledCmds.Count >= MaxVisibleCmdCount)
            {
                t = AllCalledCmds[0];
                t.transform.SetAsLastSibling();

                AllCalledCmds.RemoveAt(0);
                AllCalledCmds.Add(t);
            }
            else
            {
                t = Instantiate(Instance.ExampleText, Instance.Grid.transform);
            }
            AllCalledCmds.Add(t);
            t.fontSize = Instance.FontSize;

            //Kullanıcı tarafından geliyosa "<", uygulamadan geliyosa ">"
            if(cmdText[0] != '<' && cmdText[0] != '>')
                StrBuilderForText.Append(ShowOnConsolePrefix);
            StrBuilderForText.Append('[');
            StrBuilderForText.Append(cmd.Time.ToString("0.00"));
            StrBuilderForText.Append(']');
            StrBuilderForText.Append(' ');
            StrBuilderForText.Append(cmdText);
            t.text = StrBuilderForText.ToString();
            StrBuilderForText.Clear();

            t.color = cmd.Color;
            t.alignment = TextAnchor.UpperLeft;

            Grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            Grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Instance.ScrollVertical.value = 0;
        }

        void ShowPhoneState()
        {
        }
    }
}
