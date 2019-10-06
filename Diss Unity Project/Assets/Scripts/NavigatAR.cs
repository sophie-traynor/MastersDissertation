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

public class NavigatAR : MonoBehaviour, PlacenoteListener
{
	[SerializeField] GameObject MapSelectedPanel;
	[SerializeField] GameObject InitPanel;
    [SerializeField] GameObject InitButtonsPanel;
    [SerializeField] GameObject MapNamePanel;
    [SerializeField] InputField MapNameInputField;
    [SerializeField] GameObject MappingPanel;
	[SerializeField] GameObject AddArrowButton;
	[SerializeField] GameObject MapListPanel;
	[SerializeField] GameObject ExitButton;
	[SerializeField] GameObject mListElement;
    [SerializeField] RectTransform mListContentParent;
	[SerializeField] ToggleGroup mToggleGroup;
	[SerializeField] Text mLabelText;

    public string mapName;

    private UnityARSessionNativeInterface mSession;

	private bool mARInit = false;

	
    private LibPlacenote.MapMetadataSettable mCurrMapDetails;

	private bool mReportDebug = false;

	private LibPlacenote.MapInfo mSelectedMapInfo;
	private string mSelectedMapId {
		get {
			return mSelectedMapInfo != null ? mSelectedMapInfo.placeId : null;
		}
	}
	private string mSaveMapId = null;


	private BoxCollider mBoxColliderDummy;
	private SphereCollider mSphereColliderDummy;
	private CapsuleCollider mCapColliderDummy;


	// Use this for initialization
	void Start ()
	{
		Input.location.Start ();

		MapListPanel.SetActive (false);

		mSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();

        StartARKit();

		LibPlacenote.Instance.RegisterListener (this);

		// for simulator
		#if UNITY_EDITOR
		AddArrowButton.SetActive(true);
		#endif
	}
		
	void Update ()
	{

        if (!mARInit && LibPlacenote.Instance.Initialized()) {
			mARInit = true;
			mLabelText.text = "Ready to Start!";
		}
	}


    public void OnListMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		foreach (Transform t in mListContentParent.transform) {
			Destroy (t.gameObject);
		}


		MapListPanel.SetActive (true);
		InitPanel.SetActive (false);
		LibPlacenote.Instance.ListMaps ((mapList) => {
			// render the map list!
			foreach (LibPlacenote.MapInfo mapInfoItem in mapList) {
                if (mapInfoItem.metadata.userdata != null) {
                    Debug.Log(mapInfoItem.metadata.userdata.ToString (Formatting.None));
				}
                AddMapToList (mapInfoItem);
			}
		});
	}


	public void OnCancelClick ()
	{
		MapSelectedPanel.SetActive (false);
		MapListPanel.SetActive (false);
		InitPanel.SetActive (true);
        InitButtonsPanel.SetActive(true);
        MapNamePanel.SetActive(false);

    }


	public void OnExitClick ()
	{
		InitPanel.SetActive (true);
        InitButtonsPanel.SetActive(true);
        MapNamePanel.SetActive(false);
        ExitButton.SetActive (false);
		MappingPanel.SetActive (false);

		LibPlacenote.Instance.StopSession ();
        FeaturesVisualizer.DisablePointcloud(); 
        FeaturesVisualizer.clearPointcloud();
        GetComponent<ShapeManager>().ClearShapes();

	}


	void AddMapToList (LibPlacenote.MapInfo mapInfo)
	{
		GameObject newElement = Instantiate (mListElement) as GameObject;
		MapDetails listElement = newElement.GetComponent<MapDetails> ();
		listElement.Initialize (mapInfo, mToggleGroup, mListContentParent, (value) => {
			OnMapSelected (mapInfo);
		});
	}


	void OnMapSelected (LibPlacenote.MapInfo mapInfo)
	{
		mSelectedMapInfo = mapInfo;
        MapSelectedPanel.SetActive (true);
	}


	public void OnLoadMapClicked ()
	{
        ConfigureSession ();
        FeaturesVisualizer.DisablePointcloud();
        FeaturesVisualizer.clearPointcloud();

        if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		mLabelText.text = "Loading Route ID: " + mSelectedMapId;
		LibPlacenote.Instance.LoadMap (mSelectedMapId,
			(completed, faulted, percentage) => {
				if (completed) {
					MapSelectedPanel.SetActive (false);
					MapListPanel.SetActive (false);
					InitPanel.SetActive (false);
					MappingPanel.SetActive(false);
					ExitButton.SetActive (true);

					LibPlacenote.Instance.StartSession ();

					if (mReportDebug) {
						LibPlacenote.Instance.StartRecordDataset (
							(datasetCompleted, datasetFaulted, datasetPercentage) => {

								if (datasetCompleted) {
									mLabelText.text = "Dataset Upload Complete";
								} else if (datasetFaulted) {
									mLabelText.text = "Dataset Upload Faulted";
								} else {
									mLabelText.text = "Dataset Upload: " + datasetPercentage.ToString ("F2") + "/1.0";
								}
							});
						Debug.Log ("Started Debug Report");
					}

					mLabelText.text = "Loaded ID: " + mSelectedMapId;
				} else if (faulted) {
					mLabelText.text = "Failed to load ID: " + mSelectedMapId;
				} else {
					mLabelText.text = "Route Download: " + percentage.ToString ("F2") + "/1.0";
				}
			}
		);
	}

    public void OnMapNameBackClick()
    {
        InitButtonsPanel.SetActive(true);
        MapNamePanel.SetActive(false);
    }

    public void OnNewMapClick()
    {
        FeaturesVisualizer.EnablePointcloud();
        InitButtonsPanel.SetActive(false);
        MapNamePanel.SetActive(true);
    }

    public void OnMapNameOkClick()
    {
        mapName = MapNameInputField.text;
        if (mapName is null)
        {
            Debug.Log("Route name null");
        }
        else
        {
            CreateNewMap();
            Debug.Log("creating route with name" + mapName);
        }
    }

    public void CreateNewMap ()
	{
		ConfigureSession ();

		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		InitPanel.SetActive (false);
		MappingPanel.SetActive (true);
        ExitButton.SetActive(true);

        Debug.Log ("Started Session");
		LibPlacenote.Instance.StartSession ();

		if (mReportDebug) {
			LibPlacenote.Instance.StartRecordDataset (
				(completed, faulted, percentage) => {
					if (completed) {
						mLabelText.text = "Dataset Upload Complete";
					} else if (faulted) {
						mLabelText.text = "Dataset Upload Faulted";
					} else {
						mLabelText.text = "Dataset Upload: (" + percentage.ToString ("F2") + "/1.0)";
					}
				});
			Debug.Log ("Started Debug Report");
		}

	}

	private void StartARKit ()
	{
		#if !UNITY_EDITOR
		mLabelText.text = "Initializing ARKit";
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
		mSession.RunWithConfig (config);
 		#endif
	}


	public void OnSaveMapClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		bool useLocation = Input.location.status == LocationServiceStatus.Running;
		LocationInfo locationInfo = Input.location.lastData;

		mLabelText.text = "Saving...";
		LibPlacenote.Instance.SaveMap (
			(mapId) => {
				LibPlacenote.Instance.StopSession ();
                FeaturesVisualizer.DisablePointcloud();
                FeaturesVisualizer.clearPointcloud();

                mSaveMapId = mapId;
				InitPanel.SetActive (true);
                InitButtonsPanel.SetActive(true);
                MapNamePanel.SetActive(false);
                MappingPanel.SetActive (false);
				ExitButton.SetActive(false);

				LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();
                metadata.name = mapName;
				mLabelText.text = "Saved Route Name: " + metadata.name;

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
				LibPlacenote.Instance.SetMetadata (mapId, metadata, (success) => {
					if (success) {
						Debug.Log("Meta data successfully saved");
					} else {
						Debug.Log("Meta data failed to save");
					}
				});
				mCurrMapDetails = metadata;
			},
			(completed, faulted, percentage) => {
				if (completed) {
					mLabelText.text = "Upload Complete:" + mCurrMapDetails.name;
				}
				else if (faulted) {
					mLabelText.text = "Upload of Route Named: " + mCurrMapDetails.name + "faulted";
				}
				else {
					mLabelText.text = "Uploading Route Named: " + mCurrMapDetails.name + "(" + percentage.ToString("F2") + "/1.0)";
				}
			}
		);
	}

	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}

	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Localized";
            GetComponent<ShapeManager>().LoadShapesJSON (mSelectedMapInfo.metadata.userdata);
		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			mLabelText.text = "Mapping: Tap to add Shapes";
		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
			mLabelText.text = "Searching for position lock";
		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
            if (GetComponent<ShapeManager>().shapeObjList.Count != 0) {
                GetComponent<ShapeManager>().ClearShapes ();
			}
		}
	}



}
