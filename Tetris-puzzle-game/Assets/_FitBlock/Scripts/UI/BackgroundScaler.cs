using UnityEngine;

/// <summary>
/// SpriteRenderer 배경을 카메라 영역에 맞게 자동 스케일링.
/// 런타임 해상도/비율 변경에도 대응.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    private SpriteRenderer sr;

    private float lastAspect;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        FitToScreen();
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (!Mathf.Approximately(cam.aspect, lastAspect))
            FitToScreen();
    }

    private void FitToScreen()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;

        lastAspect = cam.aspect;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        float spriteW = sr.sprite.bounds.size.x;
        float spriteH = sr.sprite.bounds.size.y;

        // 배경 비율과 카메라 비율 비교하여 최적 스케일 결정
        float spriteAspect = spriteW / spriteH;
        float camAspect = camWidth / camHeight;

        float scale;
        if (camAspect <= spriteAspect)
        {
            // 카메라가 배경보다 좁거나 같음 (세로 모드) → 높이 기준
            scale = camHeight / spriteH;
        }
        else
        {
            // 카메라가 배경보다 넓음 (가로 모드) → 너비 기준
            scale = camWidth / spriteW;
        }

        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
