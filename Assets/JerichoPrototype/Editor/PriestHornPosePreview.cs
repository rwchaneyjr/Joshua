#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JerichoPrototype.Editor
{
    /// <summary>Creates an editor-only image of the full horn hold pose for visual QA.</summary>
    public static class PriestHornPosePreview
    {
        const string RequestPath = ".utmp/priest-horn-pose-preview.request";
        const string OutputPath = "Logs/priest-horn-pose-preview.png";

        [InitializeOnLoadMethod]
        static void CheckRequest()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                File.Delete(RequestPath);
                RenderHoldPose();
            };
        }

        [MenuItem("Tools/Jericho Prototype/Horn Tests/Render Hold Pose Preview")]
        public static void RenderHoldPose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JerichoPrototype/Priest.prefab");
            if (!prefab) throw new System.InvalidOperationException("Priest prefab is missing.");

            var priest = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var cameraObject = new GameObject("Horn Pose Preview Camera");
            var texture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            try
            {
                priest.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                var animator = priest.GetComponentInChildren<Animator>();
                var hornBlow = priest.GetComponent<PriestHornBlow>();
                if (!animator || !hornBlow) throw new System.InvalidOperationException("Priest preview requires Animator and PriestHornBlow.");

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(.1f);
                hornBlow.BlowHorn();
                hornBlow.DebugAdvance(.55f); // exact full hold pose

                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var horn = typeof(PriestHornBlow).GetField("horn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(hornBlow) as Transform;
                var focus = (head.position + chest.position) * .5f;
                CreateMarker(head.position, Color.red, priest.transform);
                CreateMarker(hand.position, Color.green, priest.transform);
                if (horn)
                {
                    CreateMarker(horn.position, Color.blue, priest.transform);
                    foreach (var renderer in horn.GetComponentsInChildren<Renderer>())
                        renderer.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, .52f, .05f, 1f) };
                }
                Debug.Log($"PRIEST HORN PREVIEW MARKERS head={head.position}, hand={hand.position}, horn={horn?.position}");
                var camera = cameraObject.AddComponent<Camera>();
                camera.backgroundColor = new Color(.14f, .17f, .21f, 1f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.fieldOfView = 30f;
                camera.nearClipPlane = .01f;
                camera.transform.position = focus + new Vector3(1.55f, .25f, -2.8f);
                camera.transform.LookAt(focus + new Vector3(0f, .05f, 0f));
                camera.targetTexture = texture;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white;
                camera.Render();

                var prior = RenderTexture.active;
                RenderTexture.active = texture;
                var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes(OutputPath, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                RenderTexture.active = prior;
                Debug.Log("PRIEST HORN VISUAL PREVIEW: " + OutputPath);
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(priest);
            }
        }

        static Transform FindHorn(Transform root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == "Ceremonial Horn") return node;
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (node.name.ToLowerInvariant().Contains("horn")) return node;
            return null;
        }

        static void CreateMarker(Vector3 position, Color color, Transform parent)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.SetParent(parent, true);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * .045f;
            marker.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = color };
        }
    }
}
#endif
