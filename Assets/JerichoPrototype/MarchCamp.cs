using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JerichoPrototype
{
    // Runtime-only camp cutaway: authored scene objects and lighting are restored on exit.
    public sealed class MarchCamp : MonoBehaviour
    {
        public bool Playing { get; private set; }
        public float Hour { get; private set; } = 9;
        Light sun;
        Quaternion originalRotation;
        Color originalColor, originalAmbient;
        float originalIntensity, originalAmbientIntensity;
        Light originalSun;
        Material originalSky, daySky;
        bool initialized;
        GameObject camp;
        Camera campCamera;
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<Behaviour,bool> paused = new Dictionary<Behaviour,bool>();
        readonly Dictionary<ProcessionFollower,bool> followers = new Dictionary<ProcessionFollower,bool>();
        float fade;
        string caption;
        bool peopleAdded;
        void AddCampPriests()
        {
            if (peopleAdded) return;
            peopleAdded=true;
            int index=0;
            foreach(var priest in FindObjectsOfType<PriestHornBlow>().Where(p=>p.gameObject.scene==gameObject.scene).Take(4))
            {
                Vector3 spot=camp.transform.position+new Vector3(-5+index*3,0,-3);
                var person=new GameObject("Camp priest "+(index+1));person.transform.SetParent(camp.transform);person.transform.position=spot;
                foreach(var skin in priest.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if(!skin.enabled || !skin.sharedMesh)continue;
                    var mesh=new Mesh();skin.BakeMesh(mesh);owned.Add(mesh);
                    var visual=new GameObject("Priest at camp");visual.transform.SetParent(person.transform);
                    visual.transform.position=spot+skin.transform.position-priest.transform.position;
                    visual.transform.rotation=skin.transform.rotation;visual.transform.localScale=skin.transform.lossyScale;
                    visual.AddComponent<MeshFilter>().sharedMesh=mesh;
                    visual.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                }
                var renderers=person.GetComponentsInChildren<MeshRenderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    person.transform.localScale*=1.85f/Mathf.Max(.01f,bounds.size.y);
                    bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    person.transform.position+=spot-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                }
                index++;
            }
        }
        public void Initialize(Vector3 center)
        {
            if (initialized) return;
            initialized = true;
            originalSun = RenderSettings.sun;
            originalSky=RenderSettings.skybox;
            var skyShader=Shader.Find("Skybox/Procedural");
            if(skyShader){daySky=new Material(skyShader);owned.Add(daySky);daySky.SetFloat("_SunSize",.065f);RenderSettings.skybox=daySky;}
            sun = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional && l.gameObject.scene == gameObject.scene && l.enabled);
            originalAmbient = RenderSettings.ambientLight; originalAmbientIntensity = RenderSettings.ambientIntensity;
            if (sun) { originalRotation = sun.transform.rotation; originalColor = sun.color; originalIntensity = sun.intensity; RenderSettings.sun = sun; }
            camp = new GameObject("March camp (runtime)");
            camp.transform.position = center + new Vector3(0,0,-82);
            var canvas = Material(new Color(.65f,.48f,.27f));
            var wood = Material(new Color(.22f,.12f,.06f));
            var flame = Material(new Color(1,.4f,.04f));
            flame.EnableKeyword("_EMISSION"); flame.SetColor("_EmissionColor",new Color(2,.5f,.02f));
            for (int i=0;i<6;i++)
            {
                Vector3 p = new Vector3((i%3-1)*8,0,(i/3)*8);
                // Two sloped canvas roofs form an open A-frame tent.
                Part("Tent canvas",p+new Vector3(-1.1f,1.6f,0),new Vector3(3.3f,.12f,4.5f),canvas,Quaternion.Euler(0,0,48));
                Part("Tent canvas",p+new Vector3(1.1f,1.6f,0),new Vector3(3.3f,.12f,4.5f),canvas,Quaternion.Euler(0,0,-48));
                Part("Tent pole",p+new Vector3(0,1.5f,-2),new Vector3(.12f,3,.12f),wood,Quaternion.identity);
            }
            for(int i=0;i<5;i++) Part("Firewood",new Vector3(0,.2f,-6),new Vector3(2,.2f,.25f),wood,Quaternion.Euler(0,i*36,0));
            Part("Campfire",new Vector3(0,.6f,-6),new Vector3(.7f,.9f,.7f),flame,Quaternion.Euler(0,45,0),PrimitiveType.Sphere);
            var fire = new GameObject("Firelight"); fire.transform.SetParent(camp.transform,false); fire.transform.localPosition=new Vector3(0,1.4f,-6);
            var light=fire.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.45f,.12f);light.intensity=3;light.range=12;
            var cameraObject = new GameObject("Camp camera"); cameraObject.transform.SetParent(camp.transform,false);
            campCamera=cameraObject.AddComponent<Camera>(); campCamera.depth=100; campCamera.farClipPlane=400;
            campCamera.transform.localPosition=new Vector3(25,8,-9);
            campCamera.transform.LookAt(camp.transform.position+new Vector3(0,2,2));
            camp.SetActive(false); SetDayProgress(0);
        }
        Material Material(Color color) { var m=new Material(Shader.Find("Standard")){color=color}; owned.Add(m);return m; }
        void Part(string name,Vector3 position,Vector3 scale,Material material,Quaternion rotation,PrimitiveType type=PrimitiveType.Cube)
        {
            var p=GameObject.CreatePrimitive(type);p.name=name;p.transform.SetParent(camp.transform,false);p.transform.localPosition=position;p.transform.localScale=scale;p.transform.localRotation=rotation;
            p.GetComponent<Renderer>().sharedMaterial=material;var collider=p.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        }
        public void SetDayProgress(float progress) { SetHour(Mathf.Lerp(9,18.5f,Mathf.Clamp01(progress))); }
        void SetHour(float hour)
        {
            Hour=hour;
            float t=Mathf.InverseLerp(9,18.5f,hour);
            float dusk=Mathf.SmoothStep(0,1,Mathf.InverseLerp(15,18.5f,hour));
            if(daySky){daySky.SetColor("_SkyTint",Color.Lerp(new Color(.5f,.5f,.5f),new Color(.65f,.32f,.24f),dusk));daySky.SetFloat("_Exposure",Mathf.Lerp(1.1f,.65f,dusk));}
            if(sun)
            {
                // East to west, low at sunset; procedural sky uses this directional light.
                float elevation=hour<=12?Mathf.Lerp(35,70,Mathf.InverseLerp(9,12,hour)):Mathf.Lerp(70,2,Mathf.InverseLerp(12,18.5f,hour));
                sun.transform.rotation=Quaternion.Euler(elevation,Mathf.Lerp(270,90,t),0);
                float evening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(15,18.5f,hour));
                sun.color=Color.Lerp(originalColor,new Color(1,.37f,.13f),evening);
                sun.intensity=Mathf.Lerp(originalIntensity,.25f,evening);
                RenderSettings.ambientIntensity=Mathf.Lerp(originalAmbientIntensity,.35f,evening);
            }
        }
        public IEnumerator Show(int day,bool evening,Transform player)
        {
            Playing=true;
            AddCampPriests();
            var controller=player?player.GetComponent<JoshuaController>():null;
            if(controller){Pause(controller);if(controller.animator){controller.animator.SetInteger("TestState",1);controller.animator.SetFloat("Speed",0);}}
            foreach(var c in FindObjectsOfType<ThirdPersonCamera>().Where(c=>c.target==player))Pause(c);
            foreach(var f in FindObjectsOfType<ProcessionFollower>().Where(f=>f.gameObject.scene==gameObject.scene)){followers[f]=f.MovementSuppressed;f.SetMovementSuppressed(true);}
            foreach(var r in FindObjectsOfType<ArkProcession.ArkCarryRig>().Where(r=>r.gameObject.scene==gameObject.scene))Pause(r);
            caption=evening?"Day "+day+" — returning to camp":"Day "+day+" — preparing to march";
            yield return Fade(0,1,.65f);
            camp.SetActive(true); SetHour(evening?17:9);
            yield return Fade(1,0,.65f);
            for(float elapsed=0;elapsed<4;elapsed+=Time.unscaledDeltaTime)
            {
                if(evening)SetHour(Mathf.Lerp(17,18.5f,elapsed/4));
                yield return null;
            }
            yield return Fade(0,1,1);
            if(evening)
            {
                caption="Night at camp";yield return new WaitForSecondsRealtime(1.2f);
                caption="Day "+(day+1)+" — morning";SetHour(9);
                yield return Fade(1,0,1);yield return new WaitForSecondsRealtime(1.5f);yield return Fade(0,1,.65f);
            }
            camp.SetActive(false);SetHour(9);caption="Day "+(evening?day+1:day)+" — march around Jericho";
            yield return Fade(1,0,.65f);
            RestoreControls();
        }
        IEnumerator Fade(float from,float to,float duration)
        { for(float t=0;t<duration;t+=Time.unscaledDeltaTime){fade=Mathf.Lerp(from,to,t/duration);yield return null;}fade=to; }
        void Pause(Behaviour component){paused[component]=component.enabled;component.enabled=false;}
        void RestoreControls()
        {
            foreach(var p in paused)if(p.Key)p.Key.enabled=p.Value;
            foreach(var p in followers)if(p.Key)p.Key.SetMovementSuppressed(p.Value);
            paused.Clear();followers.Clear();Playing=false;fade=0;if(camp)camp.SetActive(false);
        }
        void OnGUI()
        {
            if(!Playing)return;
            int depth=GUI.depth;GUI.depth=-100;
            Color old=GUI.color;GUI.color=new Color(0,0,0,fade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            var style=new GUIStyle(GUI.skin.label){fontSize=26,alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.black;
            GUI.color=Color.white;GUI.DrawTexture(new Rect(Screen.width*.15f,Screen.height*.82f,Screen.width*.7f,55),Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(new Rect(Screen.width*.15f,Screen.height*.82f,Screen.width*.7f,55),caption,style);GUI.depth=depth;
        }
        void OnDisable(){StopAllCoroutines();RestoreControls();RestoreLight();}
        void RestoreLight(){if(!initialized)return;if(sun){sun.transform.rotation=originalRotation;sun.color=originalColor;sun.intensity=originalIntensity;}RenderSettings.skybox=originalSky;RenderSettings.sun=originalSun;RenderSettings.ambientLight=originalAmbient;RenderSettings.ambientIntensity=originalAmbientIntensity;}
        void OnDestroy(){RestoreLight();if(camp)Destroy(camp);foreach(var item in owned)if(item)Destroy(item);}
    }
}
