﻿using System;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls;

namespace TestCamera
{
    public static class LogHelper
    {
        private static TextBox LogControl;

        public static void SetControl(TextBox control)
        {
            LogControl = control;
        }

        public static async void AddLog(string key, string str)
        {
            if (LogControl == null) { return; }
            await LogControl.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (LogControl.Text.Length > 0) { LogControl.Text += "\r\n"; }
                LogControl.Text += DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + " >> " + key.PadRight(10, ' ') + " >> " + str;
            });
        }

        public static void AddString(string logStr, [CallerMemberName] string callerFuncName = "")
        {
            AddLog(callerFuncName, logStr);
        }

        //public static void AddError(Exception logError, [CallerMemberName] string callerFuncName = "")
        //{
        //    AddLog(callerFuncName, logError.ToString());
        //}

        public static void Clear()
        {
            LogControl.Text = "";
        }
    }
}