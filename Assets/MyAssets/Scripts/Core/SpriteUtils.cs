using UnityEngine;

namespace InvaderInsider.Core
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
}