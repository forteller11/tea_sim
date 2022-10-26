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
    private List<Particle> _particles;
    // private List<Vector3> _particlesPositions;
    void Start()
    {
        // RenderingUtils.fullscreenMesh;
        
        // Camera camera = rd.cameraData.camera;
        // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
        // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

        _particles = new List<Particle>(Renderers.Count);
        for (int i = 0; i < Renderers.Count; i++)
        {
            _particles.Add(new Particle());    
        }
        _circleData = new ComputeBuffer(_particles.Count, 4*3, ComputeBufferType.Structured);
        
        int kernalIndex = _liquidShader.FindKernel("main");
        _liquidShader.SetBuffer(kernalIndex, "particles", _circleData);

        // _particlesPositions = new List<Vector3>(Renderers.Count);
    }

    // Update is called once per frame
    void Update()
    {
        #region

        for (var i = 0; i < _particles.Count; i++)
        {
            var part = _particles[i];
            part.ScreenPosition.x = Random.value;
            part.ScreenPosition.y = Random.value;
            part.ScreenPosition.z = Random.value;
            _particles[i] = part;
        }
        #endregion
        
        _circleData.SetData(_particles);
        int kernalIndex = _liquidShader.FindKernel("main");
        _liquidShader.Dispatch(kernalIndex, 8, 8, 1);


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
        if (_particles != null)
            foreach (var part in _particles)
            {
                Gizmos.DrawSphere(part.ScreenPosition, 0.2f);
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
