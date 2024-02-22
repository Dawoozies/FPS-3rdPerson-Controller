using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ComputeShaderManager : MonoBehaviour
{
    public struct Element
    {
        public int elementType;
        public int elementID;
        public float2 velocity;
        public int gravity;
    }
    public enum ElementType
    {
        Solid, Liquid, Gas
    }
    public enum ElementID
    {
        Air, Sand, Water, Steam
    }
    public ElementID elementToPaint;
    public ComputeShader simulation;
    ComputeBuffer particleBuffer;
    int updateKernel;
    uint updateThreadGroupSizeX, updateThreadGroupSizeY;
    public float timeStep;
    float timer;
    RenderTexture particleScreen;
    public Material spriteMaterial;

    public ComputeShader draw;
    int _DrawKernel;
    uint drawThreadGroupSizeX, drawThreadGroupSizeY;
    [Range(2,50)]
    public float brushSize = 2f;
    Element[] elements;
    void OnDisable()
    {
        particleBuffer.Release();
    }
    void Start()
    {
        //particles = new Transform[particleCount];
        //for (int i = 0; i < particleCount; i++)
        //{
        //    GameObject newParticle = Instantiate(particlePrefab);
        //    particles[i] = newParticle.transform;
        //    int x = Random.Range(0, 20);
        //    int y = Random.Range(10, 1000);
        //    particles[i].transform.position = new(x,y,0f);
        //    bool alreadyAdded = particleDictionary.TryAdd(particles[i].transform.position, particles[i]);
        //    if(alreadyAdded)
        //    {
        //        Debug.Log("Already have a particle here");
        //    }
        //}
        updateKernel = simulation.FindKernel("Update");
        simulation.GetKernelThreadGroupSizes(updateKernel, out updateThreadGroupSizeX, out updateThreadGroupSizeY, out _);
        //_resultBuffer = new ComputeBuffer(particleCount, sizeof(float) * 2);
        //_positionBuffer = new ComputeBuffer(particleCount, sizeof(float) * 2 + sizeof(int) * 3);
        //_output = new Vector2[particleCount];
        //particleComputeShader.SetBuffer(_ForcesKernel, "Positions", _positionBuffer);
        int resolutionX = Screen.width / 4;
        int resolutionY = Screen.height / 4;
        particleBuffer = new ComputeBuffer(resolutionX*resolutionY, 3*sizeof(int) + 2*sizeof(float));

        Element air = new Element();
        air.elementID = (int)ElementID.Air;
        air.elementType = (int)ElementType.Gas;
        elements = new Element[resolutionX * resolutionY];
        for (int i = 0; i < elements.Length; i++)
        {
            elements[i] = air;
        }

        particleBuffer.SetData(elements);

        particleScreen = new RenderTexture(resolutionX, resolutionY, 24, RenderTextureFormat.ARGBFloat);
        //32 * width * height
        particleScreen.enableRandomWrite = true;
        particleScreen.filterMode = FilterMode.Point;
        particleScreen.Create();

        _DrawKernel = draw.FindKernel("Draw");
        draw.GetKernelThreadGroupSizes(_DrawKernel, out drawThreadGroupSizeX, out drawThreadGroupSizeY, out _);
    }
    //void OldUpdate()
    //{
    //    if(timer < timeStep)
    //    {
    //        timer += Time.deltaTime;
    //        return;
    //    }
    //    timer = 0;

    //////    simulation.SetBuffer(updateKernel, "Result", _resultBuffer);
    //    var positions = new Pixel[particleCount];
    //    for(int i = 0; i < particles.Length; i++)
    //    {
    //        positions[i].pos = particles[i].position;
    //        Vector2 below = positions[i].pos + Vector2.down;
    //        Vector2 leftCorner = below + Vector2.left;
    //        Vector2 rightCorner = below + Vector2.right;
    //        positions[i].below = particleDictionary.ContainsKey(below) ? 1 : 0;
    //        positions[i].leftCorner = particleDictionary.ContainsKey(leftCorner) ? 1 : 0;
    //        positions[i].rightCorner = particleDictionary.ContainsKey(rightCorner) ? 1 : 0;
    //    }
    //    _positionBuffer.SetData(positions);
    //    var threadGroups = (int)((particleCount + (_threadGroupSizeX - 1)) / _threadGroupSizeX);
    //    particleComputeShader.Dispatch(_ForcesKernel, threadGroups, 1, 1);
    //    _resultBuffer.GetData(_output);
    //    particleDictionary.Clear();
    //    for(int i = 0; i < particles.Length; i++)
    //    {
    //        particles[i].position = _output[i];
    //        particleDictionary.TryAdd(_output[i], particles[i]);
    //    }
    //}
    private void Update()
    {
        if(timer < timeStep)
        {
            timer += Time.deltaTime;
            return;
        }
        timer = 0;


        draw.SetVector("mousePosition", Input.mousePosition / 4);
        draw.SetBool("mouseDown", Input.GetMouseButton(0));
        draw.SetFloat("brushSize", brushSize);
        draw.SetTexture(_DrawKernel, "textureToDrawOn", particleScreen);
        draw.SetInt("elementToPaint", (int)elementToPaint);
        draw.SetBuffer(_DrawKernel, "elements", particleBuffer);
        draw.Dispatch(_DrawKernel, (int)(particleScreen.width / drawThreadGroupSizeX), (int)(particleScreen.height / drawThreadGroupSizeY), 1);
        particleBuffer.GetData(elements);

        simulation.SetTexture(updateKernel, "particleScreen", particleScreen);
        simulation.SetBuffer(updateKernel, "elements", particleBuffer);
        simulation.Dispatch(updateKernel, (int)(particleScreen.width/updateThreadGroupSizeX), (int)(particleScreen.height / updateThreadGroupSizeY), 1);
        spriteMaterial.mainTexture = particleScreen;
        particleBuffer.GetData(elements);
    }
}
