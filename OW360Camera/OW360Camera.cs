using System;
using System.Collections;
using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using System.IO;
using OWML.Utils;
using Tessellation;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OW360Camera
{
    public class OW360Camera : ModBehaviour
    {
        private Camera _camera;
        private Camera camera { 
            get {
                if (_camera == null)
                {
                    _camera = Locator._playerCamera._mainCamera;
                }
                return _camera;
            }
        }

        private Key screenshotKey;
        
        private RenderTexture CubeMap;
        private void Start(){
            if (!SystemInfo.supportsComputeShaders) {
                ModHelper.Console.WriteLine($"{nameof(OW360Camera)} : Compute Shaders are not supported, Skipping", MessageType.Error);
            }
            
            ModHelper.Console.WriteLine($"{nameof(OW360Camera)} is loaded!", MessageType.Success);
            
            screenshotKey = (Key) System.Enum.Parse(typeof(Key), ModHelper.Config.GetSettingsValue<string>("ScreenshotKey").ToUpper()[0].ToString());
            int resolution = Utils.NearestPowerOfTwo(ModHelper.Config.GetSettingsValue<int>("Resolution"));
            ModHelper.Console.WriteLine("Nearest power of 2 is " + resolution, MessageType.Info);
            initializeCubeMap();
            
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => {
                if (loadScene != OWScene.SolarSystem) return;
                StartCoroutine(_Start());
            };
        }

        private void initializeCubeMap() {
            int resolution = Utils.NearestPowerOfTwo(ModHelper.Config.GetSettingsValue<int>("Resolution"));

            //Make sure that the resolution is an even number
            resolution = resolution + (resolution % 2);
            //Setup RenderTexture
            if(CubeMap != null)
                CubeMap.Release();
            
            CubeMap = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32);
            CubeMap.dimension = TextureDimension.Cube;
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

        public void Update(){
            if (GetKeyDown(screenshotKey)){
                PreCapture();
                initializeCubeMap();
                ScreenShot(camera, Camera.MonoOrStereoscopicEye.Left); //All I wanted was stereo Eyes ;(
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

        private void ScreenShot(Camera cam, Camera.MonoOrStereoscopicEye eye) {
            ModHelper.Console.WriteLine("Taking 360 ScreenShot from the " + eye + " eye", MessageType.Success);
            cam.stereoSeparation = ModHelper.Config.GetSettingsValue<float>("IPD");
            cam.stereoEnabled
            
            cam.RenderToCubemap(CubeMap, 63, eye);
            RenderTexture equirect = new RenderTexture(CubeMap.width, CubeMap.height/2, 16, RenderTextureFormat.Default);
            CubeMap.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Mono);

            Texture2D screenshot = new Texture2D(equirect.width, equirect.height, TextureFormat.ARGB32, false);
            
            RenderTexture _old = RenderTexture.active;
            RenderTexture.active = equirect;
            screenshot.ReadPixels(new Rect(0,0, equirect.width, equirect.height), 0, 0);
            screenshot.Apply();

            RenderTexture.active = _old;
            equirect.Release();

            byte[] bytes = screenshot.EncodeToPNG();
            string directory = ModHelper.Config.GetSettingsValue<string>("SavePath");
            string path = directory + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "." + eye + ".png";
            if (!Directory.Exists(directory)){
                ModHelper.Console.WriteLine("Directory does not exist");
                DirectoryInfo info = Directory.CreateDirectory(directory);
                ModHelper.Console.WriteLine("Creating New Directory at " + info.FullName);
            }

            File.WriteAllBytes(path, bytes);
            ModHelper.Console.WriteLine("Saved ScreenShot to " + path, MessageType.Success);
        }

        //Stolen from Cheats and Debug mod lol
        private bool GetKeyDown(Key keyCode) {
            return Keyboard.current[keyCode].wasPressedThisFrame;
        }
    }
}
