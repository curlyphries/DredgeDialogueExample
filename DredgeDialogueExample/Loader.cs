using UnityEngine;

namespace DredgeDialogueExample
{
	public class Loader
	{
		/// <summary>
		/// This method is run by Winch to initialize your mod
		/// </summary>
		public static void Initialize()
		{
			var gameObject = new GameObject(nameof(DredgeDialogueExample));
			gameObject.AddComponent<DredgeDialogueExample>();
			GameObject.DontDestroyOnLoad(gameObject);
		}
	}
}