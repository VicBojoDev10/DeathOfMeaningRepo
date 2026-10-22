using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public static class URPDefinesFileCreator
	{
		public static void CreateFile(URPSettings urpSettings, URPSettingsUserPref userPref)
		{
			string urpDefinesFilePath = Path.Combine(GlobalConfiguration.instance.RootPluginPath, "Shaders/ShaderLibrary/AllIn13DShader_FeaturesURP_Defines.hlsl");
			string hlslLibraryTemplatePath = Path.Combine(GlobalConfiguration.instance.RootPluginPath, "Editor/Templates/HLSLLibrary_Template.allIn13DTemplate");

			if (!File.Exists(urpDefinesFilePath))
			{
				Debug.LogError("URP defines file not found");
				return;
			}

			if (!File.Exists(hlslLibraryTemplatePath))
			{
				Debug.LogError("HLSL library template not found");
				return;
			}

			string fileText = File.ReadAllText(hlslLibraryTemplatePath);
			string content = string.Empty;
			for (int i = 0; i < userPref.preferences.Length; i++)
			{
				if (userPref.preferences[i].enabled)
				{
					string line = string.Format(Constants.DEFINE_LINE_FORMAT, urpSettings.configs[i].shaderDefine);

					content += line;
					content += "\n";
				}
			}

			fileText = fileText.Replace("<LIBRARY_DEFINE>", "ALLIN13DSHADER_FEATURESURP_DEFINES");
			fileText = fileText.Replace("<CONTENT>", content);

			fileText = EditorUtils.UnifyEOL(fileText);
			File.WriteAllText(urpDefinesFilePath, fileText);

			EditorUtility.SetDirty(userPref);
		}
	}
}