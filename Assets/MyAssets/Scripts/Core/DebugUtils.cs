using UnityEngine;
using System.Text;
using System.Diagnostics;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 최적화된 디버그 및 로깅 유틸리티
    /// Release 빌드에서 성능 향상을 위한 조건부 컴파일 사용
    /// </summary>
    public static class DebugUtils
    {
        private static readonly StringBuilder _stringBuilder = new StringBuilder(GameConstants.STRING_BUILDER_CAPACITY);
        private static readonly object _lock = new object();

        /// <summary>
        /// 일반 디버그 로그 (완전 비활성화)
        /// </summary>
        public static void Log(string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 접두사가 있는 디버그 로그 (완전 비활성화)
        /// </summary>
        public static void Log(string prefix, string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 형식화된 디버그 로그 (완전 비활성화)
        /// </summary>
        public static void LogFormat(string prefix, string format, params object[] args)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 경고 로그 (완전 비활성화)
        /// </summary>
        public static void LogWarning(string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 접두사가 있는 경고 로그 (완전 비활성화)
        /// </summary>
        public static void LogWarning(string prefix, string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 형식화된 경고 로그
        /// </summary>
        public static void LogWarningFormat(string prefix, string format, params object[] args)
        {
            lock (_lock)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append(prefix);
                _stringBuilder.AppendFormat(format, args);
                UnityEngine.Debug.LogWarning(_stringBuilder.ToString());
            }
        }

        /// <summary>
        /// 에러 로그 (모든 빌드에서 작동)
        /// </summary>
        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// 접두사가 있는 에러 로그
        /// </summary>
        public static void LogError(string prefix, string message)
        {
            lock (_lock)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append(prefix);
                _stringBuilder.Append(message);
                UnityEngine.Debug.LogError(_stringBuilder.ToString());
            }
        }

        /// <summary>
        /// 형식화된 에러 로그
        /// </summary>
        public static void LogErrorFormat(string prefix, string format, params object[] args)
        {
            lock (_lock)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append(prefix);
                _stringBuilder.AppendFormat(format, args);
                UnityEngine.Debug.LogError(_stringBuilder.ToString());
            }
        }

        /// <summary>
        /// 성능 측정용 디버그 로그 (에디터에서만 작동)
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogPerformance(string operation, float timeMs)
        {
            if (timeMs > 16.67f) // 60 FPS 기준 1프레임 시간
            {
                LogWarningFormat(GameConstants.LOG_PREFIX_GAME, 
                    "성능 경고: {0} 작업이 {1:F2}ms 소요됨 (권장: 16.67ms 이하)", 
                    operation, timeMs);
            }
            else
            {
                LogFormat(GameConstants.LOG_PREFIX_GAME, 
                    "성능: {0} 작업 완료 ({1:F2}ms)", 
                    operation, timeMs);
            }
        }

        /// <summary>
        /// 초기화 관련 디버그 로그 (완전 비활성화)
        /// </summary>
        public static void LogInitialization(string componentName, bool success, string details = "")
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 메모리 사용량 체크 (에디터에서만 작동)
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogMemoryUsage(string context)
        {
            long memoryUsage = System.GC.GetTotalMemory(false);
            LogFormat(GameConstants.LOG_PREFIX_GAME, 
                "{0} - 메모리 사용량: {1:F2} MB", 
                context, memoryUsage / 1024f / 1024f);
        }

        /// <summary>
        /// 조건부 로그 (조건이 참일 때만 로그 출력)
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogIf(bool condition, string prefix, string message)
        {
            if (condition)
            {
                Log(prefix, message);
            }
        }

        /// <summary>
        /// Null 체크와 함께 로그 출력
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogNullCheck(object obj, string objectName, string context)
        {
            if (obj == null)
            {
                LogError(GameConstants.LOG_PREFIX_GAME, 
                    $"{context}에서 {objectName}이(가) null입니다.");
            }
        }

        /// <summary>
        /// 일반 정보 로그 (완전 비활성화)
        /// </summary>
        public static void LogInfo(string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 접두사가 있는 정보 로그 (완전 비활성화)
        /// </summary>
        public static void LogInfo(string prefix, string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 형식화된 정보 로그
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogInfoFormat(string prefix, string format, params object[] args)
        {
            lock (_lock)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append("[INFO] ");
                _stringBuilder.Append(prefix);
                _stringBuilder.AppendFormat(format, args);
                UnityEngine.Debug.Log(_stringBuilder.ToString());
            }
        }

        /// <summary>
        /// 상세 디버그 로그 (완전 비활성화)
        /// </summary>
        public static void LogVerbose(string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 접두사가 있는 상세 디버그 로그 (완전 비활성화)
        /// </summary>
        public static void LogVerbose(string prefix, string message)
        {
            // 완전 비활성화
        }

        /// <summary>
        /// 형식화된 상세 디버그 로그
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void LogVerboseFormat(string prefix, string format, params object[] args)
        {
            lock (_lock)
            {
                _stringBuilder.Clear();
                _stringBuilder.Append("[VERBOSE] ");
                _stringBuilder.Append(prefix);
                _stringBuilder.AppendFormat(format, args);
                UnityEngine.Debug.Log(_stringBuilder.ToString());
            }
        }
    }
} 