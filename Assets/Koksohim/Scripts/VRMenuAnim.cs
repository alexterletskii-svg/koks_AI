using UnityEngine;
using System.Collections;

public class VRMenuAnim : MonoBehaviour
{
    private CanvasGroup cg;

    void Start()
    {
        cg = GetComponent<CanvasGroup>();

        transform.localScale = Vector3.zero;
        cg.alpha = 0f;

        StartCoroutine(PopUp());
    }

    private void StartCanvas()
    {
    }


    IEnumerator PopUp()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f; 
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, 0.001f);
            cg.alpha = smooth; 
            yield return null;
        }
    }
}