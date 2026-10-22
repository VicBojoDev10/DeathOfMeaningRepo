using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public class URPSettingsDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "URP Settings";

		private URPSettings urpSettings;
		private URPSettingsUserPref urpSettingsUserPref; 
		
		public URPSettingsDrawer(CommonStyles commonStyles, AllIn13DShaderWindow parentWindow) : base(commonStyles, parentWindow)
		{
			this.urpSettings = EditorUtils.FindAsset<URPSettings>(URPSettings.ASSET_NAME);
			this.urpSettingsUserPref = EditorUtils.FindAsset<URPSettingsUserPref>(URPSettingsUserPref.ASSET_NAME);
		}

		public override void Draw()
		{
			GUILayout.Label("Shader Feature Configuration", commonStyles.bigLabel);
			EditorGUILayout.HelpBox("Enable/disable shader features to optimize compilation time and editor performance. \n" +
			                        "Hover each feature to get a more detailed tooltip.", MessageType.Info);
			
			GUILayout.Space(5);

			for(int i = 0; i < urpSettings.configs.Length; i++)
			{
				EditorGUILayout.BeginHorizontal();

				URPFeatureConfig urpFeatureConfig = urpSettings.configs[i];
				URPFeatureUserPref urpFeatureUserPref = urpSettingsUserPref.FindPreferenceByID(urpFeatureConfig.shaderDefine);
				if(urpFeatureUserPref == null)
				{
					continue;
				}


				GUIContent toggleContent = new GUIContent(urpFeatureConfig.displayName, urpFeatureConfig.tooltip);
				urpFeatureUserPref.enabled = EditorGUILayout.ToggleLeft(toggleContent, urpFeatureUserPref.enabled);
				
				EditorGUILayout.EndHorizontal();
			}
			
			GUILayout.Space(10);
			
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button("Apply Changes"))
			{
				ApplyFeatureChanges();
			}
			
			if(GUILayout.Button("Reset to Defaults"))
			{
				ResetToDefaults();
			}
			EditorGUILayout.EndHorizontal();
			
			GUILayout.Space(20);
			EditorUtils.DrawThinLine();
			
			GUILayout.Label("Configure AllIn13D to work correctly with URP", commonStyles.bigLabel);
			if (GUILayout.Button("Configure"))
			{
#if ALLIN13DSHADER_URP
				URPConfigurator.Configure(forceConfigure: true);
#endif
			}
		}

		public override void Hide()
		{

		}

		public override void Show()
		{
		
		}

		public override void OnDisable()
		{
		
		}

		public override void OnEnable()
		{
		
		}

		public override void EnteredPlayMode()
		{
		
		}

		private void ApplyFeatureChanges()
		{
			URPDefinesFileCreator.CreateFile(urpSettings, urpSettingsUserPref);
			AssetDatabase.Refresh();
		}

		private void ResetToDefaults()
		{

			for(int i = 0; i < urpSettingsUserPref.preferences.Length; i++)
			{
				URPFeatureUserPref preference = urpSettingsUserPref.preferences[i];
				URPFeatureConfig urpFeatureConfig = urpSettings.FindConfigByID(preference.id);

				urpSettingsUserPref.preferences[i].Init(urpFeatureConfig);
			}
		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}
	}
}