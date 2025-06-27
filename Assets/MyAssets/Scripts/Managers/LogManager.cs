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
        
        // 극도로 엄격한 로그 필터링 설정
        public static LogLevel MinimumLogLevel = LogLevel.Error; // Error만 출력
        public static bool EnablePerformanceLogs = false; // 성능 로그 완전 비활성화
        public static bool EnableGameplayLogs = false; // 게임플레이 로그 비활성화
        public static bool EnableSystemLogs = false; // 시스템 로그 비활성화
        public static bool EnableUILogs = false; // UI 로그 비활성화
        
        // 전역 로그 필터링 활성화
        public static bool GlobalFilterEnabled = true;
        
        // 차단할 키워드들 (정규식 패턴)
        private static readonly HashSet<string> _blockedPatterns = new HashSet<string>
        {
            // MCP Unity 관련
            @"\[MCP Unity\]",
            @"McpUnity\.",
            @"WebSocket server",
            
            // UI 관련
            @"\[UI\]",
            @"\[TopBar\]",
            @"\[BottomBar\]",
            @"\[InGame\]",
            @"\[SummonChoice\]",
            @"\[Pause\]",
            @"Canvas Sorting Order",
            @"체력 업데이트",
            @"패널이 표시",
            @"패널이 숨겨짐",
            
            // SaveData 관련
            @"\[SaveData\]",
            @"SaveDataManager",
            @"HasSaveData 확인",
            @"게임 데이터 로드",
            @"스테이지 진행 업데이트",
            @"즉시 저장",
            
            // Stage 관련
            @"\[Stage\]",
            @"스테이지.*준비",
            @"스테이지.*시작",
            @"스테이지.*클리어",
            @"스테이지 상태 변경",
            
            // GameManager 관련
            @"\[GameManager\]",
            @"패널 등록",
            @"게임 초기화",
            @"게임 일시정지",
            
            // ResourceManager 관련
            @"\[ResourceManager\]",
            @"ResourceManager 인스턴스",
            
            // CardButton 관련
            @"CardButton:",
            @"필수 UI 요소들이 할당되지",
            @"프리팹 경로를 확인하세요",
            
            // 기타 시스템
            @"중복.*감지",
            @"인스턴스 생성됨",
            @"초기화 완료"
        };
        
        // 개발/에디터 모드에서만 로그 출력
        private static bool ShouldLog =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        static LogManager()
        {
#if UNITY_EDITOR
            // 에디터에서 즉시 로그 필터링 적용
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EnableMinimalLogs();
            
            // Define Symbol 설정으로 로그 제거
            SetupDefineSymbols();
#endif
        }

#if UNITY_EDITOR
        private static void SetupDefineSymbols()
        {
            // DISABLE_LOGS 심볼 추가하여 Debug.Log 호출 완전 제거
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            
            if (!defines.Contains("DISABLE_LOGS"))
            {
                if (!string.IsNullOrEmpty(defines))
                {
                    defines += ";DISABLE_LOGS";
                }
                else
                {
                    defines = "DISABLE_LOGS";
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
            }
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 플레이 모드 진입 시 로그 필터링 다시 적용
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnableMinimalLogs();
            }
        }
#endif

        // 초기화 메서드
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 게임 시작 시 즉시 로그 필터링 적용
            EnableMinimalLogs();
            
            // Unity 에디터에서 로그 필터링 설정
            if (GlobalFilterEnabled)
            {
                Application.logMessageReceived += OnLogReceived;
            }
        }
        
        // Unity 로그 메시지 필터링
        private static void OnLogReceived(string logString, string stackTrace, LogType type)
        {
            if (!GlobalFilterEnabled) return;
            
            // Error와 Exception은 항상 통과
            if (type == LogType.Error || type == LogType.Exception)
            {
                return;
            }
            
            // 차단된 패턴 확인
            foreach (var pattern in _blockedPatterns)
            {
                if (Regex.IsMatch(logString, pattern, RegexOptions.IgnoreCase))
                {
                    // 이 로그는 차단 - 하지만 Unity의 logMessageReceived는 이미 출력된 후 호출됨
                    // 대신 Debug.unityLogger.logEnabled를 사용해야 함
                    return;
                }
            }
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
            Debug.unityLogger.logEnabled = false;
        }
        
        // 로그 다시 활성화
        public static void EnableAllLogs()
        {
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Log;
            MinimumLogLevel = LogLevel.Info;
            EnablePerformanceLogs = true;
            EnableGameplayLogs = true;
            EnableSystemLogs = true;
            EnableUILogs = true;
        }
        
        // 최소 로그만 활성화 (기본값)
        public static void EnableMinimalLogs()
        {
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Error;
            MinimumLogLevel = LogLevel.Error;
            EnablePerformanceLogs = false;
            EnableGameplayLogs = false;
            EnableSystemLogs = false;
            EnableUILogs = false;
        }

        // 통합 로그 메서드
        public static void Log(string tag, string message, LogLevel level = LogLevel.Info, params object[] args)
        {
            if (!ShouldLog) return;
            
            // 로그 레벨 필터링
            if (level < MinimumLogLevel) return;
            
            // 태그별 필터링
            if (!EnableUILogs && IsUITag(tag)) return;
            if (!EnableGameplayLogs && IsGameplayTag(tag)) return;
            if (!EnableSystemLogs && IsSystemTag(tag)) return;

            var formattedMessage = FormatMessage(tag, message, args);
            
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
                    Debug.Log($"[DEBUG] {formattedMessage}");
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
            _stringBuilder.Clear();
            
            // 태그를 캐시에서 가져오거나 생성
            if (!_prefixCache.TryGetValue(tag, out var prefix))
            {
                prefix = $"[{tag}] ";
                _prefixCache[tag] = prefix;
            }
            
            _stringBuilder.Append(prefix);
            
            // 파라미터가 있으면 string.Format 사용, 없으면 직접 추가
            if (args != null && args.Length > 0)
            {
                try
                {
                    _stringBuilder.Append(string.Format(message, args));
                }
                catch (FormatException)
                {
                    _stringBuilder.Append(message);
                    _stringBuilder.Append(" [Format Error with args: ");
                    _stringBuilder.Append(string.Join(", ", args));
                    _stringBuilder.Append("]");
                }
            }
            else
            {
                _stringBuilder.Append(message);
            }
            
            return _stringBuilder.ToString();
        }

        // 성능 모니터링용 메서드 (완전 비활성화)
        public static void LogPerformance(string tag, string operation, float timeMs)
        {
            // 성능 로그는 완전 비활성화
            return;
        }

        // 에러와 함께 스택트레이스 출력
        public static void LogException(string tag, Exception exception, string context = null)
        {
            if (!ShouldLog) return;
            
            var message = string.IsNullOrEmpty(context) 
                ? $"Exception: {exception.Message}" 
                : $"Exception in {context}: {exception.Message}";
                
            Debug.LogError(FormatMessage(tag, message));
            Debug.LogException(exception);
        }

        // SaveDataManager에서 사용하는 메서드들 추가
        public static void LogSave(string message)
        {
            Info("SaveData", message);
        }

        public static void ForceLogOnce(string message)
        {
            // 한번만 출력하는 로그 - 단순화하여 일반 로그로 처리
            Info("System", message);
        }
    }
} 