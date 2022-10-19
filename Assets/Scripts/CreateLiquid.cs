using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;

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
    void Start()
    {
        // RenderingUtils.fullscreenMesh;
        
        // Camera camera = rd.cameraData.camera;
        // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
        // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

        ComputeBuffer buffer;
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.transform.SetParent(Parent.transform);
            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.transform.position = Random.insideUnitSphere * SpawnRadius;
            Renderers.Add(renderer);
        }

        // var mainCamera = Camera.main;
        // var rt = mainCamera.targetTexture;
        // var texture = new Texture2D(rt.width, rt.height, rt.graphicsFormat, TextureCreationFlags.None);
        // texture.ReadPixels(rt, 0, 0);
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
