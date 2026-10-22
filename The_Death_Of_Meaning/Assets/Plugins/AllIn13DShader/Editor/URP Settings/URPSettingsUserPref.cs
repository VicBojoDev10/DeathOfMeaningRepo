using System.IO;
using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public class URPSettingsUserPref : ScriptableObject
	{
		public const string ASSET_NAME = "URPSettingsUserPref";

		public URPFeatureUserPref[] preferences;

		public void Init(URPSettings urpSettings)
		{
			preferences = new URPFeatureUserPref[urpSettings.configs.Length];

			for(int i = 0; i < urpSettings.configs.Length; i++)
			{
				preferences[i] = new URPFeatureUserPref(urpSettings.configs[i]);
			}
		}

		public static URPSettingsUserPref InitIfNeeded()
		{
			URPSettingsUserPref res = null;

			res = EditorUtils.FindAsset<URPSettingsUserPref>(ASSET_NAME);
			
			if (res == null) 
			{
				URPSettings urpSettings = EditorUtils.FindAsset<URPSettings>(URPSettings.ASSET_NAME);

				res = ScriptableObject.CreateInstance<URPSettingsUserPref>(); 
				res.Init(urpSettings);

				string filePath = Path.Combine(GlobalConfiguration.instance.GlobalConfigFolderPath, ASSET_NAME + ".asset");
				AssetDatabase.CreateAsset(res, filePath);

				URPDefinesFileCreator.CreateFile(urpSettings, res);

				AssetDatabase.Refresh();
			}
			else
			{
				URPSettings urpSettings = EditorUtils.FindAsset<URPSettings>(URPSettings.ASSET_NAME);

				if (res.preferences.Length != urpSettings.configs.Length)
				{
					res.MatchWithURPSettings(urpSettings);
				}
			}

			return res;
		}

		public void MatchWithURPSettings(URPSettings urpSettings)
		{
			for(int i = preferences.Length - 1; i >= 0; i--)
			{
				URPFeatureConfig urpFeatureConfig = urpSettings.FindConfigByID(preferences[i].id);
				if(urpFeatureConfig == null)
				{
					ArrayUtility.RemoveAt(ref preferences, i);
				}
			}

			for(int i = 0; i < urpSettings.configs.Length; i++)
			{
				URPFeatureUserPref userPref = FindPreferenceByID(urpSettings.configs[i].shaderDefine);

				if(userPref == null)
				{
					ArrayUtility.Add(ref preferences, new URPFeatureUserPref(urpSettings.configs[i]));
				}
			}
		}

		public URPFeatureUserPref FindPreferenceByID(string id)
		{
			URPFeatureUserPref res = null;

			for(int i = 0; i < preferences.Length; i++)
			{
				if (preferences[i].id == id)
				{
					res = preferences[i];
					break;
				}
			}

			return res;
		}
	}
}