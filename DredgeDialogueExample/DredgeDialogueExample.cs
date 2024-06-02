using UnityEngine;
using Winch.Core;

namespace DredgeDialogueExample
{
	public class DredgeDialogueExample : MonoBehaviour
	{   
		public void Awake()
		{
			WinchCore.Log.Debug($"{nameof(DredgeDialogueExample)} has loaded!");

			DialogueLoader.Load();

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
        }
	}
}
