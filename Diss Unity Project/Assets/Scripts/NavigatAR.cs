using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

//https://github.com/Placenote/PlacenoteSDK-Unity/blob/master/Assets/Placenote/Examples/RandomShapes/PlacenoteSampleView.cs
public class NavigatAR : MonoBehaviour, PlacenoteListener
{
    [SerializeField] GameObject RouteSelectedPanel;
    [SerializeField] GameObject RouteNamePanel;
    [SerializeField] InputField RouteNameInputField;
    [SerializeField] GameObject MappingPanel;
    [SerializeField] GameObject AddArrowButton;
    [SerializeField] GameObject RouteListPanel;
    [SerializeField] GameObject ExitButton;
    [SerializeField] GameObject mListElement;
    [SerializeField] RectTransform mListContentParent;
    [SerializeField] ToggleGroup mToggleGroup;
    [SerializeField] Text notificationPanelText;

    public string routeName;
    private UnityARSessionNativeInterface ARSession;
    private bool ARInitialized = false;
    private LibPlacenote.MapMetadataSettable currentRouteInfo;
    private LibPlacenote.MapInfo selectedRouteInfo;
    private string selectedRouteID {
		get {
			return selectedRouteInfo != null ? selectedRouteInfo.placeId : null;
		}
	}
    private string saveRouteID = null;
    private BoxCollider mBoxColliderDummy;


	// Use this for initialization
	void Start ()
	{
        Input.location.Start ();
        LoadListOfRoutes();
        ARSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
        StartARKit();
        LibPlacenote.Instance.RegisterListener (this);
        // for simulator
        #if UNITY_EDITOR
        AddArrowButton.SetActive(true);
        #endif
	}
		
	void Update ()
	{
        if (!ARInitialized && LibPlacenote.Instance.Initialized()) {
			ARInitialized = true;
			notificationPanelText.text = "Initialized and Ready!";
		}
	}


    public void LoadListOfRoutes ()
	{

		foreach (Transform t in mListContentParent.transform) {
			Destroy (t.gameObject);
		}

		RouteListPanel.SetActive (true);
		LibPlacenote.Instance.ListMaps ((routeList) => {
			//render list of routes
			foreach (LibPlacenote.MapInfo routeInfoItem in routeList) {
                AddRouteToList (routeInfoItem);
			}
		});
	}

	public void OnExitClick ()
	{
        RouteListPanel.SetActive (true);
        RouteNamePanel.SetActive(false);
        ExitButton.SetActive (false);
        MappingPanel.SetActive (false);
        LibPlacenote.Instance.StopSession ();
        FeaturesVisualizer.DisablePointcloud(); 
        FeaturesVisualizer.clearPointcloud();
        GetComponent<ShapeManager>().ClearShapes();
	}


	void AddRouteToList (LibPlacenote.MapInfo routeInfo)
	{
		GameObject newElement = Instantiate (mListElement) as GameObject;
		MapDetails listElement = newElement.GetComponent<MapDetails> ();
		listElement.Initialize (routeInfo, mToggleGroup, mListContentParent, (value) => {
			OnRouteSelected (routeInfo);
		});
	}

	void OnRouteSelected (LibPlacenote.MapInfo routeInfo)
	{
        selectedRouteInfo = routeInfo;
        RouteSelectedPanel.SetActive (true);
	}


	public void OnLoadRouteClicked ()
	{
        ConfigureSession ();
        FeaturesVisualizer.DisablePointcloud();
        FeaturesVisualizer.clearPointcloud();
        notificationPanelText.text = "Loading Route: " + selectedRouteID;
        LibPlacenote.Instance.LoadMap (selectedRouteID,
			(completed, faulted, percentage) => {
				if (completed) {
					RouteSelectedPanel.SetActive (false);
					RouteListPanel.SetActive (false);
					MappingPanel.SetActive(false);
					ExitButton.SetActive (true);
					LibPlacenote.Instance.StartSession ();
					notificationPanelText.text = "Loaded ID: " + selectedRouteID;
				} else if (faulted) {
					notificationPanelText.text = "Failed to load ID: " + selectedRouteID;
				} else {
					notificationPanelText.text = "Route Download: " + percentage.ToString ("F2") + "/1.0";
				}
			}
		);
	}

    public void OnRouteNameBackClick()
    {
        RouteListPanel.SetActive (true);
        RouteNamePanel.SetActive(false);
    }

    public void OnNewRouteClick()
    {
        FeaturesVisualizer.EnablePointcloud();
        RouteListPanel.SetActive(false);
        RouteNamePanel.SetActive(true);
    }

    public void OnRouteNameOkClick()
    {
        RouteNamePanel.SetActive(false);
        routeName = RouteNameInputField.text;
        if (routeName is null)
        {
            Debug.Log("Route name null");
        }
        else
        {
            CreateNewRoute();
            Debug.Log("creating route with name" + routeName);
        }
    }

    public void CreateNewRoute ()
	{
        ConfigureSession ();
        RouteListPanel.SetActive(false);
        MappingPanel.SetActive (true);
        ExitButton.SetActive(true);
        LibPlacenote.Instance.StartSession ();
	}

	private void StartARKit ()
	{
		#if !UNITY_EDITOR
		notificationPanelText.text = "Initializing ARKit";
		Application.targetFrameRate = 60;
		ConfigureSession ();
		#endif
	}


	private void ConfigureSession() {
 		#if !UNITY_EDITOR
		ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();
		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.getPointCloudData = true;
		config.enableLightEstimation = true; 
          config.planeDetection = UnityARPlaneDetection.Horizontal;
		ARSession.RunWithConfig (config);
 		#endif
	}


	public void OnSaveRouteClick ()
	{
		bool useLocation = Input.location.status == LocationServiceStatus.Running;
		LocationInfo locationInfo = Input.location.lastData;
		notificationPanelText.text = "Saving...";
		LibPlacenote.Instance.SaveMap (
			(routeId) => {
                    LibPlacenote.Instance.StopSession ();
                    FeaturesVisualizer.DisablePointcloud();
                    FeaturesVisualizer.clearPointcloud();
                    saveRouteID = routeId;

                    RouteListPanel.SetActive(true);
                    RouteNamePanel.SetActive(false);
                    MappingPanel.SetActive (false);
                    ExitButton.SetActive(false);

                    LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();
                    metadata.name = routeName;
                    notificationPanelText.text = "Saved Route Name: " + metadata.name;

                    JObject userdata = new JObject ();
                    metadata.userdata = userdata;
                    JObject shapeList = GetComponent<ShapeManager>().Shapes2JSON();
                    userdata["shapeList"] = shapeList;

                    GetComponent<ShapeManager>().ClearShapes();

                    if (useLocation) {
					metadata.location = new LibPlacenote.MapLocation();
					metadata.location.latitude = locationInfo.latitude;
					metadata.location.longitude = locationInfo.longitude;
					metadata.location.altitude = locationInfo.altitude;
                    }
                    LibPlacenote.Instance.SetMetadata (routeId, metadata, (success) => {
					if (success) {
						Debug.Log("Meta data successfully saved");
					} else {
						Debug.Log("Meta data failed to save");
					}
				});
				currentRouteInfo = metadata;
			},
			(completed, faulted, percentage) => {
				if (completed) {
					notificationPanelText.text = "Upload Complete:" + currentRouteInfo.name;
				}
				else if (faulted) {
					notificationPanelText.text = "Upload of Route Named: " + currentRouteInfo.name + "faulted";
				}
				else {
					notificationPanelText.text = "Uploading Route Named: " + currentRouteInfo.name + "(" + percentage.ToString("F2") + "/1.0)";
				}
			}
		);
	}

	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}

	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
			notificationPanelText.text = "Localized";
            GetComponent<ShapeManager>().LoadShapesJSON (selectedRouteInfo.metadata.userdata);
		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			notificationPanelText.text = "Mapping: Tap to add Shapes";
		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
			notificationPanelText.text = "Searching for position lock";
		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
            if (GetComponent<ShapeManager>().shapeObjList.Count != 0) {
                GetComponent<ShapeManager>().ClearShapes ();
			}
		}
	}



}
