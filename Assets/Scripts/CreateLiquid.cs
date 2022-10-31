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
    private Texture2D _texture;
    [SerializeField] MeshRenderer _liquidRenderer;
    [SerializeField] MeshRenderer _screenGrabRenderer;
    [SerializeField] Transform _pointLight;
    private Color[] _colors;

    public ComputeShader ComputeShader;
    public ComputeBuffer _particlesBuffer;
    private ComputeBuffer _screenCellsBuffer;
    private RenderTexture _renderTexture;

    [Header("Compute Params")]
    [Range(0,1)] public float AlphaAtCenter = .1f;
    [Range(0,1)] public float AlphaAtEdge = .01f;
    [Range(0,1)] public float AlphaThreshold = .25f;
    [Space]
    public Color BaseColor = new Color(0,0,1);
    public Color BaseTint = new Color(.6f,.6f,1);
    [Header("Debug")]
    public bool DebugDraw;
    public GameObject Parent;

    private int _main1Compute = -1;
    private int _main2Compute = -1;
    private int _particleHandle = -1;
    private int _screenCellsHandle = -1;
    private int _outputTextureHandle = -1;
    private int _screenGrabTextureHandle = -1;
    void Start()
    {
        _particles = new ScreenParticle[Renderers.Count];
            _screenCells = new ScreenCell[ScreenResolution.x * ScreenResolution.y];
            _texture = new Texture2D(ScreenResolution.x, ScreenResolution.y, TextureFormat.RGBA32, false, true);
            _colors = new Color[_texture.width * _texture.height];
        
            unsafe
            {
                _particlesBuffer = new ComputeBuffer(_particles.Length, sizeof(ScreenParticle));
                _screenCellsBuffer = new ComputeBuffer(_screenCells.Length, sizeof(ScreenCell));
            }

            _renderTexture = new RenderTexture(ScreenResolution.x, ScreenResolution.y, 0, GraphicsFormat.R32G32B32A32_SFloat);
            _renderTexture.enableRandomWrite = true;
            _main1Compute = ComputeShader.FindKernel("main");
            _main2Compute = ComputeShader.FindKernel("main2");
            _particleHandle = Shader.PropertyToID("ScreenParticles");
            _screenCellsHandle = Shader.PropertyToID("ScreenCells");
            _outputTextureHandle = Shader.PropertyToID("Output");
            _screenGrabTextureHandle = Shader.PropertyToID("ScreenGrab");
            _liquidRenderer.material.mainTexture = _renderTexture;
            _screenGrabRenderer.material.mainTexture = Camera.main.targetTexture;
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
            var correctedClipPos = clipPos.xyz;
            correctedClipPos /= clipPos.w;
            correctedClipPos = (correctedClipPos + 1) / 2;
            
            var part = new ScreenParticle();
            part.CameraPosition = clipPos;
            //todo nearclip somehow effects radius and it shouldnt....
            //todo fov changes dont effect radius like it should.
            part.Radius = (1/(clipPos.z))*0.5f;
            // part.NearestPosition = correctedClipPos;
            _particles[i] = part;
        }
        #endregion

        #region compute
        var targetTexture = Camera.main.targetTexture;
        int threadSize = 8;
        _particlesBuffer.SetData(_particles);
        _screenCellsBuffer.SetData(_screenCells);

        ComputeShader.SetFloat("ParticlesLength", _particles.Length);
        ComputeShader.SetVector("CellsDimension", new Vector4(ScreenResolution.x, ScreenResolution.y, 0, 0));
        ComputeShader.SetVector("ScreenGrabDimensions", new Vector4(targetTexture.width, targetTexture.height, 0, 0));
        ComputeShader.SetFloat("AlphaAtCenter", AlphaAtCenter);
        ComputeShader.SetFloat("AlphaAtEdge", AlphaAtEdge);
        ComputeShader.SetFloat("AlphaThreshold", AlphaThreshold);
        ComputeShader.SetVector("BaseColor", BaseColor);
        ComputeShader.SetVector("BaseTint", BaseTint);
        
        //particles to screen
        ComputeShader.SetBuffer(_main1Compute, _particleHandle, _particlesBuffer);
        ComputeShader.SetBuffer(_main1Compute, _screenCellsHandle, _screenCellsBuffer);
        ComputeShader.Dispatch(_main1Compute, ScreenResolution.x / threadSize, ScreenResolution.y /threadSize, 1);
        
        //blur and write to output
        ComputeShader.SetFloat("ParticlesLength", _particles.Length);
        ComputeShader.SetVector("CellsDimension", new Vector4(ScreenResolution.x, ScreenResolution.y, 0, 0));
        ComputeShader.SetVector("ScreenGrabDimensions", new Vector4(targetTexture.width, targetTexture.height, 0, 0));
        ComputeShader.SetVector("LightPosition", lightScreenPos);
        ComputeShader.SetBuffer(_main2Compute, _screenCellsHandle, _screenCellsBuffer);
        ComputeShader.SetTexture(_main2Compute, _outputTextureHandle, _renderTexture);
        ComputeShader.SetTexture(_main2Compute, _screenGrabTextureHandle, targetTexture);
        ComputeShader.Dispatch(_main2Compute, ScreenResolution.x / threadSize, ScreenResolution.y /threadSize, 1);
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
