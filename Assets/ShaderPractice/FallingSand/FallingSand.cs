using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingSand : MonoBehaviour
{
    public ComputeShader simulation;
    public Camera mainCamera;
    public GameObject quadPrefab;
    public Material materialOriginal;
    public class ScreenQuad
    {
        public Transform transform;
        public MeshRenderer renderer;
        public Material material;
        public ScreenQuad(GameObject prefab, Material sourceMaterial)
        {
            GameObject clone = Instantiate(prefab);
            transform = clone.transform;
            renderer = clone.GetComponent<MeshRenderer>();
            material = new Material(sourceMaterial);
            renderer.material = material;
        }
        public void SetTexture(RenderTexture renderTexture)
        {
            renderer.material.mainTexture = renderTexture;
        }
        public void SetDimensions(float width, float height)
        {
            transform.localScale = new Vector3(width, height, 1);
        }
    }
    ScreenQuad screenQuad;
    float screenQuadWidth, screenQuadHeight;
    public class DataTexture
    {
        public TextureType type;
        public RenderTexture texture;
        public DataTexture(TextureType type, int width, int height)
        {
            this.type = type;
            if (type == TextureType.Result)
                texture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGBFloat);
            else
                texture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGBInt);
            texture.enableRandomWrite = true;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Create();
        }
    }
    public enum TextureType
    {
        Draw,
        MaterialInput,
        MaterialOutput,
        Result,
    }
    List<DataTexture> dataTextures = new();
    int resolutionX;
    int resolutionY;
    int DrawMaterialsKernel;
    int MergeDrawAndInputKernel;
    int ResolveFallingOnlyKernel;
    int ResolveDiagonalOnlyKernel;
    int ResolveFlowOnlyKernel;
    int DrawResultsKernel;
    int ResetTexturesKernel;

    [Range(0, 20)]
    public float brushSize;
    [Range(0,10)]
    public int drawMaterial;
    public float timeStep;
    float timer;

    public float flowTime;
    float flowTimer;
    private void Awake()
    {
        screenQuad = new ScreenQuad(quadPrefab, materialOriginal);
        Vector2 screenCornerA = new Vector2(0, 0);
        Vector2 screenCornerB = new Vector2(mainCamera.pixelWidth, mainCamera.pixelHeight);
        Vector2 cornerA = mainCamera.ScreenToWorldPoint(screenCornerA);
        Vector2 cornerB = mainCamera.ScreenToWorldPoint(screenCornerB);
        screenQuadWidth = Mathf.Abs(cornerA.x - cornerB.x);
        screenQuadHeight = Mathf.Abs(cornerA.y - cornerB.y);
        screenQuad.SetDimensions(screenQuadWidth, screenQuadHeight);
    }
    private void Start()
    {
        uint threadGroupSizeX;
        uint threadGroupSizeY;
        DrawMaterialsKernel = simulation.FindKernel("DrawMaterials");
        MergeDrawAndInputKernel = simulation.FindKernel("MergeDrawAndInput");
        ResolveFallingOnlyKernel = simulation.FindKernel("ResolveFallingOnly");
        ResolveDiagonalOnlyKernel = simulation.FindKernel("ResolveDiagonalOnly");
        ResolveFlowOnlyKernel = simulation.FindKernel("ResolveFlowOnly");
        DrawResultsKernel = simulation.FindKernel("DrawResults");
        ResetTexturesKernel = simulation.FindKernel("ResetTextures");
        simulation.GetKernelThreadGroupSizes(DrawMaterialsKernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        resolutionX = Mathf.CeilToInt(mainCamera.pixelWidth / threadGroupSizeX) * 2;
        resolutionY = Mathf.CeilToInt(mainCamera.pixelHeight / threadGroupSizeY) * 2;

        DataTexture drawTexture = new DataTexture(TextureType.Draw, resolutionX, resolutionY);
        dataTextures.Add(drawTexture);

        DataTexture cellMaterialsInputTexture = new DataTexture(TextureType.MaterialInput, resolutionX, resolutionY);
        dataTextures.Add(cellMaterialsInputTexture);

        DataTexture cellMaterialsOutputTexture = new DataTexture(TextureType.MaterialOutput, resolutionX, resolutionY);
        dataTextures.Add(cellMaterialsOutputTexture);

        DataTexture resultTexture = new DataTexture(TextureType.Result, resolutionX, resolutionY);
        dataTextures.Add(resultTexture);
    }
    private void Update()
    {
        if(flowTimer < flowTime)
        {
            flowTimer += Time.deltaTime;
        }
        if(timer < timeStep)
        {
            timer += Time.deltaTime;
            return;
        }
        timer = 0;
        simulation.SetFloat("diagRNG", Random.Range(0f, 1f));
        simulation.SetFloat("width", dataTextures[1].texture.width / 8);
        simulation.SetFloat("height", dataTextures[1].texture.height / 8);
        
        if(flowTimer >= flowTime)
        {
            simulation.SetBool("flow", true);
            flowTimer = 0;
        }
        else
        {
            simulation.SetBool("flow", false);
        }

        simulation.SetVector("mousePos", Input.mousePosition/4);
        simulation.SetBool("mouseDown", Input.GetMouseButton(0));
        simulation.SetFloat("brushSize", brushSize);
        simulation.SetInt("drawMaterialType", drawMaterial);
        simulation.SetTexture(DrawMaterialsKernel, "DrawMaterialsOutput", dataTextures[1].texture);
        simulation.Dispatch(DrawMaterialsKernel, resolutionX, resolutionY, 1);

        //simulation.SetTexture(MergeDrawAndInputKernel, "DrawMaterialsOutput", dataTextures[0].texture);
        //simulation.SetTexture(MergeDrawAndInputKernel, "CellMaterialsInput", dataTextures[1].texture);
        //simulation.Dispatch(MergeDrawAndInputKernel, resolutionX, resolutionY, 1);

        simulation.SetTexture(ResolveFallingOnlyKernel, "CellMaterialsInput", dataTextures[1].texture);
        simulation.SetTexture(ResolveFallingOnlyKernel, "CellMaterialsOutput", dataTextures[2].texture);
        simulation.Dispatch(ResolveFallingOnlyKernel, resolutionX, resolutionY, 1);

        simulation.SetTexture(ResolveDiagonalOnlyKernel, "CellMaterialsInput", dataTextures[2].texture);
        simulation.SetTexture(ResolveDiagonalOnlyKernel, "CellMaterialsOutput", dataTextures[1].texture);
        simulation.Dispatch(ResolveDiagonalOnlyKernel, resolutionX, resolutionY, 1);

        simulation.SetTexture(ResolveFlowOnlyKernel, "CellMaterialsInput", dataTextures[1].texture);
        simulation.SetTexture(ResolveFlowOnlyKernel, "CellMaterialsOutput", dataTextures[2].texture);
        simulation.Dispatch(ResolveFlowOnlyKernel, resolutionX, resolutionY, 1);

        simulation.SetTexture(ResolveFlowOnlyKernel, "CellMaterialsInput", dataTextures[2].texture);
        simulation.SetTexture(ResolveFlowOnlyKernel, "CellMaterialsOutput", dataTextures[1].texture);
        simulation.Dispatch(ResolveFlowOnlyKernel, resolutionX, resolutionY, 1);

        simulation.SetTexture(ResolveFlowOnlyKernel, "CellMaterialsInput", dataTextures[1].texture);
        simulation.SetTexture(ResolveFlowOnlyKernel, "CellMaterialsOutput", dataTextures[2].texture);
        simulation.Dispatch(ResolveFlowOnlyKernel, resolutionX, resolutionY, 1);

        simulation.SetTexture(DrawResultsKernel, "CellMaterialsOutput", dataTextures[2].texture);
        simulation.SetTexture(DrawResultsKernel, "Results", dataTextures[3].texture);
        simulation.Dispatch(DrawResultsKernel, resolutionX, resolutionY, 1);
        screenQuad.SetTexture(dataTextures[3].texture);

        simulation.SetTexture(ResetTexturesKernel, "DrawMaterialsOutput", dataTextures[0].texture);
        simulation.SetTexture(ResetTexturesKernel, "CellMaterialsInput", dataTextures[1].texture);
        simulation.SetTexture(ResetTexturesKernel, "CellMaterialsOutput", dataTextures[2].texture);
        simulation.Dispatch(ResetTexturesKernel, resolutionX, resolutionY, 1);
    }
}
