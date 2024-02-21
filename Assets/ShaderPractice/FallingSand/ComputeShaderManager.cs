using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderManager : MonoBehaviour
{
    struct Pixel
    {
        public Vector2 pos;
        public int somethingBelow;
        public int somethingLeft;
        public int somethingRight;
    }
    public GameObject particlePrefab;
    public ComputeShader particleComputeShader;
    ComputeBuffer _resultBuffer;
    ComputeBuffer _positionBuffer;
    public int particleCount;
    Vector2[] _output;
    int _ForcesKernel;
    uint _threadGroupSize;
    public bool nextStep;
    Transform[] particles;
    Dictionary<Vector2, Transform> particleDictionary = new();
    public float timeStep;
    float timer;
    void OnDisable()
    {
        _resultBuffer.Release();
        _positionBuffer.Release();
    }
    void Start()
    {
        particles = new Transform[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            GameObject newParticle = Instantiate(particlePrefab);
            particles[i] = newParticle.transform;
            int x = Random.Range(0, 100);
            int y = Random.Range(50, 200);
            particles[i].transform.position = new(x,y,0f);
            bool alreadyAdded = particleDictionary.TryAdd(particles[i].transform.position, particles[i]);
            if(alreadyAdded)
            {
                Debug.Log("Already have a particle here");
            }
        }
        _ForcesKernel = particleComputeShader.FindKernel("Forces");
        particleComputeShader.GetKernelThreadGroupSizes(_ForcesKernel, out _threadGroupSize, out _, out _);
        _resultBuffer = new ComputeBuffer(particleCount, sizeof(float) * 2);
        _positionBuffer = new ComputeBuffer(particleCount, sizeof(float) * 2 + sizeof(int) * 3);
        _output = new Vector2[particleCount];
        particleComputeShader.SetInt("Amount", particleCount);
        particleComputeShader.SetFloat("gravity", -1);
        particleComputeShader.SetBuffer(_ForcesKernel, "Positions", _positionBuffer);
    }
    void Update()
    {
        if(timer < timeStep)
        {
            timer += Time.deltaTime;
            return;
        }
        timer = 0;

        particleComputeShader.SetBuffer(_ForcesKernel, "Result", _resultBuffer);
        var positions = new Pixel[particleCount];
        for(int i = 0; i < particles.Length; i++)
        {
            positions[i].pos = particles[i].position;
            Vector2 belowPosKey = positions[i].pos + Vector2.down;
            Vector2 leftPosKey = positions[i].pos + Vector2.left;
            Vector2 rightPosKey = positions[i].pos + Vector2.right;
            positions[i].somethingBelow = particleDictionary.ContainsKey(belowPosKey) ? 1 : 0;
            positions[i].somethingLeft = particleDictionary.ContainsKey(leftPosKey) ? 1 : 0;
            positions[i].somethingRight = particleDictionary.ContainsKey(rightPosKey) ? 1 : 0;
        }
        _positionBuffer.SetData(positions);
        var threadGroups = (int)((particleCount + (_threadGroupSize - 1)) / _threadGroupSize);
        particleComputeShader.Dispatch(_ForcesKernel, threadGroups, 1, 1);
        _resultBuffer.GetData(_output);
        particleDictionary.Clear();
        for(int i = 0; i < particles.Length; i++)
        {
            particles[i].position = _output[i];
            particleDictionary.TryAdd(_output[i], particles[i]);
        }
    }
}
