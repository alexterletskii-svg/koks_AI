using UnityEngine;
using UnityEngine.UI;
using System;

public class Clock : MonoBehaviour
{
    [SerializeField] private Text timeText;

    void Start()
    {
        UpdateTime();
        InvokeRepeating(nameof(UpdateTime), 0f, 1f);
    }

    void UpdateTime()
    {
        timeText.text = DateTime.Now.ToString("HH : mm");
    }

    void OnDestroy() => CancelInvoke(nameof(UpdateTime));
}
