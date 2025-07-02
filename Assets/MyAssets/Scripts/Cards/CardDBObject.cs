using UnityEngine;
using InvaderInsider.Cards; // CardRarity 및 CardType 열거형 사용을 위해 추가

namespace InvaderInsider.Data
{
    // 이미지 자르기 확장 메서드 추가
    public static class SpriteExtensions
    {
        public static Sprite CreateSquareIconFromSprite(this Sprite originalSprite, int iconSize = 128)
        {
            // 원본 텍스처 가져오기
            Texture2D originalTexture = originalSprite.texture;
            
            // 원본 텍스처의 너비와 높이
            int width = originalTexture.width;
            int height = originalTexture.height;
            
            // 정사각형 크기 결정 (너비와 높이 중 작은 값)
            int squareSize = Mathf.Min(width, height);
            
            // 중앙에서 자를 시작점 계산
            int startX = (width - squareSize) / 2;
            int startY = (height - squareSize) / 2;
            
            // 새 텍스처 생성
            Texture2D iconTexture = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);
            
            // 원본 텍스처에서 중앙 정사각형 부분 읽기
            Color[] pixels = originalTexture.GetPixels(startX, startY, squareSize, squareSize);
            
            // 새 텍스처로 리사이징
            TextureScale.Bilinear(pixels, squareSize, squareSize, iconSize, iconSize);
            
            // 새 텍스처에 픽셀 적용
            iconTexture.SetPixels(pixels);
            iconTexture.Apply();
            
            // 스프라이트로 변환
            return Sprite.Create(
                iconTexture, 
                new Rect(0, 0, iconSize, iconSize), 
                new Vector2(0.5f, 0.5f)
            );
        }
    }

    // 텍스처 리사이징을 위한 유틸리티 클래스
    public static class TextureScale
    {
        public static void Bilinear(Color[] pixels, int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            Color[] newPixels = new Color[newWidth * newHeight];
            float xRatio = (float)(oldWidth - 1) / (newWidth - 1);
            float yRatio = (float)(oldHeight - 1) / (newHeight - 1);

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float srcX = x * xRatio;
                    float srcY = y * yRatio;

                    int x1 = Mathf.FloorToInt(srcX);
                    int y1 = Mathf.FloorToInt(srcY);
                    int x2 = Mathf.Min(x1 + 1, oldWidth - 1);
                    int y2 = Mathf.Min(y1 + 1, oldHeight - 1);

                    float xDiff = srcX - x1;
                    float yDiff = srcY - y1;

                    Color c00 = pixels[y1 * oldWidth + x1];
                    Color c10 = pixels[y1 * oldWidth + x2];
                    Color c01 = pixels[y2 * oldWidth + x1];
                    Color c11 = pixels[y2 * oldWidth + x2];

                    Color interpolatedColor = BilinearInterpolate(c00, c10, c01, c11, xDiff, yDiff);
                    newPixels[y * newWidth + x] = interpolatedColor;
                }
            }

            // 원본 배열 내용 교체
            System.Array.Copy(newPixels, pixels, newPixels.Length);
        }

        private static Color BilinearInterpolate(Color c00, Color c10, Color c01, Color c11, float xDiff, float yDiff)
        {
            Color c0 = Color.Lerp(c00, c10, xDiff);
            Color c1 = Color.Lerp(c01, c11, xDiff);
            return Color.Lerp(c0, c1, yDiff);
        }
    }

    // 유니티 에디터에서 이 Scriptable Object를 생성할 수 있도록 메뉴 항목 추가
    [CreateAssetMenu(fileName = "NewCard", menuName = "InvaderInsider/Card Data")]
    public partial class CardDBObject : ScriptableObject
    {
        [Header("Card Information")]
        public int cardId; // 카드를 식별할 고유 ID
        public string cardName; // 카드 이름
        [TextArea(3, 5)]
        public string description; // 카드 설명
        public Sprite artwork; // 카드 아트워크
        public Sprite cardIcon; // 카드 아이콘 (작은 크기의 대표 이미지)
        public int cost; // 카드 비용
        public int power; // 카드 능력치
        public CardRarity rarity; // 카드 등급
        public CardType type; // 카드 종류
        public EquipmentTargetType equipmentTarget; // 장비 아이템 적용 대상 추가

        [Header("Equipment Properties (if type is Equipment)")]
        public int equipmentBonusAttack; // 장비 아이템이 부여하는 추가 공격력
        public int equipmentBonusHealth; // 장비 아이템이 부여하는 추가 체력

        [Header("Summon Settings")]
        [Tooltip("이 카드가 소환될 확률 가중치 (높을수록 잘 나옴)")]
        public float summonWeight = 1.0f; // 소환 확률 가중치

        [Header("Gameplay Settings")]
        [Tooltip("이 카드를 소환했을 때 생성될 게임 오브젝트 프리팹")]
        public GameObject cardPrefab; // 카드가 나타낼 게임 오브젝트 (예: 적 프리팹)

        // TODO: 필요에 따라 추가적인 카드 속성 정의
        // public int attackDamage; // 공격력
        // public int health; // 체력
        // public float cooldown; // 쿨다운
        // ... 등

        // 아이콘 생성 메서드 추가
        public void GenerateCardIcon(int iconSize = 128)
        {
            if (artwork != null)
            {
                cardIcon = artwork.CreateSquareIconFromSprite(iconSize);
            }
        }
    }
} 