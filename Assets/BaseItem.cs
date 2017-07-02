using UnityEngine; 
using System;
using UnityEngine.UI; 

public class BaseItemVO
{
	public string _name; 
}

public class BaseItem : MonoBehaviour
{
	[NonSerialized] public BaseItemVO vo; 

	public virtual void Set()
	{
		_name = vo._name; 
		SetInfos(); 
	}

	public virtual void Clear()
	{
		ClearInfos(); 
	}


	#region Infos

	[NonSerialized] public string _name; 
	[SerializeField] Text nameText; 

	void SetInfos()
	{
		nameText.text = _name; 
	}

	void ClearInfos()
	{
		nameText.text = null; 
		_name = null; 
	}

	public float GetTextHeight()
	{
		return nameText.preferredHeight; 
	}
	#endregion
}