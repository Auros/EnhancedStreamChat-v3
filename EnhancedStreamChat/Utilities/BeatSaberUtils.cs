using BeatSaberMarkupLanguage;
using System.Linq;
using TMPro;
using UnityEngine;

namespace EnhancedStreamChat.Utilities
{
    public static class BeatSaberUtils
	{
		private static Material? _noGlow;
		public static Material? UINoGlowMaterial => _noGlow ??= new Material(Resources.FindObjectsOfTypeAll<Material>().Where(m => m.name == "UINoGlow").FirstOrDefault());

		private static Shader? _tmpNoGlowFontShader;
		public static Shader? TMPNoGlowFontShader
        {
			get
            {
				if (_tmpNoGlowFontShader == null)
                {
					_tmpNoGlowFontShader = BeatSaberUI.MainTextFont.material.shader;
				}
				return _tmpNoGlowFontShader;
            }
        }

		// DaNike to the rescue 
		public static bool TryGetTMPFontByFamily(string family, out TMP_FontAsset font)
		{
			if (FontManager.TryGetTMPFontByFamily(family, out font))
			{
				font.material.shader = TMPNoGlowFontShader;
				return true;
			}

			return false;
		}
	}
}