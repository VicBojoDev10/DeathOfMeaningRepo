using UnityEngine;

namespace AllIn13DShader
{
	[CreateAssetMenu(menuName = "AllIn13D/URP Settings")]
	public class URPSettings : ScriptableObject
	{
		public const string ASSET_NAME = "URPSettings";

		public URPFeatureConfig[] configs;

		public URPFeatureConfig FindConfigByID(string id)
		{
			URPFeatureConfig res = null;

			for(int i = 0; i < configs.Length; i++)
			{
				if(configs[i].shaderDefine == id)
				{
					res = configs[i];
					break;
				}
			}

			return res;
		}
	}
}