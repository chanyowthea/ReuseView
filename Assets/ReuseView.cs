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

	public void Set()
	{
		if (_vos == null || _vos.Length == 0)
		{
			Debug.Log("vos is empty! "); 
			return; 
		}

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
				// 少数隐藏
				// anchor, index
				// 只刷数据不能保持在当前index，有的数据可能减少了，也许当前index已经没有item了
				// 不能把所有的都实例化出来一次

				// --BUG--
				// 快速拉动，有时候有的不显示
				// 画面闪动

				_minItemHeight = (_itemPrefab.transform as RectTransform).sizeDelta.y; 
				Debug.Log("_minItemHeight: " + _minItemHeight); 
				_parentAnchorPoses = new float[_vos.Length]; 
				for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
				{	
					// TODO 这里写法有问题
					CalcItemEdge(i, true); 
					_parentAnchorPoses[i] = _topItemEdgePos; 
					Debug.LogFormat("_parentAnchorPoses[{0}]: {1}", i, _parentAnchorPoses[i]); 
				}
				_contentRtf.sizeDelta = _parentRtf.sizeDelta; // 先把所有的实例化了，获取content的高度

				// 已经获取到了数据，清除无用数据
				_itemPrefab.gameObject.SetActive(false); 
				for (int i = 0, count = _items.Count; i < count; i++)
				{
					var item = _items[i]; 
					item.Clear(); 
					GameObject.Destroy(item.gameObject); 
				}
				_items.Clear(); 

				// 创建新的items
				int prefabsCount = Mathf.CeilToInt(_viewPortRtf.rect.height / _minItemHeight) + 1; // 屏幕所能显示的最大数量
				Debug.Log("prefabsCount: " + prefabsCount + ", sizeDelta: " + _viewPortRtf.rect); 
				prefabsCount = prefabsCount < _vos.Length ? prefabsCount : _vos.Length; 
				for (int i = 0; i < prefabsCount; i++)
				{
					var item = GameObject.Instantiate(_itemPrefab); 
					item.gameObject.SetActive(true); 
					item.transform.SetParent(_parentRtf); 
					item.transform.localScale = Vector3.one; 
					item.vo = _vos[curIndex + i]; 
					item.Set(); 
					_items.Add(item); 
				}

				//		Invoke("CalcEdgePos", Time.maximumDeltaTime); 
				SetWait(CalcEdgePos); 
//				SetWait(Recalc); 
			}); 
	}

	float[] _parentAnchorPoses;

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
//		ClearWaitRoutine(); 
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
//		Invoke("Recalc", Time.deltaTime); 
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
	float _secondTopItemEdgePos;
	float _bottomItemEdgePos;
	float _secondBottomItemEdgePos;
	int curIndex;

	void ClearEdge()
	{
		curIndex = 0; 
		_topItemEdgePos = 0; 
		_secondTopItemEdgePos = 0; 
		_bottomItemEdgePos = 0; 
		_secondBottomItemEdgePos = 0; 
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
			_secondTopItemEdgePos = _topItemEdgePos - (layoutGroup.spacing + (item.transform as RectTransform).rect.height); 
			Debug.Log("_topItemEdgePos: " + _topItemEdgePos + ", _secondTopItemEdgePos: " + _secondTopItemEdgePos); 
		}
		else
		{
			_bottomItemEdgePos = pos2.y; 
			_secondBottomItemEdgePos = _bottomItemEdgePos + (layoutGroup.spacing + (item.transform as RectTransform).rect.height); 
			Debug.Log("_bottomItemEdgePos: " + _bottomItemEdgePos + ", _secondBottomItemEdgePos: " + _secondBottomItemEdgePos);
		}
	}

	#endregion

	#region Hide

	[SerializeField] ScrollRect scrollRect;

	void SetHide()
	{
		scrollRect.onValueChanged.RemoveAllListeners(); 
		scrollRect.onValueChanged.AddListener(OnChangeValue); 
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

		_contentPosRange = _contentRtf.rect.height - _viewPortRtf.rect.height; // TODO 要在之前算好，不能在这里算
		int yTop = (int)(-(1 - value.y) * _contentPosRange); // viewPort上边缘对应的content的localPos
		float curYPos = -(1 - value.y) * _contentPosRange;  
		int yBottom = (int)((-(1 - value.y) * _contentPosRange) - _viewPortRtf.rect.height); // viewPort下边缘对应的content的localPos
		Debug.Log("yBottom: " + yBottom); 

		float minGap = 0 - _parentAnchorPoses[_parentAnchorPoses.Length - 1]; // TODO 没有开堆不允许进入这里
		for (int i = 0, count = _parentAnchorPoses.Length; i < count; i++)
		{
			float gap = _parentAnchorPoses[i] - curYPos; // 只能算比contentRtf的上边缘点y坐标大的点
			if (gap >= 0 && gap < minGap)
			{
				minGap = gap; 
				curIndex = i; 
			} 
		}
		Debug.LogWarning("curYPos: " + curYPos); 
		Debug.LogWarning("curIndex after for loop: " + curIndex); 
		if (yBottom <= (int)_bottomItemEdgePos)
		{
			if (curIndex + 1 > _vos.Length - _items.Count)
			{
				return; 
			}
			Debug.LogError("Move down"); 
			++curIndex; 
			Debug.LogWarning("curIndex: " + curIndex); 
			float lastParentBottomPos = _parentRtf.transform.localPosition.y + (_parentRtf.transform as RectTransform).rect.yMin; 
			Debug.LogError("lastParentBottomPos: " + lastParentBottomPos); 

			for (int i = 0, count = _items.Count; i < count; i++)
			{
				var item = _items[i]; 
				item.Clear(); 
				item.vo = _vos[curIndex + i]; 
				item.Set(); 
			}

			SetWait(() =>
				{
					Debug.LogWarning("height: " + _parentRtf.rect.height); 
					_parentRtf.anchoredPosition = new Vector2(0, lastParentBottomPos + _parentRtf.rect.height); 
					CalcItemEdge(0, true); 
					CalcItemEdge(_items.Count - 1, false); 
					float moveDelta = _secondBottomItemEdgePos - _bottomItemEdgePos;
					Debug.LogError("delta: " + moveDelta); 
					_parentRtf.anchoredPosition -= Vector2.up * moveDelta; 
					_secondBottomItemEdgePos -= moveDelta; 
					_bottomItemEdgePos -= moveDelta; 

					_secondTopItemEdgePos -= moveDelta; 
					_topItemEdgePos -= moveDelta; 
					Debug.LogError("_bottomItemEdgePos: " + _bottomItemEdgePos + ", _secondBottomItemEdgePos: " + _secondBottomItemEdgePos); 
					Debug.LogError("_topItemEdgePos: " + _topItemEdgePos + ", _secondTopItemEdgePos: " + _secondTopItemEdgePos); 
				}); 
		}
		if (yTop >= (int)_topItemEdgePos)
		{
			if (curIndex - 1 < 0)
			{
				return; 
			}
			Debug.LogError("Move up"); 
			--curIndex; 
			Debug.LogWarning("curIndex: " + curIndex); 
			for (int i = 0, count = _items.Count; i < count; i++)
			{
				var item = _items[i]; 
				item.Clear(); 
				item.vo = _vos[curIndex + i]; 
				item.Set(); 
			}
			SetWait(() =>
				{
					Debug.LogWarning("height: " + _parentRtf.rect.height); 
					CalcItemEdge(0, true); 
					CalcItemEdge(_items.Count - 1, false); 
					float moveDelta = _topItemEdgePos - _secondTopItemEdgePos;
					Debug.LogError("delta: " + moveDelta); 
					_parentRtf.anchoredPosition += Vector2.up * moveDelta; 
					_secondTopItemEdgePos += moveDelta; 
					_topItemEdgePos += moveDelta; 

					_secondBottomItemEdgePos += moveDelta; 
					_bottomItemEdgePos += moveDelta; 
					Debug.LogError("_bottomItemEdgePos: " + _bottomItemEdgePos + ", _secondBottomItemEdgePos: " + _secondBottomItemEdgePos); 
					Debug.LogError("_topItemEdgePos: " + _topItemEdgePos + ", _secondTopItemEdgePos: " + _secondTopItemEdgePos); 
				}); 
		}
	}

	#endregion
}
