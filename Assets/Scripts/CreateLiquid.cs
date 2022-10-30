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
        var correctedLightClipPos = lightClipPos.xy;
        correctedLightClipPos /= lightClipPos.w;
        correctedLightClipPos = (correctedLightClipPos + 1) / 2;
        lightScreenPos = new float4(correctedLightClipPos.xyx, 0);
        #endregion
        
        for (var i = 0; i < _particles.Length; i++)
        {
            var go = Renderers[i].transform;
            var worldPos = go.position;
            var camPos = worldToViewMat * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);
            var clipPos = math.mul(projectMat, camPos);
            var correctedClipPos = clipPos.xy;
            correctedClipPos /= clipPos.w;
            correctedClipPos = (correctedClipPos + 1) / 2;
            
            var part = new ScreenParticle();
            part.ClipPosition = correctedClipPos.xy;
            //todo nearclip somehow effects radius and it shouldnt....
            //todo fov changes dont effect radius like it should.
            part.Radius = (1/(clipPos.z))*0.5f;
            part.CameraDepth = clipPos.z ;
            _particles[i] = part;
        }
        #endregion

        #region compute
        var targetTexture = Camera.main.targetTexture;
        int threadSize = 8;
        _particlesBuffer.SetData(_particles);
        _screenCellsBuffer.SetData(_screenCells);

        //particles to screen
        ComputeShader.SetFloat("ParticlesLength", _particles.Length);
        ComputeShader.SetVector("CellsDimension", new Vector4(ScreenResolution.x, ScreenResolution.y, 0, 0));
        ComputeShader.SetVector("ScreenGrabDimensions", new Vector4(targetTexture.width, targetTexture.height, 0, 0));
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

        if (GUILayout.Button("yo"))
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
    public float2 ClipPosition;
    public float CameraDepth;
    public float Radius;
    public float3 Normal;

    public float3 GetScreenNormal(float2 clipPoint)
    {
        float distFromCenter = math.distance(ClipPosition, clipPoint);
        float2 toEdge = clipPoint - ClipPosition;
        float2 toEdgeDir = math.normalize(toEdge);
        float3 tangent = new float3(toEdgeDir.x, toEdgeDir.y, 0);
        float3 ortho = new float3(0, 0, 1);

        float distFromCenterNorm = distFromCenter / Radius;

        //todo we need to acos interp it I think not just a lerp it to be correct for spheres
        float3 normal = math.lerp(ortho, tangent, distFromCenterNorm);
        return normal;
    }
    public override string ToString()
    {
        return $"Pos: {ClipPosition}, Radius: {Radius}, Depth: {CameraDepth}";
    }
}

[GenerateHLSL(PackingRules.Exact, false)]
[StructLayout(LayoutKind.Sequential)]
public struct ScreenCell
{
    public float Alpha;
    public float NearestParticle;
    public float FarthestParticle;
    public float3 NearestNormal;
}
