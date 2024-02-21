using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 1000;
    [SerializeField, Range(10, maxResolution)]
    int resolution;
    [SerializeField]
    FunctionLibrary.FunctionName function;
    public enum TransitionMode { Cycle, Random}
    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Cycle;
    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f;
    float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;

    ComputeBuffer positionsBuffer;
    [SerializeField]
    ComputeShader computeShader;
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    [SerializeField]
    Material material;
    [SerializeField]
    Mesh mesh;
    void Awake()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3*4);
    }
    void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
    }
    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }
    void Update()
    {
        duration += Time.deltaTime;
        if (duration >= functionDuration)
        {
            duration -= functionDuration;
            function = FunctionLibrary.GetNextFunctionName(function);
        }
        PickNextFunction();
        UpdateFunctionOnGPU();
    }
    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }
    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        if(transitioning)
        {
            computeShader.SetFloat(transitionProgressId, Mathf.SmoothStep(0f, 1f, duration / transitionDuration));
        }
        computeShader.SetBuffer(0, positionsId, positionsBuffer);

        var kernelIndex =
            (int)function + (int)(transitioning ? transitionFunction : function) * 5;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f/resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }
}
