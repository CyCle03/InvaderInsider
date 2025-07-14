using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using InvaderInsider.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.Core
{
    /// <summary>
    /// 게임 전체의 예외 처리를 담당하는 중앙 집중식 시스템
    /// </summary>
    public class ExceptionHandler : SingletonManager<ExceptionHandler>
    {
        [Header("Exception Handling Settings")]
        [SerializeField] private bool enableStackTrace = true;
        [SerializeField] private bool pauseOnError = false;
        [SerializeField] private int maxErrorHistory = 100;

        [Header("Recovery Settings")]
        [SerializeField] private bool enableAutoRecovery = true;

        // 에러 기록
        private readonly Queue<ErrorRecord> errorHistory = new Queue<ErrorRecord>();
        private readonly Dictionary<Type, int> errorCounts = new Dictionary<Type, int>();
        
        // 이벤트
        public static event Action<ErrorRecord> OnErrorOccurred;
        public static event Action OnCriticalErrorOccurred;

        [Serializable]
        public class ErrorRecord
        {
            public DateTime timestamp;
            public string message;
            public string stackTrace;
            public ErrorSeverity severity;
            public string context;
            public Type exceptionType;

            public ErrorRecord(Exception exception, ErrorSeverity severity, string context = null)
            {
                timestamp = DateTime.Now;
                message = exception.Message;
                stackTrace = exception.StackTrace;
                this.severity = severity;
                this.context = context ?? "";
                exceptionType = exception.GetType();
            }

            public ErrorRecord(string message, ErrorSeverity severity, string context = null)
            {
                timestamp = DateTime.Now;
                this.message = message;
                this.severity = severity;
                this.context = context ?? "";
                exceptionType = typeof(Exception);
            }
        }

        public enum ErrorSeverity
        {
            Low,        // 일반적인 경고
            Medium,     // 기능에 영향을 주는 오류
            High,       // 게임 플레이에 영향을 주는 오류
            Critical    // 게임이 중단될 수 있는 치명적 오류
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // Unity의 전역 예외 처리기 등록
            Application.logMessageReceived += HandleUnityLogMessage;
            
            DebugUtils.LogInitialization("ExceptionHandler", true, "전역 예외 처리 시스템 활성화");
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            Application.logMessageReceived -= HandleUnityLogMessage;
        }

        /// <summary>
        /// Unity 로그 메시지 처리
        /// </summary>
        private void HandleUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                ErrorSeverity severity = type == LogType.Exception ? ErrorSeverity.High : ErrorSeverity.Medium;
                var errorRecord = new ErrorRecord(logString, severity, "Unity Engine");
                errorRecord.stackTrace = stackTrace;
                
                RecordError(errorRecord);
            }
        }

        /// <summary>
        /// 예외를 안전하게 처리하고 기록
        /// </summary>
        public static void HandleException(Exception exception, string context = null, ErrorSeverity severity = ErrorSeverity.Medium)
        {
            if (Instance == null) return;

            var errorRecord = new ErrorRecord(exception, severity, context);
            Instance.RecordError(errorRecord);
        }

        /// <summary>
        /// 사용자 정의 에러 기록
        /// </summary>
        public static void LogError(string message, string context = null, ErrorSeverity severity = ErrorSeverity.Medium)
        {
            if (Instance == null) return;

            var errorRecord = new ErrorRecord(message, severity, context);
            Instance.RecordError(errorRecord);
        }

        /// <summary>
        /// 안전한 함수 실행 (예외 포착 및 처리)
        /// </summary>
        public static T SafeExecute<T>(Func<T> function, T fallbackValue = default(T), string context = null)
        {
            try
            {
                return function();
            }
            catch (Exception e)
            {
                HandleException(e, context ?? "SafeExecute", ErrorSeverity.Medium);
                return fallbackValue;
            }
        }

        /// <summary>
        /// 안전한 액션 실행 (예외 포착 및 처리)
        /// </summary>
        public static void SafeExecute(Action action, string context = null)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                HandleException(e, context ?? "SafeExecute", ErrorSeverity.Medium);
            }
        }

        /// <summary>
        /// 에러 기록 및 처리
        /// </summary>
        private void RecordError(ErrorRecord errorRecord)
        {
            // 에러 기록 저장
            errorHistory.Enqueue(errorRecord);
            
            // 히스토리 크기 제한
            while (errorHistory.Count > maxErrorHistory)
            {
                errorHistory.Dequeue();
            }

            // 에러 타입별 카운트 증가
            if (errorCounts.ContainsKey(errorRecord.exceptionType))
            {
                errorCounts[errorRecord.exceptionType]++;
            }
            else
            {
                errorCounts[errorRecord.exceptionType] = 1;
            }

            // 로깅
            LogErrorRecord(errorRecord);

            // 이벤트 발생
            OnErrorOccurred?.Invoke(errorRecord);

            // 심각도에 따른 처리
            HandleErrorBySeverity(errorRecord);
        }

        /// <summary>
        /// 에러 심각도에 따른 처리
        /// </summary>
        private void HandleErrorBySeverity(ErrorRecord errorRecord)
        {
            switch (errorRecord.severity)
            {
                case ErrorSeverity.Low:
                    // 낮은 심각도 - 로그만 기록
                    break;

                case ErrorSeverity.Medium:
                    // 중간 심각도 - 복구 시도
                    if (enableAutoRecovery)
                    {
                        AttemptRecovery(errorRecord);
                    }
                    break;

                case ErrorSeverity.High:
                    // 높은 심각도 - 즉시 복구 시도 및 경고
                    if (enableAutoRecovery)
                    {
                        AttemptRecovery(errorRecord);
                    }
                    
                    if (pauseOnError)
                    {
                        Debug.LogWarning("높은 심각도 오류로 인해 게임이 일시 정지되었습니다.");
                        Time.timeScale = 0f;
                    }
                    break;

                case ErrorSeverity.Critical:
                    // 치명적 오류 - 긴급 복구 또는 안전 종료
                    OnCriticalErrorOccurred?.Invoke();
                    
                    DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                        $"치명적 오류 발생: {errorRecord.message}");
                    
                    if (enableAutoRecovery)
                    {
                        AttemptCriticalRecovery(errorRecord);
                    }
                    break;
            }
        }

        /// <summary>
        /// 일반 복구 시도
        /// </summary>
        private void AttemptRecovery(ErrorRecord errorRecord)
        {
            DebugUtils.LogFormat(GameConstants.LOG_PREFIX_GAME, 
                "복구 시도: {0}", errorRecord.message);

            // 간단한 복구 로직 (필요에 따라 확장)
            if (errorRecord.context?.Contains("UI") == true)
            {
                // UI 관련 오류 복구
                var uiManager = UIManager.Instance;
                uiManager?.RestoreUIState();
            }
            else if (errorRecord.context?.Contains("Game") == true)
            {
                // 게임 로직 오류 복구
                var gameManager = GameManager.Instance;
                gameManager?.HandleErrorRecovery();
            }
        }

        /// <summary>
        /// 치명적 오류 복구 시도
        /// </summary>
        private void AttemptCriticalRecovery(ErrorRecord errorRecord)
        {
            DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                "치명적 오류 복구 시도 중...");

            try
            {
                // 게임 상태 초기화
                Time.timeScale = 0f;
                
                // 메모리 정리
                System.GC.Collect();
                
                // 복구 시도
                var gameManager = GameManager.Instance;
                gameManager?.EmergencyReset();
                
                // 안전한 상태로 복원
                Time.timeScale = 1f;
                
                DebugUtils.Log(GameConstants.LOG_PREFIX_GAME, "치명적 오류 복구 완료");
            }
            catch (Exception e)
            {
                DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                    $"치명적 오류 복구 실패: {e.Message}");
                
                // 마지막 수단: 게임 재시작 요청
                RequestGameRestart();
            }
        }

        /// <summary>
        /// 게임 재시작 요청
        /// </summary>
        private void RequestGameRestart()
        {
            DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, 
                "복구 불가능한 오류로 인해 게임 재시작이 필요합니다.");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// 에러 로깅
        /// </summary>
        private void LogErrorRecord(ErrorRecord errorRecord)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{errorRecord.severity}] {errorRecord.message}");
            
            if (!string.IsNullOrEmpty(errorRecord.context))
            {
                sb.AppendLine($"Context: {errorRecord.context}");
            }
            
            if (enableStackTrace && !string.IsNullOrEmpty(errorRecord.stackTrace))
            {
                sb.AppendLine($"Stack Trace:\n{errorRecord.stackTrace}");
            }

            switch (errorRecord.severity)
            {
                case ErrorSeverity.Low:
                    DebugUtils.LogWarning(GameConstants.LOG_PREFIX_GAME, sb.ToString());
                    break;
                case ErrorSeverity.Medium:
                case ErrorSeverity.High:
                    DebugUtils.LogError(GameConstants.LOG_PREFIX_GAME, sb.ToString());
                    break;
                case ErrorSeverity.Critical:
                    Debug.LogError($"[CRITICAL] {sb}");
                    break;
            }
        }

        /// <summary>
        /// 에러 통계 반환
        /// </summary>
        public Dictionary<Type, int> GetErrorStatistics()
        {
            return new Dictionary<Type, int>(errorCounts);
        }

        /// <summary>
        /// 최근 에러 히스토리 반환
        /// </summary>
        public ErrorRecord[] GetRecentErrors(int count = 10)
        {
            var recentErrors = new List<ErrorRecord>();
            var errorArray = errorHistory.ToArray();
            
            int startIndex = Mathf.Max(0, errorArray.Length - count);
            for (int i = startIndex; i < errorArray.Length; i++)
            {
                recentErrors.Add(errorArray[i]);
            }
            
            return recentErrors.ToArray();
        }

        #if UNITY_EDITOR
        [Header("Debug Tools")]
        [SerializeField] private bool showErrorConsole = false;

        private void OnGUI()
        {
            if (!showErrorConsole || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(Screen.width - 400, 10, 390, 300));
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Error Console", GUI.skin.box);
            GUILayout.Label($"Total Errors: {errorHistory.Count}");
            
            var recentErrors = GetRecentErrors(5);
            foreach (var error in recentErrors)
            {
                GUILayout.Label($"[{error.severity}] {error.message}", GUI.skin.box);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        #endif
    }
} 