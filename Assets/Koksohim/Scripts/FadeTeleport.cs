using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeTeleport : MonoBehaviour
{
   
    [Header("Ссылки")]
    [SerializeField] private Image fadeImage;         // Ссылка на черный UI Image
    [SerializeField] private Transform playerRig;     // Ссылка на XR Origin (или Camera Rig)
    

    [Header("Настройки")]
    [SerializeField] private float fadeDuration = 0.3f; // Длительность одного затемнения/просветления

    [Header("Точка телепортации для кнопки")]
    [SerializeField] private Vector3 teleportLoc1;
    [SerializeField] private Vector3 teleportLoc2;
    [SerializeField] private Vector3 teleportLoc3; // Место в сцене, куда будем телепортироваться

    // Этот метод УВИДИТ кнопка в инспекторе
    public void TeleportToLoc1()
    {
        if (teleportLoc1 == null)
        {
            Debug.LogWarning("Не указана точка телепортации!");
            return;
        }
        TeleportTo(teleportLoc1);
    }

    public void TeleportToLoc2()
    {
        if (teleportLoc2 == null)
        {
            Debug.LogWarning("Не указана точка телепортации!");
            return;
        }
        TeleportTo(teleportLoc2);
    }

    public void TeleportToLoc3()
    {
        if (teleportLoc3 == null)
        {
            Debug.LogWarning("Не указана точка телепортации!");
            return;
        }
        TeleportTo(teleportLoc3);
    }

    // Вызывайте этот метод, когда нужно телепортироваться
    public void TeleportTo(Vector3 targetPosition)
    {
        StartCoroutine(DoTeleport(targetPosition));
    }

    private IEnumerator DoTeleport(Vector3 targetPosition)
    {
        // 1. Затемнение (Fade to Black)
        yield return StartCoroutine(Fade(1f));

        // 2. Телепортация
        playerRig.position = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);

        // 3. Просветление (Fade In)
        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        //Debug.Log("start");
        float startAlpha = fadeImage.color.a;
        float elapsedTime = 0f;

        // Используем unscaledDeltaTime, чтобы анимация не зависела от Time.timeScale
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);

            // Меняем только прозрачность (Alpha), оставляя цвет черным
            fadeImage.color = new Color(0f, 0f, 0f, newAlpha);
            yield return null;
        }

        // Гарантируем точное конечное значение
        fadeImage.color = new Color(0f, 0f, 0f, targetAlpha);
    }
}
