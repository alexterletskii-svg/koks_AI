using UnityEngine;

public class DestroyOnCollisionByTag : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Тег объекта, при столкновении с которым этот объект уничтожится")]
    public string targetTag = "Player";

    private void OnCollisionEnter(Collision collision)
    {
        // CompareTag работает намного быстрее и безопаснее, чем сравнение строк (имен)
        if (collision.gameObject.CompareTag(targetTag))
        {
            Destroy(gameObject);
        }
    }
}