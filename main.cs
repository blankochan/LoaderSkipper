using MelonLoader;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players.Scaling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Il2CppTMPro;
using UnityEngine;

namespace LoaderSkipper
{
	public class main : MelonMod
	{
		private GameObject status;
		private string currentScene = "Loader";
		private bool sceneChanged = false;
		private bool init = false;
		private string[] fileText;
		private string FILEPATH = @"UserData\LoaderSkipper";
		private string FILENAME = @"OverrideLastTpose.txt";
		private static string FILEPATHLastTPose = @"UserData\LoaderSkipper\LastTpose.txt";
		private bool tPoseSet = false;
		private static Transform[] measurements = new Transform[4];
		private GameObject head, lController, rController, root;
		private PlayerMeasurement playerMeasurement;
		private bool measurementGot = false;
		private bool loading = false;

		public override void OnFixedUpdate()
		{
			if (sceneChanged)
			{
				try
                {
                    if (currentScene == "Loader")
                    {
                        status = GameObject.Find("________________SCENE_________________/Text/Measuring/TextCanvas/Status");
                        init = true;
                    }
				}
				catch
				{
					return;
				}
				if (currentScene == "Gym")
				{
					if (!measurementGot && PlayerManager.instance.localPlayer != null && PlayerManager.instance.localPlayer.Data != null)
					{
						playerMeasurement = PlayerManager.instance.localPlayer.Data.PlayerMeasurement;
						measurementGot = true;
                    }
                    else
                    {
						return;
                    }
				}
				if (!tPoseSet && (currentScene == "Gym"))
				{
					SetTPose();
					tPoseSet = true;
                }
                sceneChanged = false;
            }
			if (measurementGot && ((playerMeasurement.ArmSpan != PlayerManager.instance.localPlayer.Data.PlayerMeasurement.ArmSpan) || (playerMeasurement.Length != PlayerManager.instance.localPlayer.Data.PlayerMeasurement.Length)))
			{
				StoreTPose();
				playerMeasurement = PlayerManager.instance.localPlayer.Data.PlayerMeasurement;
			}
			if (init && !loading && (currentScene == "Loader") && (status.GetComponent<TextMeshProUGUI>().text == "Ready to RUMBLE!"))
			{
				loading = true;
				MelonCoroutines.Start(StartGymLoad());
			}
		}

		private void Log (string msg)
		{
			MelonLogger.Msg(msg);
		}

		public IEnumerator StartGymLoad()
		{
			yield return new WaitForSeconds(2);
            SceneManager.instance.PerformStartupGymLoad();
			yield break;
		}
		
		public void SetTPose()
        {
            try
			{
				fileText = File.ReadAllLines($"{FILEPATH}\\{FILENAME}");
				if (fileText.Length < 12)
                {
					MelonLogger.Msg("Override not Set, Loading Last T-Pose");
					fileText = File.ReadAllLines(FILEPATHLastTPose);
                }
                else
                {
					MelonLogger.Msg("Override Found, Loading Override Measurements");
                }
				head = new GameObject();
				head.transform.position = new Vector3(float.Parse(fileText[0]), float.Parse(fileText[1]), float.Parse(fileText[2]));
				lController = new GameObject();
				lController.transform.position = new Vector3(float.Parse(fileText[3]), float.Parse(fileText[4]), float.Parse(fileText[5]));
				rController = new GameObject();
				rController.transform.position = new Vector3(float.Parse(fileText[6]), float.Parse(fileText[7]), float.Parse(fileText[8]));
				root = new GameObject();
				root.transform.position = new Vector3(float.Parse(fileText[9]), float.Parse(fileText[10]), float.Parse(fileText[11]));
                PlayerManager.instance.localPlayer.Data.SetMeasurement(PlayerMeasurement.Create(head.transform, lController.transform, rController.transform, root.transform, true));
				playerMeasurement = PlayerManager.instance.localPlayer.Data.PlayerMeasurement;
				GameObject.Destroy(head);
				GameObject.Destroy(lController);
				GameObject.Destroy(rController);
				GameObject.Destroy(root);
				MelonLogger.Msg("TPose Loaded: " + playerMeasurement.ToString());
			} catch (Exception e)
            {
				MelonLogger.Error("Error Setting TPose, Please T-Pose In Game: " + e);
            }
		}

		public static void StoreTPose()
		{
			try
            {
                measurements[0] = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(2).GetChild(0).GetChild(0);
                measurements[1] = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(2).GetChild(1).GetChild(0);
                measurements[2] = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(2).GetChild(2).GetChild(0);
                measurements[3] = PlayerManager.instance.localPlayer.Controller.gameObject.transform.GetChild(2).GetChild(3);
                string[] textToStore = new string[12];
                textToStore[0] = measurements[0].position.x.ToString();
                textToStore[1] = measurements[0].position.y.ToString();
                textToStore[2] = measurements[0].position.z.ToString();
                textToStore[3] = measurements[1].position.x.ToString();
                textToStore[4] = measurements[1].position.y.ToString();
                textToStore[5] = measurements[1].position.z.ToString();
                textToStore[6] = measurements[2].position.x.ToString();
                textToStore[7] = measurements[2].position.y.ToString();
                textToStore[8] = measurements[2].position.z.ToString();
                textToStore[9] = measurements[3].position.x.ToString();
                textToStore[10] = (measurements[3].position.y - 0.06f).ToString();
                textToStore[11] = measurements[3].position.z.ToString();
                WriteToFile(textToStore, FILEPATHLastTPose);
            }
			catch (Exception e)
			{
				MelonLogger.Error(e);
			}
		}

        public override void OnLateInitializeMelon()
		{
			MelonCoroutines.Start(CheckIfFileExists(FILEPATH, FILENAME));
		}

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			if (sceneName == "Loader")
			{
				loading = false;
			}
			currentScene = sceneName;
			sceneChanged = true;
		}

		public static void WriteToFile(string[] text, string outputFile)
		{
			if (File.Exists(outputFile))
			{
				try
				{
					List<string> output = new List<string>();
					File.WriteAllLines(outputFile, text);
				}
				catch (Exception e) { MelonLogger.Error(e); }
			}
			else { MelonLogger.Error($"File not Found: Check Game Folder for {outputFile}"); }
		}

		public IEnumerator CheckIfFileExists(string filePath, string fileName)
		{
			if (!File.Exists($"{filePath}\\{fileName}") || !File.Exists(FILEPATHLastTPose))
			{
				if (!Directory.Exists(filePath))
				{
					MelonLogger.Msg($"Folder Not Found, Creating Folder: {filePath}");
					Directory.CreateDirectory(filePath);
				}
				if (!File.Exists($"{filePath}\\{fileName}"))
				{
					MelonLogger.Msg($"Creating File {filePath}\\{fileName}");
					File.Create($"{filePath}\\{fileName}");
				}
				if (!File.Exists(FILEPATHLastTPose))
                {
					MelonLogger.Msg($"Creating File {FILEPATHLastTPose}");
					File.Create(FILEPATHLastTPose);
				}
			}
			else
			{
				fileText = ReadFileText($"{filePath}\\{fileName}");
			}
			yield return null;
		}

		public string[] ReadFileText(string inputFile)
		{
			if (File.Exists(inputFile))
			{
				try
				{
					List<string> output = new List<string>();
					return File.ReadAllLines(inputFile);
				}
				catch (Exception e) { MelonLogger.Error(e); }
			}
			else { MelonLogger.Error($"File not Found: Check Game Folder for {inputFile}"); }
			return null;
		}
	}
}