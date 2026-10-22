using UnityEngine;
namespace JerichoPrototype
{
    public sealed class TrumpetCollectible : MonoBehaviour
    {
        public TrumpetSearchGame owner;
        public bool Collected { get; private set; }
        public void MarkCollected() { Collected = true; gameObject.SetActive(false); }
        public void ResetCollectible() { Collected = false; gameObject.SetActive(true); }
    }
}
