using UnityEngine;
namespace JerichoPrototype
{
    public sealed class TrumpetCollectible : MonoBehaviour
    {
        public TrumpetSearchGame owner;
        public bool Collected { get; private set; }
        public bool Revealed { get; private set; }
        Renderer[] visuals;
        GameObject glow;
        void Awake() { visuals = GetComponentsInChildren<Renderer>(true); ResetCollectible(); }
        public void Reveal()
        {
            if (Collected) return;
            if (visuals == null) visuals = GetComponentsInChildren<Renderer>(true);
            Revealed = true;
            foreach (var visual in visuals) if (visual) visual.enabled = true;
            if (!glow)
            {
                glow = new GameObject("Solved clue glow");
                glow.transform.SetParent(transform, false);
                var ring = glow.AddComponent<LineRenderer>();
                ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 40;
                ring.widthMultiplier = .09f;
                ring.material = new Material(Shader.Find("Sprites/Default"));
                ring.startColor = ring.endColor = new Color(1f, .8f, .08f);
                for (int i = 0; i < 40; i++)
                {
                    float a = i * Mathf.PI * 2 / 40;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.05f, .4f, Mathf.Sin(a) * 1.05f));
                }
                var light = glow.AddComponent<Light>();
                light.color = new Color(1f, .8f, .15f); light.range = 3; light.intensity = 2;
            }
            glow.SetActive(true);
        }
        public void MarkCollected() { Collected = true; gameObject.SetActive(false); }
        public void ResetCollectible()
        {
            Collected = false; Revealed = false; gameObject.SetActive(true);
            if (visuals == null) visuals = GetComponentsInChildren<Renderer>(true);
            foreach (var visual in visuals) if (visual) visual.enabled = false;
            if (glow) glow.SetActive(false);
        }
        void OnDestroy()
        {
            if (glow)
            {
                var material = glow.GetComponent<LineRenderer>().sharedMaterial;
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            }
        }
    }
}
