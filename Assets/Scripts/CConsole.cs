using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// CommonConsole
/// </summary>
public class CConsole : MonoBehaviour
{
    protected static CConsole _instance;
    /// <summary>
    /// Singleton design pattern
    /// </summary>
    /// <value>The instance.</value>
    public static CConsole Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<CConsole>();
            return _instance;
        }
    }

    private GameObject UIObject;
    [Header("Object References (Must Be Filled)")]
    public InputField CmdInput;
    public Button SendButton;
    public VerticalLayoutGroup Grid;
    public Scrollbar ScrollVertical;
    public Text ExampleText;
    [Header("Settings")]
    public bool ShowOnWarning = false;
    public bool ShowOnError = false;
    public bool ShowOnException = true;
    public int FontSize = 24;


    public int MaxVisibleCmdCount = 200;
    public int TruncateStringLength = 5000;
    public Color ErrorColor = new Color(1, 0.5f, 0);
    public Color ExceptionColor = new Color(1f, 0, 0);
    public Color WarningColor = new Color(1, 1, 0);
    public Color AssertColor = new Color(0, 0, 1);

    [Header("")]
    public string LastInput = "";

    [SerializeField]
    List<GameObject> AllCalledCmds = new List<GameObject>();

    List<KeyValuePair<string, Color>> CMDsToRun = new List<KeyValuePair<string, Color>>();

    public bool IsVisible
    {
        get
        {
            return UIObject.activeInHierarchy;
        }
    }

    /// <summary>
    /// Key parametresi küçük harflerden oluşmalı !!!
    /// </summary>
    public static Dictionary<string, Action> ActionsNoArg = new Dictionary<string, Action>();

    /// <summary>
    /// Key parametresi küçük harflerden oluşmalı !!!
    /// </summary>
    public static Dictionary<string, Action<string>> ActionsWithArg = new Dictionary<string, Action<string>>();


    protected void Awake()
    {
        //If I am the first instance, make me the Singleton
        if (_instance == null)
            _instance = this as CConsole;
        //If a Singleton already exists and you find
        //another reference in scene, destroy it!
        else if (_instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        if (_instance == this)
        {
            Application.logMessageReceivedThreaded += Application_logMessageReceived;
        }
    }

    private void Start()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledThreadException;

        UIObject = Instance.GetComponentInChildren<RectTransform>(true).gameObject;
        StartCoroutine(CoUpdate());

        // Help Cmd
        ActionsNoArg.Add("?", () =>
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
        ActionsWithArg.Add("exception", (s) =>
        {
            throw new UnityException(s);
        });
        // Clear Parametresi
        ActionsNoArg.Add("clear", () =>
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
            CmdInput.text = "";
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
            lock (CMDsToRun)
            {
                if (CMDsToRun.Count > 0)
                {
                    var cmd = CMDsToRun[CMDsToRun.Count - 1];
                    Instance.CreateTextLabel(cmd.Key, cmd.Value);
                    CMDsToRun.RemoveAt(CMDsToRun.Count - 1);
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    private void UnhandledThreadException(object sender, UnhandledExceptionEventArgs e)
    {
        //Crashlytics.logException(e);
        Debug.LogError("Catched an Exception from another Thread !!! " + sender);
        Debug.LogError((Exception)e.ExceptionObject);
    }

    /// <summary>
    /// Catch All Logs
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="stackTrace"></param>
    /// <param name="type"></param>
    private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            Log("Exc: " + condition + ":" + stackTrace, ExceptionColor);
            if (ShowOnException)
                StartCoroutine(ShowCo());
        }
        else if (type == LogType.Error)
        {
            Log("Err: " + condition, ErrorColor);
            if (ShowOnError)
                StartCoroutine(ShowCo());
        }
        else if (type == LogType.Warning)
        {
            Log("Wrg: " + condition, WarningColor);
            if (ShowOnWarning)
                StartCoroutine(ShowCo());
        }
        else if (type == LogType.Assert)
        {
            Log("Asr: " + condition, AssertColor);
        }
        else
        {
            Log(condition);
        }
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
        Instance.ShowCo();
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
        if (Instance != null)
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
    public static void Log(object cmd)
    {
        Log(cmd, Color.white);
    }

    /// <summary>
    /// Sadece metni konsola ekler, herhangi bi komut çalıştırmaz
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="c"></param>
    public static void LogError(object cmd)
    {
        Show();
        Log(cmd, Color.red);
    }

    /// <summary>
    /// Sadece metni konsola ekler, herhangi bi komut çalıştırmaz
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="c"></param>
    public static void Log(object cmd, Color c)
    {
        if (Instance != null && cmd != null && !string.IsNullOrEmpty(cmd.ToString()))
        {
            //Instance.StartCoroutine(Instance.CreateTextLabel(cmd, c));

            lock (Instance.CMDsToRun)
            {
                if (cmd.ToString().Length > Instance.TruncateStringLength)
                    // Başka Threadlerdende komutu çağırabilmek için komutlar sıraya konuluyor
                    Instance.CMDsToRun.Insert(0, new KeyValuePair<string, Color>(
                        cmd.ToString().Substring(0, Instance.TruncateStringLength) + "\n ---------- TRUNCATED ---------- (text > " + Instance.TruncateStringLength + ")", c));
                else
                    // Başka Threadlerdende komutu çağırabilmek için komutlar sıraya konuluyor
                    Instance.CMDsToRun.Insert(0, new KeyValuePair<string, Color>(
                        cmd.ToString(), c));
            }
        }
    }

    /// <summary>
    /// Console sayfasına Label ekler
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    protected virtual void CreateTextLabel(object cmd, Color c)
    {
        string cmdText = cmd.ToString();
        //Debug.Log(cmdText);

        var t = Instantiate(Instance.ExampleText, Instance.Grid.transform);
        AllCalledCmds.Add(t.gameObject);
        t.fontSize = Instance.FontSize;
        //Kullanıcı tarafından geliyosa "<", uygulamadan geliyosa ">"
        t.text = cmdText[0] == '<' || cmdText[0] == '>' ? cmdText : "> " + cmdText;
        t.color = c;
        t.alignment = TextAnchor.UpperLeft;

        Grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        Grid.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Instance.ScrollVertical.value = 0;

        if (AllCalledCmds.Count >= MaxVisibleCmdCount)
        {
            var objectToDestroy = AllCalledCmds[0];
            AllCalledCmds.RemoveAt(0);
            Destroy(objectToDestroy);
        }
    }
}

