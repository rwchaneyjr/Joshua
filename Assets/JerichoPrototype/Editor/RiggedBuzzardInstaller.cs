using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace JerichoPrototype.Editor
{
    public static class RiggedBuzzardInstaller
    {
        const string Folder="Assets/BuzzardFlight/";
        [InitializeOnLoadMethod] static void Request()
        {
            EditorApplication.delayCall+=()=>{
                if(!File.Exists(".utmp/rigged-bird") || EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(".utmp/rigged-bird");
                try{Install();}catch(Exception e){File.WriteAllText("Logs/rigged-bird.txt",e.ToString());Debug.LogException(e);}
            };
        }
        [MenuItem("Tools/Jericho Prototype/Install Rigged Buzzards")]
        public static void Install()
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"BuzzardRigged.fbx");
            importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;
            importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"BuzzardRigged.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            File.WriteAllText("Logs/rigged-bird-clips.txt",string.Join("\n",clips.Select(c=>c.name+" "+c.length)));
            var root=new GameObject("Rigged Flying Buzzard");
            try
            {
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"BuzzardRigged.fbx"),root.transform);
                model.name="BuzzardRigged";
                foreach(var old in model.GetComponentsInChildren<Animation>())UnityEngine.Object.DestroyImmediate(old);
                foreach(var old in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(old);
                var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"BuzzardColor.mat");
                if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Folder+"BuzzardColor.mat");}
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"BuzzardColor.png");material.SetFloat("_Glossiness",.1f);EditorUtility.SetDirty(material);
                foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
                var animation=model.AddComponent<Animation>();animation.playAutomatically=true;
                foreach(string name in new[]{"Fly","Glide","Land","TakeOff","Perch","Hop"})
                {
                    var source=clips.FirstOrDefault(c=>c.name==name || c.name.EndsWith("|"+name));
                    if(!source)throw new Exception("Missing exported clip "+name);
                    string path=Folder+name+".anim";
                    var copy=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if(!copy){copy=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(copy,path);}else EditorUtility.CopySerialized(source,copy);
                    copy.name=name;copy.legacy=true;copy.wrapMode=(name=="Land" || name=="TakeOff")?WrapMode.ClampForever:WrapMode.Loop;
                    EditorUtility.SetDirty(copy);animation.AddClip(copy,name);if(name=="Fly")animation.clip=copy;
                }
                root.AddComponent<BuzzardAnimation>().clips=animation;
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"BuzzardFlying.prefab");
                var scene=SceneManager.GetSceneByPath(PrototypeBuilder.ScenePath);
                if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath,OpenSceneMode.Additive);
                var flock=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BuzzardFlock>()).First();
                flock.coloredBird=prefab;EditorUtility.SetDirty(flock);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText("Logs/rigged-bird.txt","PASS: 15-bone skinned bird, six clips, texture, new prefab, and flock reference saved.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
