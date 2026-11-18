using UnityEngine;

public class DestroyOnTriggerByTag : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Тег объекта, при столкновении с которым этот объект уничтожится")]
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(targetTag))
        {
            Destroy(gameObject);
        }
    }
}
