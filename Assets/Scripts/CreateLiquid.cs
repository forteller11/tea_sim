using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


public class CreateLiquid : MonoBehaviour
{
    
    public List<MeshRenderer> Renderers = new();
    private ScreenCell [] _screenCells;
    public int2 ScreenResolution = new int2(32,32);
    private ScreenParticle [] _particles;
    [SerializeField] MeshRenderer _liquidRenderer;
    [SerializeField] MeshRenderer _screenGrabRenderer;
    [SerializeField] Transform _pointLight;

    public ComputeShader ComputeShader;
    public ComputeBuffer _particlesBuffer;
    private ComputeBuffer _screenCellsBuffer;
    private ComputeBuffer _screenCells2Buffer;
    private RenderTexture _renderTexture;

    [Header("Compute Params")]
    [Range(0,1)] public float AlphaAtCenter = .1f;
    [Range(0,1)] public float AlphaAtEdge = .01f;
    [Range(0,1)] public float AlphaThreshold = .25f;
    [Space]
    public Color BaseColor = new Color(0,0,1);
    public Color BaseTint = new Color(.6f,.6f,1);
    public Color AmbientColor = new Color(1,1,1);
    [Space]
    [Range(0,1)] public float DiffuseVsAmbient = .25f;
    [Range(0,1)] public float DiffuseVsRefraction = .8f;
    public float RefractionMultiplier = 10f;
    public int SpecularPower = 5;
    [Range(0,1)] public float SpecularRoughness = 0.1f;

    public int BlurNumber = 2;


    [Header("Debug")]
    public bool DebugDraw;
    public GameObject Parent;

    private int _main1Compute = -1;
    private int _blurCompute = -1;
    private int _main2Compute = -1;
    private int _particleHandle = -1;
    private int _screenCellsHandle = -1;
    private int _screenCells2Handle = -1;
    private int _outputTextureHandle = -1;
    private int _screenGrabTextureHandle = -1;

    private RenderTexture _targetRT;
    void Start()
    {
        _particles = new ScreenParticle[Renderers.Count];
            _screenCells = new ScreenCell[ScreenResolution.x * ScreenResolution.y];
        
            unsafe
            {
                _particlesBuffer = new ComputeBuffer(_particles.Length, sizeof(ScreenParticle));
                _screenCellsBuffer  = new ComputeBuffer(_screenCells.Length, sizeof(ScreenCell));
                _screenCells2Buffer = new ComputeBuffer(_screenCells.Length, sizeof(ScreenCell));
            }

            _renderTexture = new RenderTexture(ScreenResolution.x, ScreenResolution.y, 0, GraphicsFormat.R32G32B32A32_SFloat);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.filterMode = FilterMode.Point;
            _main1Compute = ComputeShader.FindKernel("main");
            _main2Compute = ComputeShader.FindKernel("main2");
            _blurCompute = ComputeShader.FindKernel("blurKernal");
            _particleHandle = Shader.PropertyToID("ScreenParticles");
            _screenCellsHandle = Shader.PropertyToID("ScreenCells");
            _screenCells2Handle = Shader.PropertyToID("ScreenCells2");
            _outputTextureHandle = Shader.PropertyToID("Output");
            _screenGrabTextureHandle = Shader.PropertyToID("ScreenGrab");
            _liquidRenderer.material.mainTexture = _renderTexture;
            _screenGrabRenderer.material.mainTexture = Camera.main.targetTexture;
            
            _screenCellsBuffer.SetData(_screenCells);
            _screenCells2Buffer.SetData(_screenCells);
            
            //kernal 1
            ComputeShader.SetBuffer(_main1Compute, _particleHandle, _particlesBuffer);
            ComputeShader.SetBuffer(_main1Compute, _screenCellsHandle, _screenCellsBuffer);
            
            //blur kernal
            ComputeShader.SetBuffer(_blurCompute, _screenCellsHandle, _screenCellsBuffer);
            ComputeShader.SetBuffer(_blurCompute, _screenCells2Handle, _screenCells2Buffer);
            
            //kernal 2
            ComputeShader.SetBuffer(_main2Compute, _screenCellsHandle, _screenCellsBuffer);
            ComputeShader.SetBuffer(_main2Compute, _screenCells2Handle, _screenCells2Buffer);
            ComputeShader.SetTexture(_main2Compute, _outputTextureHandle, _renderTexture);
            _targetRT = Camera.main.targetTexture;
            ComputeShader.SetTexture(_main2Compute, _screenGrabTextureHandle, _targetRT);
    }

    public void SetBlurDoubleBuffers(bool writeToScreenCells2)
    {
        var srcBuffer = writeToScreenCells2 ? _screenCellsBuffer : _screenCells2Buffer;
        var dstBuffer = writeToScreenCells2 ? _screenCells2Buffer : _screenCellsBuffer;
        ComputeShader.SetBuffer(_blurCompute, _screenCellsHandle, srcBuffer);
        ComputeShader.SetBuffer(_blurCompute, _screenCells2Handle, dstBuffer);
    }
    
    void Update()
    {
        #region
        var cam = Camera.main;
        var worldToViewMat = cam.worldToCameraMatrix;
        var aspectRatio = (float) Screen.width / Screen.height;
        float nearClip = cam.nearClipPlane;
        float farClip = cam.farClipPlane;
        float fovRads = math.radians(cam.fieldOfView);
        var projectMat = float4x4.PerspectiveFov(fovRads, aspectRatio, nearClip, farClip);

        #region light
        Vector3 lightPos = _pointLight.position;
        float4 lightScreenPos;
        var lightCamPos = worldToViewMat * new Vector4(lightPos.x, lightPos.y, lightPos.z, 1);
        var lightClipPos = math.mul(projectMat, lightCamPos);
        lightScreenPos = new float4(lightClipPos);
        #endregion
        
        for (var i = 0; i < _particles.Length; i++)
        {
            var go = Renderers[i].transform;
            var worldPos = go.position;
            var camPos = worldToViewMat * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);
            var clipPos = math.mul(projectMat, camPos);
            
            var part = new ScreenParticle();
            part.CameraPosition = clipPos;
            //todo nearclip somehow effects radius and it shouldnt....
            //todo fov changes dont effect radius like it should.
            part.Radius = (1/(clipPos.z))*0.3f;
            // part.NearestPosition = correctedClipPos;
            _particles[i] = part;
        }
        #endregion

        #region compute
        int threadSize = 8;
        int3 threadGroup = new int3(ScreenResolution.x / threadSize, ScreenResolution.y / threadSize, 1);
        
        _particlesBuffer.SetData(_particles);

        ComputeShader.SetFloat("ParticlesLength", _particles.Length);
        ComputeShader.SetVector("CellsDimension", new Vector4(ScreenResolution.x, ScreenResolution.y, 0, 0));
        ComputeShader.SetVector("ScreenGrabDimensions", new Vector4(_targetRT.width, _targetRT.height, 0, 0));
        
        ComputeShader.SetFloat("AlphaAtCenter", AlphaAtCenter);
        ComputeShader.SetFloat("AlphaAtEdge", AlphaAtEdge);
        ComputeShader.SetFloat("AlphaThreshold", AlphaThreshold);
        
        ComputeShader.SetVector("BaseColor", BaseColor);
        ComputeShader.SetVector("BaseTint", BaseTint);
        ComputeShader.SetVector("AmbientColor", AmbientColor);

        ComputeShader.SetFloat("DiffuseVsAmbient", DiffuseVsAmbient);
        ComputeShader.SetFloat("DiffuseVsRefraction", DiffuseVsRefraction);
        ComputeShader.SetFloat("RefractionMultiplier", RefractionMultiplier);
        ComputeShader.SetInt("SpecularPower", SpecularPower);
        ComputeShader.SetFloat("SpecularRoughness", SpecularRoughness);
        
        ComputeShader.SetVector("LightPosition", lightScreenPos);

        
        //particles to screen
        ComputeShader.Dispatch(_main1Compute, threadGroup.x, threadGroup.y, threadGroup.z);
        
        //blur
        for (int i = 0; i < BlurNumber; i++)
        {
            SetBlurDoubleBuffers(true);
            ComputeShader.Dispatch(_blurCompute, threadGroup.x, threadGroup.y, threadGroup.z);
            SetBlurDoubleBuffers(false);
            ComputeShader.Dispatch(_blurCompute, threadGroup.x, threadGroup.y, threadGroup.z);
        }

        //blur and write to output
        ComputeShader.Dispatch(_main2Compute, threadGroup.x, threadGroup.y, threadGroup.z);
        ComputeShader.Dispatch(_main2Compute, threadGroup.x, threadGroup.y, threadGroup.z);
        #endregion
    }
    
    private void OnDrawGizmos()
    {
        if (!DebugDraw)
            return;
        
        Gizmos.color = Color.red;

        float cellSize = 1;
        float cellHalfSize = cellSize *.75f;
        if (_screenCells != null)
            for (int i = 0; i < ScreenResolution.x; i++)
            for (int j = 0; j < ScreenResolution.y; j++)
            {
                int index = i + j * ScreenResolution.x;
                var cell = _screenCells[index];
                var pos = new Vector3(i, j, 0);

                Color color = new Color(cell.NearestNormal.x, cell.NearestNormal.y, cell.NearestNormal.z) * cell.Alpha;
                color.a = 1f;
                Gizmos.color = color;
                Gizmos.DrawCube(pos, new Vector3(cellHalfSize, cellHalfSize, cellHalfSize));
            }
    }
}

[CustomEditor(typeof(CreateLiquid))]
public class CreateLiquidInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var t = target as CreateLiquid;

        if (GUILayout.Button("Change Parent"))
        {
            t.Renderers = new List<MeshRenderer>();
            var renderers = t.Parent.GetComponentsInChildren<MeshRenderer>();
            t.Renderers.AddRange(renderers);
        }

    }
}

[GenerateHLSL(PackingRules.Exact, false)]
[StructLayout(LayoutKind.Sequential)]
public struct ScreenParticle
{
    public float4 CameraPosition;
    public float Radius;
}

[GenerateHLSL(PackingRules.Exact, false)]
[StructLayout(LayoutKind.Sequential)]
public struct ScreenCell
{
    public float Alpha;
    public float3 NearestParticle;
    public float3 FurthestParticle;
    public float3 NearestNormal;
}
