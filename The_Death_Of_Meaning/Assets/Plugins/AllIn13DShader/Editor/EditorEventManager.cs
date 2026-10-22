using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public class EditorEventManager : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
		{
			bool assetImportedForTheFirstTime = IsAssetImportedForTheFirstTime(importedAssets);
			if (assetImportedForTheFirstTime)
			{
				Debug.Log("Setting up AllIn13DShader for the first time...");
			}

#if ALLIN13DSHADER_URP
			URPConfigurator.CheckURPRemoved(deletedAssets, didDomainReload);
#endif
			GlobalConfiguration.InitIfNeeded();

			PropertiesConfigCollection propertiesConfigCollection = PropertiesConfigCreator.InitIfNeeded(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);
			//GlobalConfiguration.SetPropertiesConfigCollection(propertiesConfigCollection);
			GlobalConfiguration.SetupShadersReferences();

			EffectsProfileCollection effectsProfileCollection = EditorUtils.FindAsset<EffectsProfileCollection>(EffectsProfileCollection.ASSET_NAME);
			if (effectsProfileCollection == null)
			{
				effectsProfileCollection = EffectsProfileCollection.CreateAsset(propertiesConfigCollection);
			}

			effectsProfileCollection.BindEffectConfigs(propertiesConfigCollection.propertiesConfig);

			effectsProfileCollection.CleanInvalidProfiles();
			effectsProfileCollection.CheckBakedShadersFolder(propertiesConfigCollection.propertiesConfig);

			for (int i = 0; i < deletedAssets.Length; i++)
			{
				if (deletedAssets[i].EndsWith(".shader"))
				{
					effectsProfileCollection.CheckRemovedShader(deletedAssets[i]);
				}
			}

			if (assetImportedForTheFirstTime)
			{
				ShaderFeaturesFileCreator.CreateFile(effectsProfileCollection.generalProfile);
			}

			GlobalConfiguration.instance.SetEffectsProfileCollection(effectsProfileCollection);
			GlobalConfiguration.instance.shaderPassCollection = EditorUtils.FindAsset<ShaderPassCollection>("ShaderPassCollection");

			URPSettingsUserPref urpSettingsUserPrefs = URPSettingsUserPref.InitIfNeeded();
			GlobalConfiguration.instance.urpSettingsUserPref = urpSettingsUserPrefs;

#if ALLIN13DSHADER_URP
			URPConfigurator.AllAssetProcessed();
			AllIn13DShaderWindow.AllAssetProcessed();
#endif
		}

		private static bool IsAssetImportedForTheFirstTime(string[] importedAssets)
		{
			bool res = false;
			for(int i = 0; i < importedAssets.Length; i++)
			{
				if (importedAssets[i].EndsWith(Constants.MAIN_ASSEMBLY_NAME))
				{
					res = true;
					break;
				}
			}

			return res;
		}
	}
} 