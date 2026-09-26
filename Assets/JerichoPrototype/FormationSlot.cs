using UnityEngine;
namespace JerichoPrototype
{
    public sealed class FormationSlot : MonoBehaviour
    {
        public Vector3 localOffset;
        [Range(0, 1)] public int phaseGroup;
    }
}
