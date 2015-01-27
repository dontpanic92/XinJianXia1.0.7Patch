using System;
using UnityEngine;


public class InjectedClass
{
	// Fields
	public static bool CtrlFlag;
	public static bool isCtrlKeyDown;
	public static bool isEscKeyDown;
	public static bool isF12KeyDown;
	public static bool isPaused;
	public static bool SaturationCorrectionFlag;
	public static Transform LastNearCameraTransform;
	public static float LastNearCameraDesiredDistance;
	public static Quaternion LastCameraQuaternion;

	public static bool TM_Enable = true;
	public static bool SSAO_Enable = true;
	public static bool Bloom_Enable = true;
	public static bool CE_Enable = true;
	public static bool CS_Enable = true;

	private static IniHelper iniHelper = null;

	// Methods
	public static void CheckAndLockCursor()
	{
		if (Input.GetKeyUp(KeyCode.LeftControl))
		{
			isCtrlKeyDown = false;
		}
		if (Input.GetKeyDown(KeyCode.LeftControl) && !isCtrlKeyDown)
		{
			if (CtrlFlag)
			{
				FightUIInterface.ShowPromptText("原始操作模式", 1, 1f);
				CtrlFlag = false;
			}
			else
			{
				FightUIInterface.ShowPromptText("新增操作模式", 1, 1f);
				CtrlFlag = true;
			}
			isCtrlKeyDown = true;
		}
		if (CtrlFlag)
		{
			GameStateManager.GameState state = Main.stateMgr.getCurrState();
			if (!MainMenu.InjectedField && ((state == GameStateManager.GameState.Fighting) || (state == GameStateManager.GameState.Normal)))
			{
				Screen.lockCursor = true;
				Screen.showCursor = false;
				return;
			}
		}
		Screen.lockCursor = false;
		Screen.showCursor = true;
	}

	public static void CheckTogglePauseGame()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			isEscKeyDown = false;
		}
		if (!isEscKeyDown && Input.GetKeyDown(KeyCode.Escape))
		{
			isPaused = !isPaused;
			isEscKeyDown = true;
			if (isPaused)
			{
				FightUIInterface.ShowPromptText("暂停", 1, 1f);
				Time.timeScale = 0f;
			}
			else
			{
				FightUIInterface.ShowPromptText("继续", 1, 1f);
				Time.timeScale = 1f;
			}
		}
	}

	public static Quaternion CalcCameraQuaternion(Actor actor)
	{
		Vector3 actorEuler = actor.moveCtrl.GetTransform ().eulerAngles;
		Vector3 euler = LastCameraQuaternion.eulerAngles;
		euler.y = actorEuler.y;
		euler.z = actorEuler.z;
		return Quaternion.Euler(euler);
	}

	public static void CheckToggleSaturationCorrection()
	{
		GameObject obj2 = GameObject.FindWithTag("MainCamera");
		if (obj2 != null)
		{
			if (Input.GetKeyUp(KeyCode.F12))
			{
				isF12KeyDown = false;
			}
			if (!isF12KeyDown && Input.GetKeyDown(KeyCode.F12))
			{
				if(iniHelper == null)
					iniHelper = new IniHelper (Application.dataPath + "/Managed/new.ini");

				TM_Enable = (iniHelper.ReadValue ("Graphics Enhance", "ToneMapping") == "1") ? true : false;
				SSAO_Enable = (iniHelper.ReadValue ("Graphics Enhance", "SSAO") == "1") ? true : false;
				CE_Enable = (iniHelper.ReadValue ("Graphics Enhance", "ContrastEnhance") == "1") ? true : false;
				CS_Enable = (iniHelper.ReadValue ("Graphics Enhance", "ContrastStretch") == "1") ? true : false;
				Bloom_Enable = (iniHelper.ReadValue ("Graphics Enhance", "Bloom") == "1") ? true : false;

				Tonemapping TM = obj2.GetComponent<Tonemapping> ();
				SSAOEffect SSAO = obj2.GetComponent<SSAOEffect>();
				ContrastEnhance CE = obj2.GetComponent<ContrastEnhance> ();
				BloomAndLensFlares bloom = obj2.GetComponent<BloomAndLensFlares> ();
				ContrastStretchEffect CS = obj2.GetComponent<ContrastStretchEffect> ();

				QualitySettings.antiAliasing = 4;

				if (CS == null) 
				{
					CS = obj2.AddComponent<ContrastStretchEffect> ();
					CS.shaderAdapt = Shader.Find ("Hidden/Contrast Stretch Adaptation");
					CS.shaderApply = Shader.Find ("Hidden/Contrast Stretch Apply");
					CS.shaderLum = Shader.Find ("Hidden/Contrast Stretch Luminance");
					CS.shaderReduce = Shader.Find ("Hidden/Contrast Stretch Reduction");
				}

				if (bloom == null) 
				{
					bloom = obj2.AddComponent<BloomAndLensFlares> ();
					bloom.addBrightStuffOneOneShader = Shader.Find ("Hidden/BlendOneOne");
					bloom.brightPassFilterShader = Shader.Find ("Hidden/BrightPassFilterForBloom");
					bloom.hollywoodFlaresShader = Shader.Find ("Hidden/MultipassHollywoodFlares");
					bloom.lensFlareShader = Shader.Find ("Hidden/LensFlareCreate");
					bloom.screenBlendShader = Shader.Find ("Hidden/Blend");
					bloom.separableBlurShader = Shader.Find ("Hidden/SeparableBlurPlus");
					bloom.vignetteShader = Shader.Find ("Hidden/VignetteShader");
				}

				if (TM == null) 
				{
					TM = obj2.AddComponent<Tonemapping> ();
					TM.tonemapper = Shader.Find ("Hidden/Tonemapper");
					TM.type = Tonemapping.TonemapperType.AdaptiveReinhardAutoWhite;
				}
				if (SSAO == null) 
				{
					SSAO = obj2.AddComponent<SSAOEffect>();
					SSAO.m_SSAOShader = Shader.Find ("Hidden/SSAO");
					obj2.camera.farClipPlane = 1000;
				}

				FightUIInterface.uiInterface.cheatWindow.Open ();
				//Main.actorMgr.SwitchControlActor ();

				if (CE == null) 
				{
					CE = obj2.AddComponent<ContrastEnhance> ();
					CE.contrastCompositeShader = Shader.Find ("Hidden/ContrastComposite");
					CE.separableBlurShader = Shader.Find ("Hidden/SeparableBlurPlus");
				}

				isF12KeyDown = true;
				if (!SaturationCorrectionFlag)
				{
					FightUIInterface.ShowPromptText("画面增强打开", 1, 1f);
					SaturationCorrectionFlag = true;

					if (TM) 
					{
						TM.enabled = TM_Enable;
					}

					if (SSAO) 
					{
						SSAO.enabled = SSAO_Enable;
						SSAO.m_Radius = 0.8f;
						SSAO.m_SampleCount = SSAOEffect.SSAOSamples.Medium;
						SSAO.m_OcclusionIntensity = 2.4f;
						SSAO.m_Downsampling = 2;
						SSAO.m_MinZ = 0.8f;

					}
					if (CE)
					{
						CE.enabled = CE_Enable;
					}
					if (bloom) {
						bloom.enabled = Bloom_Enable;
					}

					if (CS) 
					{
						CS.enabled = CS_Enable;
					}
				}
				else
				{
					FightUIInterface.ShowPromptText("画面增强关闭", 1, 1f);
					SaturationCorrectionFlag = false;
					//component.enabled = false;
					if(SSAO)
						SSAO.enabled = false;
					if (CE)
						CE.enabled = false;
					if (TM) 
						TM.enabled = false;
					if (bloom)
						bloom.enabled = false;
					if (CS)
						CS.enabled = false;
				}
				//FightUIInterface.ShowPromptText (SSAO_Enable ? "SSAO ON" : "SSAO OFF");
			}
		}

		//FightUIInterface.ShowPromptText (Application.unityVersion.ToString ());
	}
}
