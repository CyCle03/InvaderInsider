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
        public static LogLevel MinimumLogLevel = LogLevel.Warning; // Warning 이상만 출력
        public static bool EnablePerformanceLogs = false; // 성능 로그 비활성화
        public static bool EnableGameplayLogs = false; // 게임플레이 로그 비활성화 (핵심 로그만)
        public static bool EnableSystemLogs = true; // 시스템 로그 활성화 (에러 추적용)
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
            // 에디터에서 최소 로그만 활성화
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EnableMinimalLogs();
            
            // Define Symbol에서 DISABLE_LOGS 제거
            SetupDefineSymbols();
#endif
        }

#if UNITY_EDITOR
        private static void SetupDefineSymbols()
        {
            // DISABLE_LOGS 심볼 제거하여 Debug.Log 호출 활성화
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            
            if (defines.Contains("DISABLE_LOGS"))
            {
                defines = defines.Replace("DISABLE_LOGS;", "").Replace(";DISABLE_LOGS", "").Replace("DISABLE_LOGS", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                Debug.Log("[LogManager] DISABLE_LOGS 심볼을 제거했습니다. 스크립트 재컴파일이 필요할 수 있습니다.");
            }
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 플레이 모드 진입 시 최소 로그만 활성화
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnableMinimalLogs();
            }
        }
        
        // 수동으로 DISABLE_LOGS 심볼 제거하는 메서드
        [MenuItem("Tools/LogManager/Enable All Logs")]
        public static void ForceEnableAllLogs()
        {
            SetupDefineSymbols();
            EnableAllLogs();
            Debug.Log("[LogManager] 모든 로그가 강제로 활성화되었습니다.");
        }
        
        [MenuItem("Tools/LogManager/Enable Minimal Logs (Default)")]
        public static void ForceEnableMinimalLogs()
        {
            EnableMinimalLogs();
            Debug.LogWarning("[LogManager] 최소 로그 모드로 설정되었습니다. (Warning/Error만 출력)");
        }
        
        [MenuItem("Tools/LogManager/Enable Error Only")]
        public static void EnableErrorOnly()
        {
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Error;
            MinimumLogLevel = LogLevel.Error;
            EnablePerformanceLogs = false;
            EnableGameplayLogs = false;
            EnableSystemLogs = false;
            EnableUILogs = false;
            Debug.LogError("[LogManager] 에러 로그만 활성화되었습니다.");
        }
        
        [MenuItem("Tools/LogManager/Disable All Logs")]
        public static void ForceDisableAllLogs()
        {
            DisableAllLogs();
            // 로그가 비활성화되어 있으므로 콘솔에 출력되지 않음
        }
#endif

        // 초기화 메서드
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 게임 시작 시 최소 로그만 활성화
            EnableMinimalLogs();
            
            // Unity 에디터에서 로그 필터링 설정 - 활성화
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
            MinimumLogLevel = LogLevel.Warning;
            EnablePerformanceLogs = false;
            EnableGameplayLogs = false;
            EnableSystemLogs = true;
            EnableUILogs = false;
        }
        
        // 최소 로그만 활성화 (기본값)
        public static void EnableMinimalLogs()
        {
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Warning; // Warning 이상만 출력
            MinimumLogLevel = LogLevel.Warning; // Warning 이상만 출력
            EnablePerformanceLogs = false;
            EnableGameplayLogs = false;
            EnableSystemLogs = true; // 시스템 로그는 유지 (에러 추적용)
            EnableUILogs = false;
        }

        // 통합 로그 메서드
        public static void Log(string tag, string message, LogLevel level = LogLevel.Info, params object[] args)
        {
            if (!ShouldLog) return;
            
            // 로그 레벨 필터링 - Error와 Warning만 허용
            if (level < MinimumLogLevel) return;
            
            // 태그별 필터링
            if (!EnableUILogs && IsUITag(tag)) return;
            if (!EnableGameplayLogs && IsGameplayTag(tag)) return;
            if (!EnableSystemLogs && IsSystemTag(tag)) return;

            var formattedMessage = FormatMessage(tag, message, args);
            
            switch (level)
            {
                case LogLevel.Info:
                    // Info 레벨은 출력하지 않음 (핵심 로그만)
                    return;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formattedMessage);
                    break;
                case LogLevel.Debug:
                    // Debug 레벨은 출력하지 않음
                    return;
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
    }
} 