using UnityEditor;
using UnityEngine;

namespace AllIn13DShader
{
	public class TriplanarEffectDrawer : AbstractEffectDrawer
	{
		private int mainTexPropertyIndex;
		private EffectProperty mainNormalMapProperty;
		
		public TriplanarEffectDrawer(EffectProperty mainNormalMapProperty, AllIn13DShaderInspectorReferences references, 
			PropertiesConfig propertiesConfig) : base(references, propertiesConfig)
		{
			this.drawerID = Constants.TRIPLANAR_EFFECT_DRAWER_ID;
			this.mainNormalMapProperty = mainNormalMapProperty;

			mainTexPropertyIndex = FindPropertyIndex("_MainTex");
		}

		protected override void DrawProperties()
		{
			bool isTriplanarNoiseTransitionEnabled = references.IsKeywordEnabled("_TRIPLANAR_NOISE_TRANSITION_ON");

			MaterialProperty matProperty = references.matProperties[mainTexPropertyIndex];

			EffectPropertyDrawer.DrawProperty(
				materialProperty: matProperty,
				labelPrefix: string.Empty,
				displayName: matProperty.displayName,
				customValue: string.Empty,
				allowReset: true, 
				isKeywordProperty: false,
				propertyType: EffectProperty.PropertyType.BASIC,
				references: references);

			EffectProperty triplanarNoiseTransitionToggleProperty = effectConfig.effectProperties[6];

			DrawProperty(effectConfig.effectProperties[0]);
			DrawProperty(effectConfig.effectProperties[1]);

			if (IsEffectPropertyVisible(mainNormalMapProperty, references.targetMatInfos))
			{
				EditorUtils.DrawLine(Color.grey, 1, 3);
				DrawProperty(mainNormalMapProperty, false);
				DrawProperty(effectConfig.effectProperties[2]);
			}

			EditorUtils.DrawLine(Color.grey, 1, 3);
			DrawProperty(effectConfig.effectProperties[3]);
			DrawProperty(effectConfig.effectProperties[4]);

			EditorUtils.DrawLine(Color.grey, 1, 3);
			DrawProperty(effectConfig.effectProperties[5]);
			DrawProperty(triplanarNoiseTransitionToggleProperty);
			
			if (isTriplanarNoiseTransitionEnabled)
			{
				DrawProperty(effectConfig.effectProperties[7]);
				DrawProperty(effectConfig.effectProperties[8]);
			}
		}
	}
}