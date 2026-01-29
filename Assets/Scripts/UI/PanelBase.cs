using UnityEngine;

namespace UI
{
	public abstract class PanelBase : MonoBehaviour
	{
		public virtual void Show(bool isActive)
		{
			gameObject.SetActive(isActive);
		}

		public abstract void OnOpenHandler();
	}
}


