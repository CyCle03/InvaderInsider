using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private static readonly Dictionary<string, string> _prefixCache = new Dictionary<string, string>();
        private static readonly StringBuilder _stringBuilder = new StringBuilder();
        
        // 로그 기본 활성화 (DebugUtils와 협력)
        public static LogLevel MinimumLogLevel = LogLevel.Warning; // 경고 이상만 로그
        public static bool EnableLogs = true; 
        
        // 전역 로그 필터링 활성화
        public static bool GlobalFilterEnabled = true;
        
        // 차단할 로그 패턴들 추가
        private static readonly HashSet<string> _blockedPatterns = new HashSet<string>
        {
            "[ObjectPool]", // 오브젝트 풀 관련 로그
            "Loaded shader", // 쉐이더 로딩 로그
            "Shader.CreateGPUSkinningVertexTexture", // GPU 스키닝 관련 로그
            "RenderTexture", // 렌더 텍스처 관련 로그
            "Preloading", // 에셋 프리로딩 로그
            "Async", // 비동기 작업 관련 로그
            "Texture", // 텍스처 로딩 관련 로그
            "Material", // 머티리얼 관련 로그
            "Shader", // 쉐이더 관련 로그
            "Mesh", // 메시 관련 로그
            "Animation", // 애니메이션 관련 로그
            "AudioClip" // 오디오 클립 관련 로그
        };
        
        // 개발/에디터 모드에서 로그 활성화
        private static bool ShouldLog => EnableLogs;

        static LogManager()
        {
            // 기본적으로 경고와 에러만 활성화
            EnableMinimalLogs();
            
            #if UNITY_EDITOR
            // 에디터에서는 더 많은 로그 허용
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Warning;
            #endif
        }

        // 초기화 메서드
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Unity 로그 시스템 기본 활성화 (LogManager가 제어)
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Log; // 모든 타입의 로그를 LogManager로 전달

            // LogManager의 최소 로그 레벨 설정
            SetLogLevel(MinimumLogLevel);

            // Unity 로그 메시지 수신 이벤트 등록 (LogManager가 필터링)
            Application.logMessageReceived -= OnLogReceived;
            Application.logMessageReceived += OnLogReceived;
        }
        
        // Unity 로그 메시지 필터링 (LogManager의 설정에 따라)
        private static void OnLogReceived(string logString, string stackTrace, LogType type)
        {
            // LogManager를 통해 출력된 로그는 다시 필터링하지 않음
            if (logString.StartsWith("[") && logString.Contains("] ")) return;

            // LogManager의 설정에 따라 필터링
            LogLevel logLevel = LogLevel.Info;
            if (type == LogType.Warning) logLevel = LogLevel.Warning;
            else if (type == LogType.Error || type == LogType.Exception) logLevel = LogLevel.Error;

            if (logLevel < MinimumLogLevel) return;
            if (IsBlockedMessage(logString)) return;

            // LogManager를 통해 다시 출력 (스택 트레이스는 Unity가 자동으로 처리)
            // Debug.Log, Debug.LogWarning, Debug.LogError는 이미 LogManager.Log에서 처리되므로 직접 호출하지 않음
        }
        
        // 전역 로그 필터링 토글
        public static void SetGlobalLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    // Error만 허용
                    Debug.unityLogger.filterLogType = LogType.Error;
                    break;
                case LogLevel.Warning:
                    // Warning 이상 허용
                    Debug.unityLogger.filterLogType = LogType.Warning;
                    break;
                case LogLevel.Info:
                    // 모든 로그 허용
                    Debug.unityLogger.filterLogType = LogType.Log;
                    break;
                default:
                    Debug.unityLogger.filterLogType = LogType.Error;
                    break;
            }
        }
        
        // 로그 완전 비활성화
        public static void DisableAllLogs()
        {
            EnableLogs = false;
            Debug.unityLogger.logEnabled = false;
            Debug.unityLogger.filterLogType = LogType.Exception;
            Debug.unityLogger.logEnabled = false;
            
            // Unity 콘솔 로그 레벨을 가장 높게 설정 (추가 차단)
            Debug.unityLogger.filterLogType = LogType.Exception;
        }
        
        // 로그 다시 활성화
        public static void EnableAllLogs()
        {
            EnableLogs = true;
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Log;
            MinimumLogLevel = LogLevel.Info;
        }
        
        // 최소 로그만 활성화 (기본값)
        public static void EnableMinimalLogs()
        {
            EnableLogs = true;
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Error;
            MinimumLogLevel = LogLevel.Error;
        }

        // 로그 메시지가 차단 대상인지 확인
        private static bool IsBlockedMessage(string message)
        {
            if (!GlobalFilterEnabled || _blockedPatterns.Count == 0) return false;
            
            foreach (string pattern in _blockedPatterns)
            {
                if (message.Contains(pattern)) return true;
            }
            return false;
        }

        // 메인 로그 메서드
        public static void Log(string tag, string message, LogLevel level = LogLevel.Info, params object[] args)
        {
            if (!ShouldLog || level < MinimumLogLevel) return;
            if (IsBlockedMessage(message)) return;

            string formattedMessage = FormatMessage(tag, message, args);
            
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
                    Debug.Log($"[DEBUG] {formattedMessage}"); // Debug 레벨은 항상 출력
                    break;
            }
        }
        
        private static bool IsUITag(string tag)
        {
            return tag == "UI" || tag == "TopBar" || tag == "BottomBar" || 
                   tag == "InGame" || tag == "SummonChoice" || tag == "Pause";
        }
        
        private static bool IsGameplayTag(string tag)
        {
            return tag == "Stage" || tag == "Player" || tag == "Enemy" || 
                   tag == "Cards" || tag == "GameManager" || tag == "Combat";
        }
        
        private static bool IsSystemTag(string tag)
        {
            return tag == "SaveData" || tag == "ResourceManager" || 
                   tag == "Network" || tag == "Audio";
        }

        // 편의 메서드들
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

        private static string FormatMessage(string tag, string message, params object[] args)
        {
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

        // 예외 로깅 (중요한 예외만 허용)
        public static void LogException(string tag, Exception exception, string context = null)
        {
            // 예외는 허용 (심각한 오류이므로)
            Debug.LogException(exception);
        }

        // SaveDataManager 호환성을 위한 메서드들 (모두 비활성화)
        public static void LogSave(string message)
        {
            Info("Save", message);
        }

        public static void LogSave(string tag, string message, bool isError = false)
        {
            if (isError)
            {
                Error(tag, message);
            }
            else
            {
                Info(tag, message);
            }
        }

        public static void ForceLogOnce(string message)
        {
            // 차단됨
        }

        public static void ForceLogOnce(string tag, string message)
        {
            // 차단됨
        }

        // 로그 제어 메서드들
        public static void SetLogLevel(LogLevel level)
        {
            MinimumLogLevel = level;
            
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
                    Debug.unityLogger.filterLogType = LogType.Exception;
                    break;
            }
        }

        // 차단 패턴 관리 (사용되지 않음)
        public static void AddBlockedPattern(string pattern)
        {
            // 이미 모든 로그가 차단됨
        }
        
        public static void RemoveBlockedPattern(string pattern)
        {
            // 이미 모든 로그가 차단됨
        }
        
        public static void ClearBlockedPatterns()
        {
            // 이미 모든 로그가 차단됨
        }
    }
} 