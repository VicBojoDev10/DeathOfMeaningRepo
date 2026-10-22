namespace AllIn13DShader
{
	[System.Serializable]
	public class URPFeatureUserPref
	{
		public string id;
		public bool enabled;

		public URPFeatureUserPref(URPFeatureConfig config)
		{
			Init(config);
		}

		public void Init(URPFeatureConfig config)
		{
			this.id = config.shaderDefine;
			this.enabled = config.defaultValue;
		}
	}
}