using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class CreateLiquid : MonoBehaviour
{
    public GameObject Parent;
    public Material Material;
    public RenderTexture RT;
    public float SpawnRadius = 10;
    

    public List<MeshRenderer> Renderers = new();
    
    //todo for all gameobjects
    //costum render them to rt.....
    //then collect that... blur it
    //and marching cubes it (2d)
    //then render it... using depth for texture.
    // Start is called before the first frame update

    [SerializeField] private ComputeShader _liquidShader;
    private ComputeBuffer _circleData;
    private Particle [] _particles;
    // private List<Vector3> _particlesPositions;
    void Start()
    {
        // RenderingUtils.fullscreenMesh;
        
        // Camera camera = rd.cameraData.camera;
        // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
        // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

        _particles = new Particle[Renderers.Count];
        
        _circleData = new ComputeBuffer(_particles.Length, 4*3, ComputeBufferType.Structured);
        _circleData.SetData(_particles);

        int kernalIndex = _liquidShader.FindKernel("main");
        _liquidShader.SetBuffer(kernalIndex, "Particles", _circleData);


        // _particlesPositions = new List<Vector3>(Renderers.Count);
    }

    // Update is called once per frame
    void Update()
    {
        #region

        for (var i = 0; i < _particles.Length; i++)
        {
            var part = _particles[i];
            // part.ScreenPosition.x = part.ScreenPosition.x;
            // part.ScreenPosition.y = part.ScreenPosition.y;
            // part.ScreenPosition.z = 0;
            _particles[i] = part;
        }
        #endregion
        
        int kernalIndex = _liquidShader.FindKernel("main");
        // _circleData.SetData(_particles);
        _liquidShader.Dispatch(kernalIndex, 2, 1, 1);
        _circleData.GetData(_particles);

        // if (Keyboard.current.spaceKey.wasPressedThisFrame)
        // {
        //     var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //     gameObject.transform.SetParent(Parent.transform);
        //     var renderer = gameObject.GetComponent<MeshRenderer>();
        //     renderer.transform.position = Random.insideUnitSphere * SpawnRadius;
        //     Renderers.Add(renderer);
        // }

        // var mainCamera = Camera.main;
        // var rt = mainCamera.targetTexture;
        // var texture = new Texture2D(rt.width, rt.height, rt.graphicsFormat, TextureCreationFlags.None);
        // texture.ReadPixels(rt, 0, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (_particles != null)
            foreach (var part in _particles)
            {
                Gizmos.DrawSphere(part.ScreenPosition * 10, 0.2f);
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
public struct Particle
{
    public float3 ScreenPosition;
}
