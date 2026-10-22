using UnityEngine;

namespace AllIn13DShader
{
	[System.Serializable]
	public class URPFeatureConfig
	{
		public string shaderDefine;
		public bool defaultValue;
		[TextArea] public string displayName;
		[TextArea] public string tooltip;
	}
}