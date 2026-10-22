using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JerichoPrototype.Editor
{
    public static class BuzzardFlockInstaller
    {
        [InitializeOnLoadMethod] static void Request()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/buzzard-install") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/buzzard-install");
                try{Install();}catch(Exception e){File.WriteAllText("Logs/buzzard-install.txt",e.ToString());}
            };
        }
        [MenuItem("Tools/Jericho Prototype/Install Buzzard Flock")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first.");
            var scene=SceneManager.GetSceneByPath(PrototypeBuilder.ScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath,OpenSceneMode.Additive);
            var all=scene.GetRootGameObjects();
            var flock=all.SelectMany(g=>g.GetComponentsInChildren<BuzzardFlock>()).FirstOrDefault();
            if(!flock){var go=new GameObject("Buzzards over Jericho");SceneManager.MoveGameObjectToScene(go,scene);flock=go.AddComponent<BuzzardFlock>();}
            flock.coloredBird=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CHARACTERS/BUZZARDWCOLOR.prefab");
            if(!flock.coloredBird)throw new Exception("Colored buzzard prefab missing.");
            flock.gameplayCamera=all.SelectMany(g=>g.GetComponentsInChildren<ThirdPersonCamera>()).First().GetComponent<Camera>();
            flock.birdCount=18;flock.cityCenter=Vector3.zero;
            EditorUtility.SetDirty(flock);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/buzzard-install.txt","PASS: colored prefab connected; 18 birds, six foreground flight lanes; scene saved.");
        }
    }
}
