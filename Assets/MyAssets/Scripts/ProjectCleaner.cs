using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
#endif

namespace InvaderInsider
{
    /// <summary>
    /// Unity 프로젝트 정리 유틸리티
    /// </summary>
    public class ProjectCleaner : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Clean Orphaned Meta Files")]
        public static void CleanOrphanedMetaFiles()
        {
            string scriptsPath = "Assets/MyAssets/Scripts";
            if (!Directory.Exists(scriptsPath))
            {
                Debug.LogWarning($"Scripts path not found: {scriptsPath}");
                return;
            }
            
            string[] metaFiles = Directory.GetFiles(scriptsPath, "*.meta", SearchOption.AllDirectories);
            int cleanedCount = 0;
            
            foreach (string metaFile in metaFiles)
            {
                string assetFile = metaFile.Substring(0, metaFile.Length - 5); // .meta 제거
                
                if (!File.Exists(assetFile) && !Directory.Exists(assetFile))
                {
                    Debug.Log($"Deleting orphaned meta file: {metaFile}");
                    try
                    {
                        File.Delete(metaFile);
                        cleanedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to delete {metaFile}: {e.Message}");
                    }
                }
            }
            
            if (cleanedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"Cleaned {cleanedCount} orphaned meta files.");
            }
            else
            {
                Debug.Log("No orphaned meta files found.");
            }
        }
        
        [MenuItem("Tools/Force Clean All Meta Files")]
        public static void ForceCleanAllMetaFiles()
        {
            string scriptsPath = "Assets/MyAssets/Scripts";
            if (!Directory.Exists(scriptsPath))
            {
                Debug.LogWarning($"Scripts path not found: {scriptsPath}");
                return;
            }
            
            // 삭제된 파일들의 이름 목록
            string[] deletedFiles = {
                "DragDiagnostic.cs",
                "DraggableUnit.cs", 
                "FieldUnitDragHelper.cs",
                "QuickFixDragSetup.cs",
                "UnitMergeTarget.cs",
                "ForceAddComponents.cs"
            };
            
            int cleanedCount = 0;
            
            foreach (string deletedFile in deletedFiles)
            {
                string metaFile = Path.Combine(scriptsPath, deletedFile + ".meta");
                if (File.Exists(metaFile))
                {
                    Debug.Log($"Force deleting meta file: {metaFile}");
                    try
                    {
                        File.Delete(metaFile);
                        cleanedCount++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to delete {metaFile}: {e.Message}");
                    }
                }
            }
            
            if (cleanedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"Force cleaned {cleanedCount} specific meta files.");
            }
            else
            {
                Debug.Log("No specific meta files found to clean.");
            }
        }
        
        [MenuItem("Tools/Refresh Asset Database")]
        public static void RefreshAssetDatabase()
        {
            AssetDatabase.Refresh();
            Debug.Log("Asset database refreshed.");
        }
#endif
    }
}