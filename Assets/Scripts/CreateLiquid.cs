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
        var projectMat = float4x4.PerspectiveFov(cam.fieldOfView, aspectRatio, cam.nearClipPlane, cam.farClipPlane);

        for (var i = 0; i < _particles.Length; i++)
        {
            var go = Renderers[i].transform;
            var worldPos = go.position;
            var camSpace = worldToViewMat * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);
            var clipSpace = math.mul(projectMat, camSpace);
            var part = new ScreenParticle();
            part.Position = (clipSpace.xyz + 1)/2;
            part.Radius = 0.1f;
            _particles[i] = part;
            
            Debug.Log(camSpace);
            Debug.Log(clipSpace);
            Debug.Log(part.Position);
        }
        #endregion
        
        for (int i = 0; i < Resolution.x; i++)
        for (int j = 0; j < Resolution.y; j++)
        {
            int index = i + j * Resolution.x;
            var cell = _screenCells[index];
            cell.Alpha = 0;
            
            float2 cellScreenPos = new float2((float) i / Resolution.x, (float) j / Resolution.y);
            foreach (var part in _particles)
            {
                float distance = math.distance(cellScreenPos, part.Position.xy);
                if (distance < part.Radius)
                {
                    cell.Alpha = math.lerp(cell.Alpha, 1, 0.5f);
                    cell.Alpha = 1;
                }
            }

            _screenCells[index] = cell;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        float cellSize = 1;
        float cellHalfSize = cellSize / 2f;
        if (_screenCells != null)
            for (int i = 0; i < Resolution.x; i++)
            for (int j = 0; j < Resolution.y; j++)
            {
                int index = i + j * Resolution.x;
                var cell = _screenCells[index];
                var pos = new Vector3(i, j, 0);
                Gizmos.color = new Color(cell.Alpha, cell.Alpha, cell.Alpha);
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
    public float3 Position;
    public float Radius;
}

[StructLayout(LayoutKind.Sequential)]
public struct ScreenCell
{
    public float Alpha;
}
