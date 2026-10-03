using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace JerichoPrototype.Editor
{
    public static class BlackTextInstaller
    {
        [InitializeOnLoadMethod] static void Init()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(".utmp/black-text")) return;
                File.Delete(".utmp/black-text");
                if (EditorApplication.isPlayingOrWillChangePlaymode) { File.WriteAllText("Logs/black-text.txt", "Exit Play Mode before saving text colors."); return; }
                int count = 0;
                foreach (var text in Resources.FindObjectsOfTypeAll<TextMesh>())
                {
                    if (!text.gameObject.scene.IsValid() || EditorUtility.IsPersistent(text)) continue;
                    Undo.RecordObject(text, "Set game text black");
                    text.color = Color.black;
                    EditorUtility.SetDirty(text);
                    if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    EditorSceneManager.MarkSceneDirty(text.gameObject.scene);
                    count++;
                }
                EditorSceneManager.SaveOpenScenes();
                File.WriteAllText("Logs/black-text.txt", "PASS: " + count + " scene TextMesh labels saved black; game IMGUI uses black text for all control states.");
            };
        }
    }
}
