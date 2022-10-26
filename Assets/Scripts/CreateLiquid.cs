using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;


public class CreateLiquid : MonoBehaviour
{
    public GameObject Parent;
    public List<MeshRenderer> Renderers = new();
    private ScreenCell [] _screenCells;
    public int2 Resolution = new int2(32,32);
    private ScreenParticle [] _particles;
    void Start()
    {
        _particles = new ScreenParticle[Renderers.Count];
        _screenCells = new ScreenCell[Resolution.x * Resolution.y];
    }

    void Update()
    {
        Debug.Log("update");
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
            part.Radius = (1/(clipPos.z));
            part.CameraDepth = clipPos.z ;
            _particles[i] = part;
            
            Debug.Log(camPos);
            Debug.Log(correctedClipPos);
            Debug.Log(part.ToString());
        }
        #endregion
        
        for (int i = 0; i < Resolution.x; i++)
        for (int j = 0; j < Resolution.y; j++)
        {
            int index = i + j * Resolution.x;
            var cell = _screenCells[index];
            cell.Alpha = 0;
            cell.NearestNormal = float3.zero;
            
            float2 cellScreenPos = new float2((float) i / Resolution.x, (float) j / Resolution.y);
            foreach (var part in _particles)
            {
                float distance = math.distance(cellScreenPos, part.ClipPosition.xy);
                if (distance < part.Radius)
                {
                    cell.Alpha = math.lerp(cell.Alpha, 1, 0.5f);
                    cell.Alpha = 1;
                    
                    cell.NearestNormal = part.GetScreenNormal(cellScreenPos);
                }
            }

            _screenCells[index] = cell;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        float cellSize = 1;
        float cellHalfSize = cellSize *.75f;
        if (_screenCells != null)
            for (int i = 0; i < Resolution.x; i++)
            for (int j = 0; j < Resolution.y; j++)
            {
                int index = i + j * Resolution.x;
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

[StructLayout(LayoutKind.Sequential)]
public struct ScreenCell
{
    public float Alpha;
    public float3 NearestNormal;
}
