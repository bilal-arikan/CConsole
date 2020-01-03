using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arikan
{
    public class CConsoleCommand : MonoBehaviour
    {
        public string Cmd;

        public UnityEvent OnCalled;

        private void Start()
        {
            if (!string.IsNullOrWhiteSpace(Cmd))
            {
                CConsole.AddCmd(Cmd,OnCalled.Invoke);
            }
        }
    }
}
