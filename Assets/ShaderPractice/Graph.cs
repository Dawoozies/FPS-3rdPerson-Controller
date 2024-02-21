using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public GameObject pointPrefab;
    [SerializeField, Range(10, 200)]
    int resolution;
    Transform[] points;
    [SerializeField]
    FunctionLibrary.FunctionName function;
    void Awake()
    {
        float step = 2f / resolution;
        var scale = Vector3.one * step;
        points = new Transform[resolution*resolution];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = Instantiate(pointPrefab).transform;
            Transform point = points[i];
            point.localScale = scale;
            point.SetParent(transform, false);
        }
    }
    void Update()
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = f(u, v, time);
        }
    }
}
