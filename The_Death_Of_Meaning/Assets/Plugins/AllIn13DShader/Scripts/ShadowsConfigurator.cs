using UnityEngine;

namespace AllIn13DShader
{
	[ExecuteInEditMode]
	public class ShadowsConfigurator : MonoBehaviour
	{
		public Color shadowColor = Color.black;
		public bool updateEveryFrame = false;

		private readonly int shadowColorPropID = Shader.PropertyToID("global_shadowColor");

		private void OnEnable()
		{
			SetupShadowColor();
		}

		public void Update()
		{
#if UNITY_EDITOR
			UpdateEditor();
#else
			UpdateRuntime();
#endif
		}

		private void UpdateEditor()
		{
			SetupShadowColor();
		}

		private void UpdateRuntime()
		{
			if (updateEveryFrame)
			{
				SetupShadowColor();
			}
		}

		public void SetupShadowColor()
		{
			Shader.SetGlobalColor(shadowColorPropID, shadowColor);
		}
	}
}