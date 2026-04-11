using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SevenDaysForKids
{
    /// <summary>
    /// Replaces loading screen background images with solid black.
    /// XML overrides all failed because the XUi controller loads textures
    /// via Unity Addressables at runtime, bypassing XML config.
    /// This patch intercepts after the loading screen opens and blacks out
    /// the background image using multiple fallback strategies.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_LoadingScreen), "OnOpen")]
    public static class LoadingScreenPatch
    {
        private static Texture2D _blackTex;

        static void Postfix(XUiC_LoadingScreen __instance)
        {
            EnsureBlackTexture();

            // Strategy 1: Use reflection to access m_ImageFullScreen field
            if (TryBlackoutField(__instance))
                return;

            // Strategy 2: Find all XUiV_Texture views via the controller's view tree
            if (TryBlackoutViewComponent(__instance))
                return;

            // Strategy 3: Nuclear — find all UITexture NGUI components under the window
            TryBlackoutUITextures(__instance);
        }

        private static void EnsureBlackTexture()
        {
            if (_blackTex != null)
                return;

            _blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _blackTex.SetPixel(0, 0, Color.black);
            _blackTex.Apply();
            Object.DontDestroyOnLoad(_blackTex);
        }

        private static bool TryBlackoutField(XUiC_LoadingScreen instance)
        {
            // XUiC_LoadingScreen has m_ImageFullScreen (found via DLL metadata scan)
            FieldInfo field = typeof(XUiC_LoadingScreen).GetField("m_ImageFullScreen",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
                return false;

            object val = field.GetValue(instance);
            if (val == null)
                return false;

            // If it's a XUiV_Texture view
            if (val is XUiV_Texture texView)
            {
                texView.Texture = _blackTex;
                return true;
            }

            // If it's a UITexture NGUI component
            if (val is UITexture uiTex)
            {
                uiTex.mainTexture = _blackTex;
                return true;
            }

            // Unknown type — try setting mainTexture via reflection
            PropertyInfo texProp = val.GetType().GetProperty("mainTexture",
                BindingFlags.Instance | BindingFlags.Public);
            if (texProp != null && texProp.CanWrite)
            {
                texProp.SetValue(val, _blackTex);
                return true;
            }

            return false;
        }

        private static bool TryBlackoutViewComponent(XUiC_LoadingScreen instance)
        {
            XUiView root = instance.viewComponent;
            if (root == null || root.UiTransform == null)
                return false;

            // Find XUiV_Texture components in the transform hierarchy
            XUiV_Texture[] texViews = root.UiTransform.GetComponentsInChildren<XUiV_Texture>(true);
            if (texViews == null || texViews.Length == 0)
                return false;

            foreach (XUiV_Texture tv in texViews)
            {
                tv.Texture = _blackTex;
            }
            return true;
        }

        private static void TryBlackoutUITextures(XUiC_LoadingScreen instance)
        {
            XUiView root = instance.viewComponent;
            if (root == null || root.UiTransform == null)
                return;

            UITexture[] textures = root.UiTransform.GetComponentsInChildren<UITexture>(true);
            if (textures == null)
                return;

            foreach (UITexture tex in textures)
            {
                if (tex.mainTexture != null && tex.mainTexture != _blackTex)
                {
                    tex.mainTexture = _blackTex;
                }
            }
        }
    }
}
