using System;
using UnityEngine;

namespace JerichoPrototype
{
    /// <summary>Black text for game IMGUI without changing the shared Unity skin.</summary>
    public static class BlackGameText
    {
        static GUISkin blackSkin;
        public static IDisposable Scope()
        {
            var previous = GUI.skin;
            if (!blackSkin)
            {
                blackSkin = UnityEngine.Object.Instantiate(previous);
                blackSkin.hideFlags = HideFlags.HideAndDontSave;
                blackSkin.box.normal.background = Texture2D.whiteTexture;
                foreach (GUIStyle style in blackSkin)
                {
                    style.normal.textColor = Color.black;
                    style.hover.textColor = Color.black;
                    style.active.textColor = Color.black;
                    style.focused.textColor = Color.black;
                    style.onNormal.textColor = Color.black;
                    style.onHover.textColor = Color.black;
                    style.onActive.textColor = Color.black;
                    style.onFocused.textColor = Color.black;
                }
            }
            GUI.skin = blackSkin;
            return new RestoreSkin(previous);
        }
        sealed class RestoreSkin : IDisposable
        {
            readonly GUISkin previous;
            public RestoreSkin(GUISkin skin) { previous = skin; }
            public void Dispose() { GUI.skin = previous; }
        }
    }
}
