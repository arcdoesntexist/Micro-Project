using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ControllerSceneTests
    {
        private AppSession app;
        private UsbFixtureFeed feed;
        private ControllerInput input;
        private bool hadPreference;
        private int preference;
        private static T Field<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        [SetUp] public void SetUp()
        {
            var runner=GameObject.Find("Code-based tests runner");if(runner!=null)Object.DontDestroyOnLoad(runner);
            hadPreference=PlayerPrefs.HasKey(ControllerInput.PreferenceKey);preference=PlayerPrefs.GetInt(ControllerInput.PreferenceKey);
            input=ControllerInput.EnsureExists();input.ReleaseRound();input.SelectMode(ControllerMode.Keyboard);
            var values=new Dictionary<string,string>();var root=new GameObject("USB scene app");root.SetActive(false);app=root.AddComponent<AppSession>();
            app.Initialize(new ProfileStore(key=>values.TryGetValue(key,out var value)?value:null,(key,value)=>values[key]=value,()=>{}));root.SetActive(true);
            var profile=app.Store.Create("USB demo");app.Select(profile.PlayerId,DifficultyId.Easy);
            var feedRoot=new GameObject("Fixture USB stream");Object.DontDestroyOnLoad(feedRoot);feed=feedRoot.AddComponent<UsbFixtureFeed>();
            feed.Connection=input.Connection;feed.Template=File.ReadAllText(Path.Combine(Application.dataPath,"CPS/Tests/Fixtures/Usb/valid-v1.json")).Trim();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(feed!=null)feed.Running=false;
            var idle=SceneManager.CreateScene("USB test idle");SceneManager.SetActiveScene(idle);
            var loaded=new List<Scene>();for(int i=0;i<SceneManager.sceneCount;i++){var scene=SceneManager.GetSceneAt(i);if(scene.name=="MainMenu"||scene.name=="Gameplay")loaded.Add(scene);}
            foreach(var scene in loaded)yield return SceneManager.UnloadSceneAsync(scene);
            if(feed!=null)Object.DestroyImmediate(feed.gameObject);
            if(input!=null){input.ReleaseRound();input.SelectMode(ControllerMode.Keyboard);var serial=input.Connection;Object.DestroyImmediate(input.gameObject);if(serial!=null)Object.DestroyImmediate(serial.gameObject);}
            if(app!=null)Object.DestroyImmediate(app.gameObject);
            if(hadPreference)PlayerPrefs.SetInt(ControllerInput.PreferenceKey,preference);else PlayerPrefs.DeleteKey(ControllerInput.PreferenceKey);
        }
        [UnityTest] public IEnumerator ControllerPanelPracticeAndUsbLossUseTheSameSelectedSource()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            var menu=Object.FindFirstObjectByType<MainMenuUI>();var panel=menu.GetComponent<ControllerPanel>();var start=Field<Button>(menu,"startButton");
            Assert.That(start.interactable,Is.True);panel.Show();input.SelectMode(ControllerMode.FSR);yield return null;
            Assert.That(start.interactable,Is.False);Assert.That(panel.IsOpen,Is.True);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})yield return Capture("home-fsr",size);
            int generation=input.Connection.BeginSession();input.Connection.TransportOpened(generation);feed.Running=true;feed.Emit();yield return null;
            panel.Hide();Assert.That(input.CanPlay(out _),Is.True);
            var practice=menu.GetComponent<MenuPracticeUI>();var mascot=Field<RectTransform>(practice,"mascot");float previous=mascot.anchoredPosition.x;
            practice.StepPractice(input.Read(),.05f);Assert.That(mascot.anchoredPosition.x,Is.LessThan(previous));
            input.SelectMode(ControllerMode.LoadCell);input.Connection.SendCommand+=_=>{};Assert.That(input.Connection.RequestTare(),Is.True);feed.TareId=2;feed.Emit();yield return null;
            Assert.That(input.CanPlay(out _),Is.True);Assert.That(menu.StartSelectedGame(),Is.True);
            float deadline=Time.realtimeSinceStartup+8;while(SceneManager.GetActiveScene().name!="Gameplay"&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;var flow=Object.FindFirstObjectByType<GameSessionController>();Assert.That(flow.Router.SelectedMode,Is.EqualTo(ControllerMode.LoadCell));
            input.SendMessage("OnApplicationFocus",true);feed.Emit();Assert.That(flow.BeginRound(),Is.True,flow.InputIssue);flow.Step(3);feed.Emit();flow.Step(.01f);Assert.That(flow.LastInput.Left.ForceN,Is.Not.Null);Assert.That(flow.LastInput.Left.HasStrength,Is.False);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})yield return Capture("load-gameplay",size);
            feed.Running=false;input.Connection.OnWebSerialStatus(input.Connection.ConnectionGeneration+":error:USB disconnected");yield return null;
            Assert.That(flow.IsPaused,Is.True);Assert.That(flow.CanResume,Is.False);Assert.That(flow.Model.Health.CurrentHp,Is.EqualTo(3));
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})yield return Capture("sensor-paused",size);
            var ui=Object.FindFirstObjectByType<GameplayUI>();Field<ControllerPanel>(ui,"controllerPanel").Show(flow);yield return null;
            Assert.That(Field<ControllerPanel>(ui,"controllerPanel").IsOpen,Is.True);Assert.That(Field<Button>(ui,"resumeButton").interactable,Is.False);
        }
        private static IEnumerator Capture(string label,Vector2Int size)
        {
            var camera=Camera.main;var viewport=camera.GetComponent<GameplayViewport>();var target=new RenderTexture(size.x,size.y,24);
            var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;bool enabled=viewport.enabled;viewport.enabled=false;
            try
            {
                camera.targetTexture=target;viewport.Apply(size.x,size.y);yield return null;yield return null;Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                try
                {
                    RenderTexture.active=target;image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                    string directory=System.Environment.GetEnvironmentVariable("CPS_USB_CAPTURE_DIR");
                    if(!string.IsNullOrEmpty(directory)){Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,label+"-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());}
                }
                finally{Object.DestroyImmediate(image);}
            }
            finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;viewport.enabled=enabled;Object.DestroyImmediate(target);}
        }
    }
}
