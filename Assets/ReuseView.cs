using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// --使用说明--
// 1 都要向左上角对齐

public class ReuseView : MonoBehaviour
{
	#region Main

	[NonSerialized] public BaseItemVO[] _vos;

	public void Set()
	{
		if (_vos == null || _vos.Length == 0)
		{
			Debug.Log("vos is empty! "); 
			return; 
		}
		SetPrefab(); 
		SetScrollRect(); 
		SetParent(); 

		Action a = () =>
			{
				CalcItemHeight(); 
				float parentHeight = CalcParentHeight(); 
				OnContent(parentHeight); 
				OnSetScrollRect();  
				int prefabsCount = CalcPrefabsCount(); 
				CalcParentAnchors(prefabsCount); 
				SetItems(prefabsCount); 
				CalcEdgePos();
			}; 
		if (_minItemHeight == 0)
		{
			SetWait(a); 
		}
		else
		{
			a(); 
		}
	}

	public void Clear()
	{
		ClearWait(); 
		ClearEdge(); 
		ClearItems(); 
		ClearParent(); 
		ClearScrollRect(); 
		ClearPrefab(); 
		_vos = null; 
	}

	#endregion



	#region Prefab

	[SerializeField] BaseItem _itemPrefab;
	// 最小高度Prefab

	void SetPrefab()
	{
		_itemPrefab.Clear(); 
		// 如果处于未激活状态，是否可以用rect来获取高度？
		_itemPrefab.gameObject.SetActive(true); // 为了后面获取itemPrefab的高度，必须激活
	}

	void ClearPrefab()
	{
		_itemPrefab.Clear(); 
		_itemPrefab.gameObject.SetActive(false); 
	}

	#endregion



	#region Items

	[NonSerialized] float _minItemHeight;
	List<BaseItem> _items = new List<BaseItem>();

	void SetItems(int prefabsCount)
	{
		// 创建新的items
		for (int i = 0; i < prefabsCount; i++)
		{
			var item = GameObject.Instantiate(_itemPrefab); 
			item.gameObject.SetActive(true); 
			item.transform.SetParent(_parentRtf); 
			item.transform.localScale = Vector3.one; 
			item.vo = _vos[_curIndex + i]; 
			item.Set(); 
			_items.Add(item); 
		}
	}

	void ClearItems()
	{
		for (int i = 0, count = _items.Count; i < count; i++)
		{
			var item = _items[i]; 
			item.Clear(); 
			GameObject.Destroy(item.gameObject); 
		}
		_items.Clear(); 
	}

	void CalcItemHeight()
	{
		_minItemHeight = (_itemPrefab.transform as RectTransform).rect.height; 
		Debug.Log("_minItemHeight: " + _minItemHeight); 
	}

	int CalcPrefabsCount()
	{
		int prefabsCount = Mathf.CeilToInt(_viewPortRtf.rect.height / _minItemHeight) + 1; // 屏幕所能显示的最大数量
		Debug.Log("prefabsCount: " + prefabsCount + ", sizeDelta: " + _viewPortRtf.rect); 
		bool isLess = prefabsCount < _vos.Length; 
		return isLess ? prefabsCount : _vos.Length; 
	}

	void OnSetItems()
	{
		for (int i = 0, count = _items.Count; i < count; i++)
		{
			var item = _items[i]; 
			item.Clear(); 
			item.vo = _vos[_curIndex + i]; 
			item.Set(); 
		}
	}

	void OnChangeIndex(float yTop, float yBottom, out int index)
	{
		index = -1; 
		float minGapTop = 0 - _parentAnchorPoses[_parentAnchorPoses.Length - 1]; // TODO 没有开堆不允许进入这里
		float minGapBottom = 0 - _parentBottomPoses[_parentBottomPoses.Length - 1]; // TODO 没有开堆不允许进入这里

		if (yTop >= _topItemEdgePos) // 如果滑动后，顶部超过parent的顶部，那么向上移动parent
		{	
			Debug.LogError("Error enter yTop area!!! "); 
			for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
			{
				float gap = _parentAnchorPoses[i] - yTop; // 只能算比contentRtf的上边缘y坐标大的点
				if (gap >= 0 && gap < minGapTop)
				{
					minGapTop = gap; 
					index = i; 
				} 
			}
			if ((int)(yTop) == 0)
			{
				index = 0; 
			}
		}
		else if (yBottom <= _bottomItemEdgePos) // 如果滑动后，底部超过parent的底部，那么向下移动parent
		{
			for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
			{
				float gap = yBottom - _parentBottomPoses[i]; // 只能算比contentRtf的下边缘y坐标小的点
				if (gap > 0 && gap < minGapBottom)
				{
					minGapBottom = gap; 
					index = i; 
				} 
				if ((int)(gap) == 0)
				{
					index = _parentAnchorPoses.Length - 1; 
				}
			}
		}
		else
		{
			// 滑动后没有超过范围就不用变化parent的位置
		}
	}

	#endregion



	#region Parent

	[SerializeField] RectTransform _parentRtf;
	float[] _parentAnchorPoses;
	float[] _parentBottomPoses;
	float[] itemHeights;

	void SetParent()
	{
		_parentRtf.anchorMax = Vector2.up; 
		_parentRtf.anchorMin = Vector2.up; 
	}

	void ClearParent()
	{
		_parentRtf.anchoredPosition = Vector2.zero;
	}

	float CalcParentHeight()
	{
		float parentHeight = 0; 
		float heightWithoutText = _minItemHeight - _itemPrefab.GetTextHeight(); 
		itemHeights = new float[_vos.Length]; 
		for (int i = 0, count = _vos.Length; i < count; i++)
		{
			_itemPrefab.vo = _vos[i]; 
			_itemPrefab.Set();
			float height = heightWithoutText + _itemPrefab.GetTextHeight(); 
			parentHeight += height + layoutGroup.spacing; 
			itemHeights[i] = height; 
			_itemPrefab.Clear(); 
		}
		_itemPrefab.gameObject.SetActive(false); 
		parentHeight -= layoutGroup.spacing; 
		Debug.Log("parentHeight: " + parentHeight); 
		return parentHeight; 
	}

	void CalcParentAnchors(int prefabsCount)
	{
		bool isLess = prefabsCount < _vos.Length; 
		int len = isLess ? (_vos.Length - prefabsCount + 1) : 0; // parent的anchorPos只可能在这个范围内变动
		_parentAnchorPoses = new float[len]; 
		_parentBottomPoses = new float[len]; 
		Debug.Log("len: " + len); 
		float _topItemEdgePos = 0; 
		float _bottomItemEdgePos = 0; 
		for (int j = 0; j < prefabsCount; j++)
		{
			_bottomItemEdgePos -= itemHeights[j] + layoutGroup.spacing;
		}
		_bottomItemEdgePos += layoutGroup.spacing; 

		for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
		{	
			_parentAnchorPoses[i] = _topItemEdgePos; 
			_parentBottomPoses[i] = _bottomItemEdgePos; 
			Debug.LogFormat("_parentAnchorPoses[{0}]: {1}", i, _parentAnchorPoses[i]); 
			Debug.LogFormat("_parentBottomPoses[{0}]: {1}", i, _parentBottomPoses[i]); 

			_topItemEdgePos -= itemHeights[i] + layoutGroup.spacing; 
			if (i + prefabsCount >= itemHeights.Length)
			{
				break; 
			}
			_bottomItemEdgePos -= layoutGroup.spacing + itemHeights[i + prefabsCount]; 
		}
	}

	void OnChangeParentAnchor()
	{
		if (_curIndex < 0 || _curIndex >= _parentAnchorPoses.Length)
		{
			Debug.LogError("index out of range! "); 
		}
		_parentRtf.anchoredPosition = new Vector2(0, _parentAnchorPoses[_curIndex]); 
	}

	#endregion



	#region Scroll Rect

	[NonSerialized] public float targetPos = 1;
	[SerializeField] ScrollRect scrollRect;
	[SerializeField] RectTransform _viewPortRtf;
	[SerializeField] RectTransform _contentRtf;
	float _contentPosRange;

	void SetScrollRect()
	{
		_contentRtf.anchorMax = Vector2.up; 
		_contentRtf.anchorMin = Vector2.up; 
	}

	void ClearScrollRect()
	{
		targetPos = scrollRect.verticalNormalizedPosition; 

		scrollRect.onValueChanged.RemoveAllListeners(); 
		scrollRect.verticalNormalizedPosition = 1; 
		_contentRtf.sizeDelta = new Vector2(_parentRtf.sizeDelta.x, 0); 
	}

	void OnContent(float parentHeight)
	{
		_contentRtf.sizeDelta = new Vector2(_parentRtf.sizeDelta.x, parentHeight); 
		_contentPosRange = _contentRtf.rect.height - _viewPortRtf.rect.height; // TODO 要在之前算好，不能在这里算
	}

	void OnSetScrollRect()
	{
		scrollRect.onValueChanged.RemoveAllListeners(); 
		scrollRect.onValueChanged.AddListener(OnChangeValue); 
		scrollRect.normalizedPosition = Vector2.up * targetPos; 
	}

	public void OnChangeValue(Vector2 value)
	{
//		if (value.y < 0 || value.y > 1)
//		{
//			return; 
//		}
		if (_parentAnchorPoses == null || _parentAnchorPoses.Length == 0)
		{
			return; 
		}
		float yTop = -(1 - value.y) * _contentPosRange; // viewPort上边缘对应的content的localPos
		Debug.Log("yTop: " + yTop); 
		float yBottom = (-(1 - value.y) * _contentPosRange) - _viewPortRtf.rect.height; // viewPort下边缘对应的content的localPos
		Debug.Log("yBottom: " + yBottom); 

		// 计算parent的anchorPos
		int index; 
		OnChangeIndex(yTop, yBottom, out index); 
		SetEdge(index); 
		OnSetItems(); 
		OnChangeParentAnchor(); 
	}

	#endregion



	#region Edge

	[SerializeField] VerticalLayoutGroup layoutGroup;
	float _topItemEdgePos;
	float _bottomItemEdgePos;
	int _curIndex;

	void SetEdge(int index)
	{
		if (index < 0 || index >= _parentAnchorPoses.Length)
		{
			Debug.Log("index out of range! "); 
			return; 
		}
		if (_curIndex == index)
		{
			return; 
		}
		_curIndex = index; 
		Debug.Log("_curIndex: " + _curIndex); 
		_bottomItemEdgePos = _parentBottomPoses[_curIndex]; 
		_topItemEdgePos = _parentAnchorPoses[_curIndex]; 
	}

	void ClearEdge()
	{
		_curIndex = 0; 
		_topItemEdgePos = 0; 
		_bottomItemEdgePos = 0; 
	}

	void CalcEdgePos()
	{
		_topItemEdgePos = 0;
		float height = 0; 
		for (int i = 0, count = _items.Count; i < count; i++)
		{
			height -= itemHeights[i] + layoutGroup.spacing; 
		}
		height += layoutGroup.spacing; 
		_bottomItemEdgePos = height; 
	}

	#endregion



	#region Wait

	Queue<IEnumerator> waitRoutines = new Queue<IEnumerator>();

	void SetWait(Action a)
	{
		var r = WaitRoutine(a); 
		StartCoroutine(r); 
		waitRoutines.Enqueue(r); 
	}

	void ClearWait()
	{
		for (int i = 0, count = waitRoutines.Count; i < count; i++)
		{
			ClearWaitRoutine(); 
		}
	}

	IEnumerator WaitRoutine(Action a)
	{
		yield return null; 
		if (a != null)
		{
			a(); 
			a = null; 
		}
		ClearWaitRoutine(); 
	}

	void ClearWaitRoutine()
	{
		if (waitRoutines.Count == 0)
		{
			return; 
		}

		var r = waitRoutines.Peek(); 
		if (r != null)
		{
			StopCoroutine(r); 
			r = null; 
		}
		waitRoutines.Dequeue(); 
	}

	#endregion
}
