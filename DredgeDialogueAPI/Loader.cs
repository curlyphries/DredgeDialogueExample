using UnityEngine;

namespace DredgeDialogueAPI
{
	public class Loader
	{
		/// <summary>
		/// This method is run by Winch to initialize your mod
		/// </summary>
		public static void Initialize()
		{
			var gameObject = new GameObject(nameof(DredgeDialogueAPI));
			gameObject.AddComponent<DredgeDialogueAPI>();
			GameObject.DontDestroyOnLoad(gameObject);
		}
	}
}
