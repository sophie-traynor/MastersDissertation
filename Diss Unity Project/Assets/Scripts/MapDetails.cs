using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

//https://github.com/Placenote/PlacenoteSDK-Unity/blob/master/Assets/Placenote/Examples/RandomShapes/MapInfoElement.cs
public class MapDetails : MonoBehaviour
{
	[SerializeField] Text routeIdText;
	[SerializeField] Toggle toggle;

	public void Initialize (LibPlacenote.MapInfo routeInfo, ToggleGroup toggleGroup,
	                       RectTransform listParent, UnityAction<bool> onToggleChanged)
	{
		routeIdText.text = routeInfo.placeId;
		if (routeInfo.metadata.name != null && routeInfo.metadata.name.Length > 0) {
			routeIdText.text = routeInfo.metadata.name;
		}
		toggle.group = toggleGroup;
		gameObject.transform.SetParent (listParent);
		toggle.onValueChanged.AddListener (onToggleChanged);

	}

}
