using UnityEngine;

public class AnimationStopper : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Этот метод будет вызван Animation Event
    /// </summary>
    public void StopAnimation()
    {
        if (animator != null)
        {
            animator.speed = 0f;
            Debug.Log("Анимация остановлена через Animation Event");
        }
    }

    /// <summary>
    /// Вызовите этот метод, если нужно возобновить анимацию
    /// </summary>
    public void ResumeAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            Debug.Log("Анимация возобновлена");
        }
    }
}