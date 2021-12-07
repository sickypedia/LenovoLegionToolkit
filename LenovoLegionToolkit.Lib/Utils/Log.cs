﻿using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace LenovoLegionToolkit.Lib.Utils
{
    public class Log
    {
        private static Log _instance;
        public static Log Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Log();
                return _instance;
            }
        }

        private readonly object _lock = new();
        private readonly string _logPath;

#if DEBUG
        public bool IsTraceEnabled { get; set; } = true;
#else
        public bool IsTraceEnabled { get; set; } = false;
#endif

        public Log()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appData, "LenovoLegionToolkit", "log");
            Directory.CreateDirectory(folderPath);
            _logPath = Path.Combine(folderPath, $"log_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.txt");
        }

        public void Trace(string message = null, [CallerFilePath] string file = null, [CallerMemberName] string caller = null)
        {
            if (!IsTraceEnabled)
                return;

            lock (_lock)
            {
                var date = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                var fileName = Path.GetFileName(file);
                var line = $"[{date}] [{fileName}:{caller}] {message}";
                File.AppendAllLines(_logPath, new[] { line });
            }
        }
    }
}