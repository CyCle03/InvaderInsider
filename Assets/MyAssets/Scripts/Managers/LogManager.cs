using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
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
            Debug = 3
        }

        // Cache and builder for performance
        private static readonly Dictionary<string, string> _prefixCache = new Dictionary<string, string>();
        private static readonly StringBuilder _stringBuilder = new StringBuilder();
        
        // Public properties for log control
        public static LogLevel MinimumLogLevel { get; private set; } = LogLevel.Warning;
        public static bool EnableLogs { get; private set; } = true;
        public static bool GlobalFilterEnabled { get; set; } = true;
        
        // Blocked patterns for fine-grained control
        private static readonly HashSet<string> _blockedPatterns = new HashSet<string>
        {
            "[ObjectPool]",
            "Loaded shader",
            "Shader.CreateGPUSkinningVertexTexture",
            "RenderTexture",
            "Preloading",
            "Async",
            "Texture",
            "Material",
            "Shader",
            "Mesh",
            "Animation",
            "AudioClip"
        };
        
        private static bool ShouldLog => EnableLogs;

        static LogManager()
        {
            // Set initial log level on load
            SetLogLevel(LogLevel.Warning);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // This ensures our log level settings are applied when the game starts.
            SetLogLevel(MinimumLogLevel);
        }
        
        // --- Log Control Methods ---

        /// <summary>
        /// Sets the minimum log level. Messages below this level will be ignored.
        /// This also sets the global Unity logger filter.
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            MinimumLogLevel = level;
            
            // For Debug level, we allow all logs and filter within our own Log method.
            if (level == LogLevel.Debug)
            {
                Debug.unityLogger.filterLogType = LogType.Log;
                return;
            }

            switch (level)
            {
                case LogLevel.Error:
                    Debug.unityLogger.filterLogType = LogType.Error;
                    break;
                case LogLevel.Warning:
                    Debug.unityLogger.filterLogType = LogType.Warning;
                    break;
                case LogLevel.Info:
                    Debug.unityLogger.filterLogType = LogType.Log;
                    break;
                default:
                    // Default to only the most critical logs
                    Debug.unityLogger.filterLogType = LogType.Exception;
                    break;
            }
        }

        /// <summary>
        /// Disables all logging through this manager and Unity's logger.
        /// </summary>
        public static void DisableAllLogs()
        {
            EnableLogs = false;
            Debug.unityLogger.logEnabled = false;
        }
        
        /// <summary>
        /// Enables logging and sets the level to Info.
        /// </summary>
        public static void EnableAllLogs()
        {
            EnableLogs = true;
            Debug.unityLogger.logEnabled = true;
            SetLogLevel(LogLevel.Info);
        }
        
        /// <summary>
        /// Enables logging for Warning and Error levels only.
        /// </summary>
        public static void EnableMinimalLogs()
        {
            EnableLogs = true;
            Debug.unityLogger.logEnabled = true;
            SetLogLevel(LogLevel.Warning);
        }

        // --- Blocked Pattern Management ---

        /// <summary>
        /// Checks if a message should be blocked based on the pattern list.
        /// </summary>
        private static bool IsBlockedMessage(string message)
        {
            if (!GlobalFilterEnabled || _blockedPatterns.Count == 0) return false;
            
            foreach (string pattern in _blockedPatterns)
            {
                if (message.Contains(pattern)) return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a pattern to the block list.
        /// </summary>
        public static void AddBlockedPattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                _blockedPatterns.Add(pattern);
            }
        }
        
        /// <summary>
        /// Removes a pattern from the block list.
        /// </summary>
        public static void RemoveBlockedPattern(string pattern)
        {
            _blockedPatterns.Remove(pattern);
        }
        
        /// <summary>
        /// Clears all patterns from the block list.
        /// </summary>
        public static void ClearBlockedPatterns()
        {
            _blockedPatterns.Clear();
        }

        // --- Core Logging Methods ---

        /// <summary>
        /// The main logging method.
        /// </summary>
        public static void Log(string tag, string message, LogLevel level = LogLevel.Info, params object[] args)
        {
            if (!ShouldLog || level < MinimumLogLevel) return;
            
            // Format first, then check for blocking.
            string formattedMessage = FormatMessage(tag, message, args);
            if (IsBlockedMessage(formattedMessage)) return;

            switch (level)
            {
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formattedMessage);
                    break;
                case LogLevel.Debug:
                    // We add a [DEBUG] prefix to distinguish these logs.
                    Debug.Log($"[DEBUG] {formattedMessage}");
                    break;
            }
        }
        
        private static string FormatMessage(string tag, string message, params object[] args)
        {
            // Using a lock for thread safety on the shared StringBuilder
            lock (_stringBuilder)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append($"[{tag}] ");
                
                if (args != null && args.Length > 0)
                {
                    _stringBuilder.AppendFormat(message, args);
                }
                else
                {
                    _stringBuilder.Append(message);
                }
                
                return _stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Logs an exception. These are always logged regardless of log level.
        /// </summary>
        public static void LogException(Exception exception, string context = null)
        {
            if (exception == null) return;
            string message = string.IsNullOrEmpty(context) ? exception.ToString() : $"Context: {context}\n{exception}";
            Debug.LogError(message);
        }

        // --- Convenience Methods ---

        public static void Info(string tag, string message, params object[] args)
        {
            Log(tag, message, LogLevel.Info, args);
        }
        
        public static void Warning(string tag, string message, params object[] args)
        {
            Log(tag, message, LogLevel.Warning, args);
        }
        
        public static void Error(string tag, string message, params object[] args)
        {
            Log(tag, message, LogLevel.Error, args);
        }

        public static void DebugLog(string tag, string message, params object[] args)
        {
            Log(tag, message, LogLevel.Debug, args);
        }
    }
} 