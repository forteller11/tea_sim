using System;
using UnityEngine;

namespace Charly
{
    public class DepthTest : MonoBehaviour
    {
        public RenderTexture DepthSrc;
        public RenderTexture DepthDst;
        public Material Material;

        public Camera Camera;
        
        //todo
        //BlitColorAndDepth()
        //then use the _CameraDepthTexture from the shader 
        //remove now unusused depth and screengrab textures
        
        //https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/renderer-features/how-to-fullscreen-blit.html
        //https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@13.1/api/UnityEngine.Rendering.Blitter.html

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Debug.Log("OnRenderImage");
            //https://forum.unity.com/threads/rendering-using-another-cameras-depth-buffer.749522/ 
            Graphics.SetRenderTarget(DepthDst);
            GL.Clear(false, true, Color.clear);
            Graphics.SetRenderTarget(null);
        
            Camera.SetTargetBuffers(DepthDst.colorBuffer, source.depthBuffer);
        
            // presumably you have to composite the vfx cam's output back into the main image?
            // and presumably you've already assigned the VFXRenderTarget as a texture for the composite material
        
            Graphics.Blit(source, destination);
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
}