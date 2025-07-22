using UnityEngine;
using UnityEngine.UI;
using InvaderInsider.Data;
using InvaderInsider.Cards;
using InvaderInsider.Managers;

namespace InvaderInsider.UI
{
     public class CardButton : MonoBehaviour
     {
         [SerializeField] private CardDisplay cardDisplay; // CardDisplay 컴포넌트 참조
         private Button cardButton;

         private void Awake()
         {
             cardButton = GetComponent<Button>();
             if (cardButton == null)
             {
                 Debug.LogError($"[CardButton] Button 컴포넌트를 찾을 수 없습니다. GameObject: {gameObject.name}");
             }

             if (cardDisplay == null)
             {
                 cardDisplay = GetComponent<CardDisplay>();
                 if (cardDisplay == null)
                 {
                     Debug.LogError($"[CardButton] CardDisplay 컴포넌트를 찾을 수 없습니다. GameObject: {gameObject.name}");
                 }
             }
         }

         public void Initialize(CardDBObject card)
         {
             if (card == null)
             {
                 LogManager.Error("CardButton", "초기화할 카드 데이터가 null입니다.");
                 return;
             }

             // 시각적 업데이트는 CardDisplay에 위임
             cardDisplay?.SetupCard(card);

             LogManager.Info("CardButton", $"초기화 완료 - {card.cardName}");
         }

         public void SetInteractable(bool interactable)
         {
             if (cardButton != null)
             {
                 cardButton.interactable = interactable;
             }
         }

         public void SetSelected(bool selected)
         {
             cardDisplay?.SetHighlight(selected);
         }
     }
}