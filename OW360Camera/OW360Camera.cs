using System;
using System.Collections;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using System.IO;
using OWML.Utils;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OW360Camera
{
    public class OW360Camera : ModBehaviour
    {
        private Camera _camera;
        private Camera camera { 
            get {
                if (_camera == null) {
                    _camera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
                }
                return _camera;
            }
        }
        
        private RenderTexture CubeMap;
        private void Start(){
            if (!SystemInfo.supportsComputeShaders) {
                ModHelper.Console.WriteLine($"{nameof(OW360Camera)} : Compute Shaders are not supported, Skipping", MessageType.Error);
            }
            
            ModHelper.Console.WriteLine($"{nameof(OW360Camera)} is loaded!", MessageType.Success);


            int resolution = Utils.NearestPowerOfTwo(ModHelper.Config.GetSettingsValue<int>("Resolution"));
            ModHelper.Console.WriteLine("Nearest power of 2 is " + resolution, MessageType.Info);

            //Make sure that the resolution is an even number
            resolution = resolution + (resolution % 2);
            //Setup RenderTexture
            CubeMap = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            CubeMap.dimension = TextureDimension.Cube;

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => {
                if (loadScene != OWScene.SolarSystem) return;
                StartCoroutine(_Start());
            };
        }

        private GameObject shadow, player, vfx;
        private IEnumerator _Start()
        {
            //Wait a little bit of time so that it is able to get everything
            yield return new WaitForSeconds(0.1f);
            shadow = GameObject.Find("ShadowProjector");
            vfx = GameObject.Find("PlayerVFX");
            player = GameObject.Find("Traveller_HEA_Player_v2");
        }
        
        private void Update() {
            if (GetKeyDown(Key.M)) {
                PreCapture();
                ScreenShot();
                PostCapture();
            }
        }

        private void PreCapture() {
            shadow.SetActive(false);
            player.SetActive(false);
            vfx.SetActive(false);
            GUIMode.SetRenderMode(GUIMode.RenderMode.Hidden);
        }

        private void PostCapture(){
            shadow.SetActive(true);
            player.SetActive(true);
            vfx.SetActive(true);
            GUIMode.SetRenderMode(GUIMode.RenderMode.FPS);
        }

        private void ScreenShot() {
            ModHelper.Console.WriteLine("Taking 360 ScreenShot", MessageType.Success);
            camera.RenderToCubemap(CubeMap);
            RenderTexture equirect = new RenderTexture(CubeMap.width, CubeMap.height/2, 24, RenderTextureFormat.ARGB32);
            CubeMap.ConvertToEquirect(equirect);
            
            Texture2D screenshot = new Texture2D(equirect.width, equirect.height, TextureFormat.ARGB32, false);
            
            RenderTexture _old = RenderTexture.active;
            RenderTexture.active = equirect;
            screenshot.ReadPixels(new Rect(0,0, equirect.width, equirect.height), 0, 0);
            screenshot.Apply();

            RenderTexture.active = _old;
            equirect.Release();

            byte[] bytes = screenshot.EncodeToJPG();
            string path = ModHelper.Config.GetSettingsValue<string>("SavePath") + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";
            File.WriteAllBytes(path, bytes);
            
            ModHelper.Console.WriteLine("Saved ScreenShot to " + path, MessageType.Success);
        }

        //Stolen from Cheats and Debug mod lol
        private bool GetKeyDown(Key keyCode) {
            return Keyboard.current[keyCode].wasPressedThisFrame;
        }
    }
}
