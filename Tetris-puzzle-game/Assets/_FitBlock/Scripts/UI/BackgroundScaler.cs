using UnityEngine;

/// <summary>
/// SpriteRenderer 배경을 카메라 영역에 맞게 자동 스케일링.
/// 런타임 해상도/비율 변경에도 대응.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        FitToScreen();
    }

    private void FitToScreen()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        float spriteW = sr.sprite.bounds.size.x;
        float spriteH = sr.sprite.bounds.size.y;
        float scaleX = camWidth / spriteW;
        float scaleY = camHeight / spriteH;
        float scale = Mathf.Max(scaleX, scaleY);
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
