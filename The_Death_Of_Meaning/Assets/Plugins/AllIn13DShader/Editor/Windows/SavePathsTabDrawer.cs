using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public class SavePathsTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Save Paths";

		public SavePathsTabDrawer(CommonStyles commonStyles, AllIn13DShaderWindow parentWindow) : base(commonStyles, parentWindow)
		{}

		public override void OnEnable()
		{

		}

		public override void OnDisable()
		{

		}

		public override void Show()
		{

		}

		public override void Hide()
		{

		}

		public override void EnteredPlayMode()
		{

		}

		public override void Draw()
		{
			GUILayout.Label("Material Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where new Materials will be saved when the Save Material To Folder button of the asset component is pressed", EditorStyles.boldLabel);
			GlobalConfiguration.instance.MaterialSavePath = EditorUtils.DrawSelectorFolder(GlobalConfiguration.instance.MaterialSavePath, "New Material Folder");

			EditorUtils.DrawThinLine();
			GUILayout.Label("Render Material to Image Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Rendered Image Texture Scale", GUILayout.MaxWidth(190));
			GlobalConfiguration.instance.RenderImageScale = EditorGUILayout.Slider(GlobalConfiguration.instance.RenderImageScale, 0.2f, 5f, GUILayout.MaxWidth(200));
			if (GUILayout.Button("Default Value", GUILayout.MaxWidth(100)))
			{
				GlobalConfiguration.instance.RenderImageScale = 1f;
			}
			EditorGUILayout.EndHorizontal();
			
			GlobalConfiguration.instance.RenderImageSavePath = EditorUtils.DrawSelectorFolder(GlobalConfiguration.instance.RenderImageSavePath, "New Images Folder");
			EditorUtils.DrawThinLine();

			GUILayout.Label("Baked Shader Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GUILayout.Label("Select the folder where baked shaders will be saved when the Bake Shader Keywords button of the material inspector is pressed", EditorStyles.boldLabel);
			GlobalConfiguration.instance.BakedShaderSavePath = EditorUtils.DrawSelectorFolder(GlobalConfiguration.instance.BakedShaderSavePath, "New Bake Shader Folder");
			EditorUtils.DrawThinLine();

			GUILayout.Label("Gradients Save Path", commonStyles.bigLabel);
			GUILayout.Space(20);
			GlobalConfiguration.instance.GradientsSavePath = EditorUtils.DrawSelectorFolder(GlobalConfiguration.instance.GradientsSavePath, "Gradients Folder");
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}
	}
}