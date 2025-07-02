using UnityEngine;
using UnityEditor;
using InvaderInsider.Data;

namespace InvaderInsider.Editor
{
    public class CardIconGenerator : EditorWindow
    {
        [MenuItem("InvaderInsider/Card Icon Generator")]
        public static void ShowWindow()
        {
            GetWindow<CardIconGenerator>("Card Icon Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("카드 아이콘 생성", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();

            // 아이콘 크기 선택
            int[] iconSizes = new int[] { 64, 128, 256 };
            int selectedSizeIndex = EditorGUILayout.Popup("아이콘 크기", 1, 
                new string[] { "64x64", "128x128", "256x256" });

            EditorGUILayout.Space();

            // 선택된 카드 데이터 오브젝트
            CardDBObject selectedCard = EditorGUILayout.ObjectField("카드 데이터", null, typeof(CardDBObject), false) as CardDBObject;

            EditorGUILayout.Space();

            // 아이콘 생성 버튼
            if (GUILayout.Button("아이콘 생성"))
            {
                if (selectedCard != null && selectedCard.artwork != null)
                {
                    // 선택된 크기로 아이콘 생성
                    selectedCard.GenerateCardIcon(iconSizes[selectedSizeIndex]);

                    // 변경사항 저장
                    EditorUtility.SetDirty(selectedCard);
                    AssetDatabase.SaveAssets();

                    // 성공 메시지
                    EditorUtility.DisplayDialog(
                        "아이콘 생성 완료", 
                        $"{selectedCard.cardName} 카드의 {iconSizes[selectedSizeIndex]}x{iconSizes[selectedSizeIndex]} 아이콘을 생성했습니다.", 
                        "확인"
                    );
                }
                else
                {
                    // 오류 메시지
                    EditorUtility.DisplayDialog(
                        "오류", 
                        "카드 데이터 또는 아트워크를 선택해주세요.", 
                        "확인"
                    );
                }
            }
        }
    }
} 