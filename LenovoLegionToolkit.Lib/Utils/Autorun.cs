﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;

namespace LenovoLegionToolkit.Lib.Utils
{
    public static class Autorun
    {
        private const string TaskName = "LenovoLegionToolkit_Autorun_6efcc882-924c-4cbc-8fec-f45c25696f98";

        public static bool IsEnabled => TaskService.Instance.GetTask(TaskName) != null;

        public static void Enable()
        {
            Disable();

            var filename = Process.GetCurrentProcess().MainModule?.FileName;

            if (filename == null)
                throw new InvalidOperationException("Current process file name cannot be null");

            var currentUser = WindowsIdentity.GetCurrent().Name;

            var ts = TaskService.Instance;
            var td = ts.NewTask();
            td.Principal.UserId = currentUser;
            td.Principal.RunLevel = TaskRunLevel.Highest;
            td.Triggers.Add(new LogonTrigger { UserId = currentUser });
            td.Actions.Add($"\"{filename}\"", "--minimized");
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.StopIfGoingOnBatteries = false;
            ts.RootFolder.RegisterTaskDefinition(TaskName, td);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Autorun enabled");
        }

        public static void Disable()
        {
            try
            {
                TaskService.Instance.RootFolder.DeleteTask(TaskName);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Autorun disabled");
            }
            catch
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Autorun was not enabled");
            }
        }
    }
}
