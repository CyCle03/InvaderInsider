using UnityEngine;
using System.Text;
using System.Diagnostics;
using InvaderInsider.Managers;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 통합 디버그 및 로깅 유틸리티
    /// LogManager와 협력하여 일관된 로깅 시스템 제공
    /// </summary>
    public static class DebugUtils
    {
        private static readonly StringBuilder _stringBuilder = new StringBuilder(GameConstants.STRING_BUILDER_CAPACITY);
        private static readonly object _lock = new object();

        // 로깅 활성화 제어 (개발 단계별 조정 가능)
        private static bool EnableBasicLogs = false;     // 일반 로그
        private static bool EnableInfoLogs = false;      // 정보 로그  
        private static bool EnableWarningLogs = true;    // 경고 로그
        private static bool EnableErrorLogs = true;      // 에러 로그 (항상 활성화 권장)
        private static bool EnableVerboseLogs = false;   // 상세 로그

        /// <summary>
        /// 일반 디버그 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            if (!EnableBasicLogs) return;
            LogManager.Info("Debug", message);
        }

        /// <summary>
        /// 접두사가 있는 디버그 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string prefix, string message)
        {
            if (!EnableBasicLogs) return;
            LogManager.Info(prefix, message);
        }

        /// <summary>
        /// 형식화된 디버그 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogFormat(string prefix, string format, params object[] args)
        {
            if (!EnableBasicLogs) return;
            LogManager.Info(prefix, string.Format(format, args));
        }

        /// <summary>
        /// 경고 로그
        /// </summary>
        public static void LogWarning(string message)
        {
            if (!EnableWarningLogs) return;
            LogManager.Warning("Warning", message);
        }

        /// <summary>
        /// 접두사가 있는 경고 로그
        /// </summary>
        public static void LogWarning(string prefix, string message)
        {
            if (!EnableWarningLogs) return;
            LogManager.Warning(prefix, message);
        }

        /// <summary>
        /// 형식화된 경고 로그
        /// </summary>
        public static void LogWarningFormat(string prefix, string format, params object[] args)
        {
            if (!EnableWarningLogs) return;
            LogManager.Warning(prefix, string.Format(format, args));
        }

        /// <summary>
        /// 에러 로그 (모든 빌드에서 작동)
        /// </summary>
        public static void LogError(string message)
        {
            if (!EnableErrorLogs) return;
            LogManager.Error("Error", message);
        }

        /// <summary>
        /// 접두사가 있는 에러 로그
        /// </summary>
        public static void LogError(string prefix, string message)
        {
            if (!EnableErrorLogs) return;
            LogManager.Error(prefix, message);
        }

        /// <summary>
        /// 형식화된 에러 로그
        /// </summary>
        public static void LogErrorFormat(string prefix, string format, params object[] args)
        {
            if (!EnableErrorLogs) return;
            LogManager.Error(prefix, string.Format(format, args));
        }

        /// <summary>
        /// 정보 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string message)
        {
            if (!EnableInfoLogs) return;
            LogManager.Info("Info", message);
        }

        /// <summary>
        /// 접두사가 있는 정보 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfo(string prefix, string message)
        {
            if (!EnableInfoLogs) return;
            LogManager.Info(prefix, message);
        }

        /// <summary>
        /// 상세 로그 (디버깅용)
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogVerbose(string prefix, string message)
        {
            if (!EnableVerboseLogs) return;
            LogManager.Info($"[VERBOSE]{prefix}", message);
        }

        /// <summary>
        /// 형식화된 상세 로그
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogVerboseFormat(string prefix, string format, params object[] args)
        {
            if (!EnableVerboseLogs) return;
            LogManager.Info($"[VERBOSE]{prefix}", string.Format(format, args));
        }

        /// <summary>
        /// 초기화 관련 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInitialization(string componentName, bool success, string details = "")
        {
            if (!EnableInfoLogs) return;
            
            string message = success 
                ? $"{componentName} 초기화 성공" 
                : $"{componentName} 초기화 실패";
                
            if (!string.IsNullOrEmpty(details))
            {
                message += $": {details}";
            }
            
            if (success)
                LogManager.Info("Init", message);
            else
                LogManager.Error("Init", message);
        }

        /// <summary>
        /// Null 체크와 함께 로그 출력
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogNullCheck(object obj, string objectName, string context)
        {
            if (obj == null && EnableErrorLogs)
            {
                LogManager.Error("NullCheck", $"{context}에서 {objectName}이(가) null입니다.");
            }
        }

        /// <summary>
        /// 성능 측정용 로그 (에디터에서만)
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogPerformance(string operation, float timeMs)
        {
            if (!EnableVerboseLogs) return;
            
            if (timeMs > 16.67f) // 60 FPS 기준
            {
                LogManager.Warning("Performance", $"{operation} 작업이 {timeMs:F2}ms 소요됨 (권장: 16.67ms 이하)");
            }
            else
            {
                LogManager.Info("Performance", $"{operation} 작업 완료 ({timeMs:F2}ms)");
            }
        }

        /// <summary>
        /// 메모리 사용량 체크 (에디터에서만)
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogMemoryUsage(string context)
        {
            if (!EnableVerboseLogs) return;
            
            long memoryUsage = System.GC.GetTotalMemory(false);
            LogManager.Info("Memory", $"{context} - 메모리 사용량: {memoryUsage / 1024f / 1024f:F2} MB");
        }

        /// <summary>
        /// 조건부 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogIf(bool condition, string prefix, string message)
        {
            if (condition && EnableBasicLogs)
            {
                LogManager.Info(prefix, message);
            }
        }

        #region 로깅 제어 메서드

        /// <summary>
        /// 개발 모드 로깅 활성화 (모든 로그 활성화)
        /// </summary>
        public static void EnableDevelopmentLogging()
        {
            EnableBasicLogs = true;
            EnableInfoLogs = true;
            EnableWarningLogs = true;
            EnableErrorLogs = true;
            EnableVerboseLogs = true;
            LogManager.EnableAllLogs();
        }

        /// <summary>
        /// 최소 로깅 모드 (에러와 경고만)
        /// </summary>
        public static void EnableMinimalLogging()
        {
            EnableBasicLogs = false;
            EnableInfoLogs = false;
            EnableWarningLogs = true;
            EnableErrorLogs = true;
            EnableVerboseLogs = false;
            LogManager.EnableMinimalLogs();
        }

        /// <summary>
        /// 프로덕션 모드 로깅 (에러만)
        /// </summary>
        public static void EnableProductionLogging()
        {
            EnableBasicLogs = false;
            EnableInfoLogs = false;
            EnableWarningLogs = false;
            EnableErrorLogs = true;
            EnableVerboseLogs = false;
            LogManager.SetLogLevel(LogManager.LogLevel.Error);
        }

        /// <summary>
        /// 모든 로깅 비활성화
        /// </summary>
        public static void DisableAllLogging()
        {
            EnableBasicLogs = false;
            EnableInfoLogs = false;
            EnableWarningLogs = false;
            EnableErrorLogs = false;
            EnableVerboseLogs = false;
            LogManager.DisableAllLogs();
        }

        #endregion
    }
} 