using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using InvaderInsider.UI;
using InvaderInsider.Managers;

namespace InvaderInsider.Core
{
    public class ExceptionHandler : SingletonManager<ExceptionHandler>
    {
        [Header("Exception Handling Settings")]
        [SerializeField] private bool enableStackTrace = true;
        [SerializeField] private bool pauseOnError = false;
        [SerializeField] private int maxErrorHistory = 100;

        private readonly Queue<ErrorRecord> errorHistory = new Queue<ErrorRecord>();
        public static event Action<ErrorRecord> OnErrorOccurred;

        [Serializable]
        public class ErrorRecord
        {
            public DateTime timestamp;
            public string message;
            public string stackTrace;
            public ErrorSeverity severity;
            public string context;

            public ErrorRecord(Exception exception, ErrorSeverity severity, string context = null)
            {
                timestamp = DateTime.Now;
                message = exception.Message;
                stackTrace = exception.StackTrace;
                this.severity = severity;
                this.context = context ?? "";
            }
        }

        public enum ErrorSeverity { Low, Medium, High, Critical }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Application.logMessageReceived += HandleUnityLogMessage;
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            Application.logMessageReceived -= HandleUnityLogMessage;
        }

        private void HandleUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                var severity = type == LogType.Exception ? ErrorSeverity.High : ErrorSeverity.Medium;
                var errorRecord = new ErrorRecord(new Exception(logString), severity, "Unity Engine");
                errorRecord.stackTrace = stackTrace;
                RecordError(errorRecord);
            }
        }

        public static void HandleException(Exception exception, string context = null, ErrorSeverity severity = ErrorSeverity.Medium)
        {
            if (Instance == null) return;
            var errorRecord = new ErrorRecord(exception, severity, context);
            Instance.RecordError(errorRecord);
        }

        private void RecordError(ErrorRecord errorRecord)
        {
            if (errorHistory.Count >= maxErrorHistory) errorHistory.Dequeue();
            errorHistory.Enqueue(errorRecord);

            LogErrorRecord(errorRecord);
            OnErrorOccurred?.Invoke(errorRecord);

            if (errorRecord.severity >= ErrorSeverity.High && pauseOnError)
            {
                Debug.LogWarning("높은 심각도 오류로 인해 게임이 일시 정지되었습니다.");
                Time.timeScale = 0f;
            }

            if (errorRecord.severity == ErrorSeverity.Critical)
            {
                Debug.LogError("치명적 오류 발생! 게임을 재시작해야 할 수 있습니다.");
                // 이전의 자동 복구 로직 대신 로그만 남깁니다.
            }
        }

        private void LogErrorRecord(ErrorRecord errorRecord)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{errorRecord.severity}] {errorRecord.message}");
            if (!string.IsNullOrEmpty(errorRecord.context)) sb.AppendLine($"Context: {errorRecord.context}");
            if (enableStackTrace && !string.IsNullOrEmpty(errorRecord.stackTrace)) sb.AppendLine($"Stack Trace:\n{errorRecord.stackTrace}");

            Debug.LogError(sb.ToString());
        }
    }
}