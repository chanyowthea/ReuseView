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
	[SerializeField] VerticalLayoutGroup layoutGroup;
	[SerializeField] BaseItem _itemPrefab;
	// 最小高度Prefab
	[SerializeField] RectTransform _parentRtf;
	[SerializeField] RectTransform _viewPortRtf;
	[SerializeField] RectTransform _contentRtf;
	List<BaseItem> _items = new List<BaseItem>();

	#region Items

	float _minItemHeight;

	#endregion

	float[] itemHeights; 

	public void Set()
	{
		if (_vos == null || _vos.Length == 0)
		{
			Debug.Log("vos is empty! "); 
			return; 
		}

		_itemPrefab.Clear(); 
		// 如果处于未激活状态，是否可以用rect来获取高度？
		_itemPrefab.gameObject.SetActive(true); // 为了后面获取itemPrefab的高度，必须激活
		SetWait(() =>
			{
				_minItemHeight = (_itemPrefab.transform as RectTransform).sizeDelta.y; 
				Debug.Log("_minItemHeight: " + _minItemHeight); 

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
				parentHeight -= layoutGroup.spacing; 
				Debug.Log("parentHeight: " + parentHeight); 
				_contentRtf.sizeDelta = new Vector2(_parentRtf.sizeDelta.x, parentHeight); 

				_itemPrefab.gameObject.SetActive(false);

				SetWait(() => // 也许这里不必SetWait
					{
						int prefabsCount = Mathf.CeilToInt(_viewPortRtf.rect.height / _minItemHeight) + 1; // 屏幕所能显示的最大数量
						Debug.Log("prefabsCount: " + prefabsCount + ", sizeDelta: " + _viewPortRtf.rect); 
						bool isLess = prefabsCount < _vos.Length; 
						prefabsCount = isLess ? prefabsCount : _vos.Length; 
						int len = isLess ? (_vos.Length - prefabsCount + 1) : 0; // parent的anchorPos只可能在这个范围内变动
						_parentAnchorPoses = new float[len]; 
						_parentBottomPoses = new float[len]; 
						Debug.LogWarning("len: " + len); 
						_topItemEdgePos = 0; 
						_bottomItemEdgePos = 0; 
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
							if(i + prefabsCount >= itemHeights.Length)
							{
								break; 
							}
							_bottomItemEdgePos -= layoutGroup.spacing + itemHeights[i + prefabsCount]; 
						}
							
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

						SetWait(CalcEdgePos); 
					}); 
			}); 
	}

	float[] _parentAnchorPoses;
	float[] _parentBottomPoses;

	public void Clear()
	{
		ClearEdge(); 
		ClearHide(); 
		ClearWait(); 
		for (int i = 0, count = _items.Count; i < count; i++)
		{
			var item = _items[i]; 
			item.Clear(); 
			GameObject.Destroy(item.gameObject); 
		}
		_items.Clear(); 
		_vos = null; 
		_parentRtf.anchoredPosition = Vector2.zero;
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



	#region Edge

	float _topItemEdgePos;
	float _bottomItemEdgePos;
	int _curIndex;

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
		SetHide(); 
	}

	#endregion

	#region Hide
	[NonSerialized] public float targetPos = 1; 
	[SerializeField] ScrollRect scrollRect;

	void SetHide()
	{
		scrollRect.onValueChanged.RemoveAllListeners(); 
		scrollRect.onValueChanged.AddListener(OnChangeValue); 
		scrollRect.normalizedPosition = Vector2.up * targetPos; 
	}

	void ClearHide()
	{
		scrollRect.onValueChanged.RemoveAllListeners(); 
		scrollRect.verticalNormalizedPosition = 1; 
	}


	float _contentPosRange;

	public void OnChangeValue(Vector2 value)
	{
		if (value.y < 0 || value.y > 1)
		{
			return; 
		}
		if (_parentAnchorPoses == null || _parentAnchorPoses.Length == 0)
		{
			return; 
		}

		_contentPosRange = _contentRtf.rect.height - _viewPortRtf.rect.height; // TODO 要在之前算好，不能在这里算
		float yTop = (int)(-(1 - value.y) * _contentPosRange); // viewPort上边缘对应的content的localPos
		Debug.LogWarning("yTop: " + yTop); 
		float yBottom = (int)((-(1 - value.y) * _contentPosRange) - _viewPortRtf.rect.height); // viewPort下边缘对应的content的localPos
		Debug.Log("yBottom: " + yBottom); 

		// 计算parent的anchorPos
		float minGapTop = 0 - _parentAnchorPoses[_parentAnchorPoses.Length - 1]; // TODO 没有开堆不允许进入这里
		float minGapBottom = 0 - _parentBottomPoses[_parentBottomPoses.Length - 1]; // TODO 没有开堆不允许进入这里
		int index = -1; 
		if (yTop >= _topItemEdgePos) // 如果滑动后，顶部超过parent的顶部，那么向上移动parent
		{	
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
					minGapTop = gap; 
					index = i; 
				} 
			}
		}
		else
		{
			// 滑动后没有超过范围就不用变化parent的位置
		}
		if (index < 0 || index >= _parentAnchorPoses.Length)
		{
//			Debug.LogError("index out of range: " + index); 
			return; 
		}
		if (_curIndex == index)
		{
			return; 
		}
		_curIndex = index; 
//		Debug.LogError("_curIndex: " + _curIndex); 
		_bottomItemEdgePos = _parentBottomPoses[index]; 
		_topItemEdgePos = _parentAnchorPoses[index]; 

		for (int i = 0, count = _items.Count; i < count; i++)
		{
			var item = _items[i]; 
			item.Clear(); 
			item.vo = _vos[_curIndex + i]; 
			item.Set(); 
		}
		_parentRtf.anchoredPosition = new Vector2(0, _parentAnchorPoses[index]); 
	}

	#endregion
}
