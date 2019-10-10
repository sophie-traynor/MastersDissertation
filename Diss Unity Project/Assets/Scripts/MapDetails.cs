using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

//https://github.com/Placenote/PlacenoteSDK-Unity/blob/master/Assets/Placenote/Examples/RandomShapes/MapInfoElement.cs
public class MapDetails : MonoBehaviour
{
	[SerializeField] Text mMapIdText;
	[SerializeField] Toggle mToggle;

	public void Initialize (LibPlacenote.MapInfo mapInfo, ToggleGroup toggleGroup,
	                       RectTransform listParent, UnityAction<bool> onToggleChanged)
	{
		mMapIdText.text = mapInfo.placeId;
		if (mapInfo.metadata.name != null && mapInfo.metadata.name.Length > 0) {
			mMapIdText.text = mapInfo.metadata.name;
		}
		mToggle.group = toggleGroup;
		gameObject.transform.SetParent (listParent);
		mToggle.onValueChanged.AddListener (onToggleChanged);

	}

}
