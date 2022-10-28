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
    public GameObject Parent;
    public List<MeshRenderer> Renderers = new();
    private ScreenCell [] _screenCells;
    public int2 ScreenResolution = new int2(32,32);
    private ScreenParticle [] _particles;
    private Texture2D _texture;
    [SerializeField] Material _material;
    private Color[] _colors;

    [Header("Compute")] public ComputeShader ComputeShader;
    public ComputeBuffer _particlesBuffer;
    private ComputeBuffer _screenCellsBuffer;
    private RenderTexture _renderTexture;

    private int _kernalHandle = -1;
    private int _particleHandle = -1;
    private int _screenCellsHandle = -1;
    private int _renderHandle = -1;
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
            _kernalHandle = ComputeShader.FindKernel("main");
            _particleHandle = Shader.PropertyToID("ScreenParticles");
            _screenCellsHandle = Shader.PropertyToID("ScreenCells");
            _renderHandle = Shader.PropertyToID("Output");
            _material.mainTexture = _renderTexture;


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
            part.Radius = (1/(clipPos.z))*0.5f;
            part.CameraDepth = clipPos.z ;
            _particles[i] = part;
        }
        #endregion
        
        // #region cell-based stuff
        // for (int i = 0; i < ScreenResolution.x; i++)
        // for (int j = 0; j < ScreenResolution.y; j++)
        // {
        //     int index = i + j * ScreenResolution.x;
        //     var cell = _screenCells[index];
        //     cell.Alpha = 0;
        //     cell.NearestNormal = float3.zero;
        //     cell.NearestParticle = Single.PositiveInfinity;
        //     cell.FarthestParticle = Single.NegativeInfinity;
        //
        //     float2 cellScreenPos = new float2((float) i / ScreenResolution.x, (float) j / ScreenResolution.y);
        //     foreach (var part in _particles)
        //     {
        //         //todo make this the distance to the sphere at screen... not just the center
        //         float screenDist = math.distance(cellScreenPos, part.ClipPosition.xy);
        //         if (screenDist < part.Radius)
        //         {
        //             float distance = part.CameraDepth;
        //             cell.Alpha = math.lerp(cell.Alpha, 1, 0.5f);
        //             if (distance < cell.NearestParticle)
        //             {
        //                 cell.NearestNormal = part.GetScreenNormal(cellScreenPos);
        //             }
        //
        //             cell.NearestParticle = math.min(cell.NearestParticle, distance);
        //             cell.FarthestParticle = math.max(cell.FarthestParticle, distance);
        //         }
        //     }
        //     
        //     var color = new Color(cell.NearestNormal.x, cell.NearestNormal.y, cell.NearestNormal.z, cell.Alpha);
        //     _colors[index] = color;
        //     _screenCells[index] = cell;
        // }
        // _texture.SetPixels(_colors);
        // _texture.Apply();
        // #endregion
        
        #region compute
        _particlesBuffer.SetData(_particles);
        _screenCellsBuffer.SetData(_screenCells);
        
        ComputeShader.SetFloat("ParticlesLength", _particles.Length);
        ComputeShader.SetVector("CellsDimension", new Vector4(ScreenResolution.x, ScreenResolution.y, 0, 0));
        ComputeShader.SetBuffer(_kernalHandle, _particleHandle, _particlesBuffer);
        ComputeShader.SetBuffer(_kernalHandle, _screenCellsHandle, _screenCellsBuffer);
        ComputeShader.SetTexture(_kernalHandle, _renderHandle, _renderTexture);
        int threadSize = 8;
        ComputeShader.Dispatch(_kernalHandle, ScreenResolution.x / threadSize, ScreenResolution.y /threadSize, 1);
        _screenCellsBuffer.GetData(_screenCells);
        #endregion

        #region apply texture
        #endregion
    }

    private void OnDrawGizmos()
    {
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
