using System;
using HarmonyLib;
using UnityEngine;
using Winch.Core;
using Yarn;

namespace DredgeDialogueAPI
{
	public class DredgeDialogueAPI : MonoBehaviour
	{   
		public void Awake()
		{
			WinchCore.Log.Debug($"{nameof(DredgeDialogueAPI)} has loaded!");
            try
            {
                DialogueLoader.LoadDialogues();
            } catch(Exception ex)
            {
                WinchCore.Log.Error(ex);
            }

            GameManager.Instance.OnGameStarted += GameStarted;

            WinchCore.Log.Debug("Success!");
		}

		public void GameStarted()
		{
			Inject();
		}

		public void Inject()
		{
			DialogueLoader.Inject();
            try
            {
                DialogueLoader.AddInstruction("TravellingMerchant_ChatOptions", 1, Instruction.Types.OpCode.AddOption, "line:01f8b99", "L84shortcutoption_TravellingMerchant_ChatOptions_6", 0, false);
                DialogueLoader.AddInstruction("TravellingMerchant_ChatOptions", 2, Instruction.Types.OpCode.AddOption, "line:01f8b99", "L84shortcutoption_TravellingMerchant_ChatOptions_6", 0, false);
            } catch (Exception e)
            {
                WinchCore.Log.Error(e);
            }
        }
    }
}
