using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// --使用说明--
// 1 都要向左上角对齐

// BUG
// 不要实例化

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

	public void Set()
	{
		if (_vos == null || _vos.Length == 0)
		{
			Debug.Log("vos is empty! "); 
			return; 
		}

		_itemPrefab.gameObject.SetActive(false); // TODO
		for (int i = 0, count = _vos.Length; i < count; i++)
		{
			var item = GameObject.Instantiate(_itemPrefab); // 此处实例化是错的，如果有1000个，岂不是要真的实例化1000个，不切实际？
			item.gameObject.SetActive(true); 
			item.transform.SetParent(_parentRtf); 
			item.transform.localScale = Vector3.one; 
			item.vo = _vos[i]; 
			item.Set(); 
			_items.Add(item); 
		}

		_itemPrefab.Clear(); 
		// 如果处于未激活状态，是否可以用rect来获取高度？
		_itemPrefab.gameObject.SetActive(true); // 为了后面获取itemPrefab的高度，必须激活
		SetWait(() =>
			{

				_minItemHeight = (_itemPrefab.transform as RectTransform).sizeDelta.y; 
				Debug.Log("_minItemHeight: " + _minItemHeight); 
				_itemPrefab.gameObject.SetActive(false);

				SetWait(() =>
					{
						int prefabsCount = Mathf.CeilToInt(_viewPortRtf.rect.height / _minItemHeight) + 1; // 屏幕所能显示的最大数量
						Debug.Log("prefabsCount: " + prefabsCount + ", sizeDelta: " + _viewPortRtf.rect); 
						bool isLess = prefabsCount < _vos.Length; 
						prefabsCount = isLess ? prefabsCount : _vos.Length; 
						int len = isLess ? (_vos.Length - prefabsCount + 1) : 0; // parent的anchorPos只可能在这个范围内变动
						_parentAnchorPoses = new float[len]; 
						_parentBottomPoses = new float[len]; 
						Debug.LogWarning("len: " + len); 
						for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
						{	
							// TODO 这里写法有问题
							CalcItemEdge(i, true); 
							CalcItemEdge(i + prefabsCount - 1, false); // 由于现在item的个数还是等于vos.Length，因此可以使用这个
							_parentAnchorPoses[i] = _topItemEdgePos; 
							_parentBottomPoses[i] = _bottomItemEdgePos; 
							Debug.LogFormat("_parentAnchorPoses[{0}]: {1}", i, _parentAnchorPoses[i]); 
							Debug.LogFormat("_parentBottomPoses[{0}]: {1}", i, _parentBottomPoses[i]); 
						}
						_contentRtf.sizeDelta = _parentRtf.sizeDelta; // 先把所有的实例化了，获取content的高度

						// 已经获取到了数据，清除无用数据
//						_itemPrefab.gameObject.SetActive(false); 
						for (int i = 0, count = _items.Count; i < count; i++)
						{
							var item = _items[i]; 
							item.Clear(); 
							GameObject.Destroy(item.gameObject); 
						}
						_items.Clear(); 

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
	//	float _secondTopItemEdgePos;
	float _bottomItemEdgePos;
	//	float _secondBottomItemEdgePos;
	int _curIndex;

	void ClearEdge()
	{
		_curIndex = 0; 
		_topItemEdgePos = 0; 
//		_secondTopItemEdgePos = 0; 
		_bottomItemEdgePos = 0; 
//		_secondBottomItemEdgePos = 0; 
	}

	void CalcEdgePos()
	{
		CalcItemEdge(0, true); 
		CalcItemEdge(_items.Count - 1, false); 
		SetHide(); 
	}

	void CalcItemEdge(int index, bool isTop)
	{
		if (index < 0 || index > _items.Count - 1)
		{
			Debug.LogError("index out of range! "); 
			return; 
		}

		var item = _items[index]; 
		Vector3 pos = item.transform.position +
		              new Vector3(0, isTop ? ((item.transform as RectTransform).rect.yMax) : ((item.transform as RectTransform).rect.yMin), 0); // 获取最上边缘的线位置
		Vector2 pos2 = RectTransformUtility.WorldToScreenPoint(null, pos); 
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_contentRtf, pos2, null, out pos2); 
		if (isTop)
		{
			_topItemEdgePos = pos2.y; 
		}
		else
		{
			_bottomItemEdgePos = pos2.y; 
		}
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

		// 如果value的值在接近边缘的地方，parent没有往上

		_contentPosRange = _contentRtf.rect.height - _viewPortRtf.rect.height; // TODO 要在之前算好，不能在这里算
		float yTop = (int)(-(1 - value.y) * _contentPosRange); // viewPort上边缘对应的content的localPos
		Debug.LogWarning("yTop: " + yTop); 
		float yBottom = (int)((-(1 - value.y) * _contentPosRange) - _viewPortRtf.rect.height); // viewPort下边缘对应的content的localPos
		Debug.Log("yBottom: " + yBottom); 

		// 计算parent的anchorPos
		float minGap = 0 - _parentAnchorPoses[_parentAnchorPoses.Length - 1]; // TODO 没有开堆不允许进入这里
		int index = -1; 
		if (yTop >= _topItemEdgePos) // 如果滑动后，顶部超过parent的顶部，那么向上移动parent
		{	
			for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
			{
				float gap = _parentAnchorPoses[i] - yTop; // 只能算比contentRtf的上边缘y坐标大的点
				if (gap >= 0 && gap < minGap)
				{
					minGap = gap; 
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
				if (gap >= 0 && gap < minGap)
				{
					minGap = gap; 
					index = i; 
				} 
			}
		}
		else
		{
			// 滑动后没有超过范围就不用变化parent的位置
		}
		Debug.LogError("index: " + index); 
		if (index < 0 || index >= _parentAnchorPoses.Length)
		{
			Debug.LogError("index out of range: " + index); 
			return; 
		}
		if (_curIndex == index)
		{
			return; 
		}
		_curIndex = index; 
		Debug.LogError("_curIndex: " + _curIndex); 
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
		// 目前只关心用unity跳转过来的，不是在代码里面跳转
	}

	#endregion
}
