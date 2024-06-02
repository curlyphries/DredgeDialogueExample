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

			WinchCore.Log.Debug("Success!");
		}

		public void Inject()
		{
			DialogueLoader.Inject();
        }
	}
}
