using System.Collections;
using UnityEngine;

public class ShaderTest : MonoBehaviour
{
    Material mat;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.S))
        {
            SetSpawn(1.5f);
        }
        if(Input.GetKeyDown(KeyCode.D))
        {
            SetDie(1.5f);
        }
    }

    void SetSpawn(float duration)
    {
        StartCoroutine(AnimateShader(-1f, 1f, duration));
    }

    void SetDie(float duration)
    {
        StartCoroutine(AnimateShader(1f, -1f, duration));
    }

    IEnumerator AnimateShader(float start, float end, float duration)
    {
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float value = Mathf.Lerp(start, end, t);
            mat.SetFloat("_DissolveAmount", value);
            yield return null;
        }
        mat.SetFloat("_DissolveAmount", end);
    }

}
