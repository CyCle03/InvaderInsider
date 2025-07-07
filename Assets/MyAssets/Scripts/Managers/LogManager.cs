using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InvaderInsider.Managers
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class LogManager
    {
        public enum LogLevel
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Debug = 3,
            Verbose = 4
        }

        private static readonly StringBuilder _stringBuilder = new StringBuilder(1024);
        private static readonly object _lock = new object();
        
        public static LogLevel MinimumLogLevel { get; private set; } = LogLevel.Warning;
        private static bool _enableLogs = true; // Internal flag
        public static bool EnableLogs 
        {
            get => _enableLogs;
            private set
            {
                _enableLogs = value;
                UnityEngine.Debug.unityLogger.logEnabled = value; // Control Unity's logger directly
            }
        }
        public static bool GlobalFilterEnabled { get; set; } = true;
        
        private static readonly HashSet<string> _blockedPatterns = new HashSet<string>
        {
            "[ObjectPool]", "Loaded shader", "Shader.CreateGPUSkinningVertexTexture", "RenderTexture",
            "Preloading", "Async", "Texture", "Material", "Shader", "Mesh", "Animation", "AudioClip"
        };
        
        static LogManager()
        {
            EnableMinimalLogging();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SetLogLevel(MinimumLogLevel);
        }
        
        #region Log Control Methods

        public static void SetLogLevel(LogLevel level)
        {
            MinimumLogLevel = level;
            // Always set Unity's logger filter based on our minimum level
            switch (level)
            {
                case LogLevel.Error: UnityEngine.Debug.unityLogger.filterLogType = LogType.Error; break;
                case LogLevel.Warning: UnityEngine.Debug.unityLogger.filterLogType = LogType.Warning; break;
                case LogLevel.Info: 
                case LogLevel.Debug: 
                case LogLevel.Verbose: UnityEngine.Debug.unityLogger.filterLogType = LogType.Log; break;
                default: UnityEngine.Debug.unityLogger.filterLogType = LogType.Exception; break; // Fallback
            }
        }

        public static void EnableDevelopmentLogging()
        {
            EnableLogs = true;
            SetLogLevel(LogLevel.Verbose);
        }

        public static void EnableMinimalLogging()
        {
            EnableLogs = true;
            SetLogLevel(LogLevel.Warning);
        }

        public static void EnableProductionLogging()
        {
            EnableLogs = true;
            SetLogLevel(LogLevel.Error);
        }

        public static void DisableAllLogs()
        {
            EnableLogs = false;
        }

        #endregion

        #region Blocked Pattern Management

        private static bool IsBlockedMessage(string message)
        {
            if (!GlobalFilterEnabled) return false;
            foreach (string pattern in _blockedPatterns)
            {
                if (message.Contains(pattern)) return true;
            }
            return false;
        }

        public static void AddBlockedPattern(string pattern) { if (!string.IsNullOrEmpty(pattern)) _blockedPatterns.Add(pattern); }
        public static void RemoveBlockedPattern(string pattern) => _blockedPatterns.Remove(pattern);
        public static void ClearBlockedPatterns() => _blockedPatterns.Clear();

        #endregion

        #region Core Logging Methods

        // Apply Conditional attributes to the core Log method as well,
        // to ensure it's stripped if somehow directly called in release builds.
        // However, the primary intent is for public convenience methods to be used.
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void Log(LogLevel level, string prefix, string message, params object[] args)
        {
            if (!EnableLogs || level < MinimumLogLevel) return;

            string formattedMessage = FormatMessage(prefix, message, args);
            if (IsBlockedMessage(formattedMessage)) return;

            switch (level)
            {
                case LogLevel.Info: UnityEngine.Debug.Log(formattedMessage); break;
                case LogLevel.Warning: UnityEngine.Debug.LogWarning(formattedMessage); break;
                case LogLevel.Error: UnityEngine.Debug.LogError(formattedMessage); break;
                case LogLevel.Debug: UnityEngine.Debug.Log(formattedMessage); break;
                case LogLevel.Verbose: UnityEngine.Debug.Log($"[VERBOSE] {formattedMessage}"); break;
            }
        }
        
        private static string FormatMessage(string tag, string message, params object[] args)
        {
            lock (_lock)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append($"[{tag}] ");
                if (args != null && args.Length > 0) _stringBuilder.AppendFormat(message, args);
                else _stringBuilder.Append(message);
                return _stringBuilder.ToString();
            }
        }

        public static void LogException(Exception exception, string context = null)
        {
            if (exception == null) return;
            string message = string.IsNullOrEmpty(context) ? exception.ToString() : $"Context: {context}\n{exception}";
            UnityEngine.Debug.LogError(message);
        }

        #endregion

        #region Convenience Methods

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string prefix, string message, params object[] args) => Log(LogLevel.Info, prefix, message, args);
        
        public static void Warning(string prefix, string message, params object[] args) => Log(LogLevel.Warning, prefix, message, args);
        
        public static void Error(string prefix, string message, params object[] args) => Log(LogLevel.Error, prefix, message, args);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Debug(string prefix, string message, params object[] args) => Log(LogLevel.Debug, prefix, message, args);

        [Conditional("UNITY_EDITOR")]
        public static void Verbose(string prefix, string message, params object[] args) => Log(LogLevel.Verbose, prefix, message, args);

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInitialization(string componentName, bool success, string details = "")
        {
            string message = success ? $"{componentName} 초기화 성공" : $"{componentName} 초기화 실패";
            if (!string.IsNullOrEmpty(details)) message += $": {details}";
            
            if (success) Info("Init", message);
            else Error("Init", message);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogNullCheck(object obj, string objectName, string context)
        {
            if (obj == null) Error("NullCheck", $"{context}에서 {objectName}이(가) null입니다.");
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogPerformance(string operation, float timeMs)
        {
            if (timeMs > 16.67f) Warning("Performance", $"{operation} 작업이 {timeMs:F2}ms 소요됨 (권장: 16.67ms 이하)");
            else Verbose("Performance", $"{operation} 작업 완료 ({timeMs:F2}ms)");
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogMemoryUsage(string context)
        {
            Verbose("Memory", $"{context} - 메모리 사용량: {System.GC.GetTotalMemory(false) / 1024f / 1024f:F2} MB");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogIf(bool condition, string prefix, string message)
        {
            if (condition) Info(prefix, message);
        }

        // Backwards compatibility methods
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string prefix, string message) => Info(prefix, message);

        #endregion
    }
}