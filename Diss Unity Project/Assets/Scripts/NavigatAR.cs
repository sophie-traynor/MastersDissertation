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

//Code has been adapted from Placenote's Sample Application
//https://github.com/Placenote/PlacenoteSDK-Unity/blob/master/Assets/Placenote/Examples/RandomShapes/PlacenoteSampleView.cs
public class NavigatAR : MonoBehaviour, PlacenoteListener
{
	[SerializeField] GameObject routeSelectedPanel;
	[SerializeField] GameObject initialPanel;
     [SerializeField] GameObject initialButtonsPanel;
     [SerializeField] GameObject enterRouteNamePanel;
     [SerializeField] InputField routeNameInputField;
     [SerializeField] GameObject mappingPanel;
	[SerializeField] GameObject routeListPanel;
	[SerializeField] GameObject exitButton;
	[SerializeField] GameObject mListElement;
     [SerializeField] RectTransform mListContentParent;
	[SerializeField] ToggleGroup mToggleGroup;
	[SerializeField] Text notificationPanelText;

     private UnityARSessionNativeInterface mSession; //ARKit Session Handler
     private bool mARInit = false; //Is AR Initialised

     //Information about Route
     public string routeName;
     private LibPlacenote.MapMetadataSettable currentRouteDetails; 
	private LibPlacenote.MapInfo selectedRouteInfo;
	private string selectedRouteId {
		get {
			return selectedRouteInfo != null ? selectedRouteInfo.placeId : null;
		}
	}
	private string saveRouteId = null;

	private BoxCollider mBoxColliderDummy;
	private SphereCollider mSphereColliderDummy;
	private CapsuleCollider mCapColliderDummy;

	//Executes upon Initialization 
	void Start ()
	{
		Input.location.Start (); //Get initial location
		mSession = UnityARSessionNativeInterface.GetARSessionNativeInterface (); //Initializes and starts ARKit Session
          StartARKit(); //Sets ARKit Start Details
		LibPlacenote.Instance.RegisterListener (this); //Initializes Placenote with the session and registers listener
          routeListPanel.SetActive(false);
    }
		
    //Called once every frame
	void Update ()
	{
        //Checks for ARKit Initialisation
        if (!mARInit && LibPlacenote.Instance.Initialized()) {
			mARInit = true;
			notificationPanelText.text = "Ready to Start!";
		}
	}

    //Called when Load Routes button is clicked to load list of routes
    public void OnLoadRouteListClick ()
	{
        foreach (Transform t in mListContentParent.transform) {
			Destroy (t.gameObject);
		}
		routeListPanel.SetActive (true);
		initialPanel.SetActive (false);

         //Checks if a mapping of a route exists in Placenote Database, returns list of all routes
		LibPlacenote.Instance.ListMaps ((routeList) => {
		  //Callback handler function to access metadata in each route and add to list
            foreach (LibPlacenote.MapInfo routeInfoItem in routeList) {
                AddRouteToList (routeInfoItem);
			}
		});
	}

    //Called when back button is clicked whilst on the route list screen
	public void OnCancelClick ()
	{
		routeSelectedPanel.SetActive (false);
		routeListPanel.SetActive (false);
		initialPanel.SetActive (true);
          initialButtonsPanel.SetActive(true);
          enterRouteNamePanel.SetActive(false);
    }

    //Called when back button is clicked when in a mapping session for either create or load route
	public void OnExitClick ()
	{
        initialPanel.SetActive (true);
        initialButtonsPanel.SetActive(true);
        enterRouteNamePanel.SetActive(false);
        exitButton.SetActive (false);
        mappingPanel.SetActive (false);

        LibPlacenote.Instance.StopSession (); //Stop running Placenote Session
        FeaturesVisualizer.DisablePointcloud(); //Stop detecting feature points
        FeaturesVisualizer.clearPointcloud(); //Clear all feature points currently on screen
        GetComponent<ShapeManager>().ClearShapes(); //Calls Shape Manager Class to clear shapes from screen
    	}

    //Called from within Load Route List to add a route from Placenote to the list in the application
	void AddRouteToList (LibPlacenote.MapInfo routeInfo)
	{
		GameObject newElement = Instantiate (mListElement) as GameObject;
         //Calls MapDetails Class to set details for new row in list for route
         MapDetails listElement = newElement.GetComponent<MapDetails> ();
		listElement.Initialize (routeInfo, mToggleGroup, mListContentParent, (value) => {
			OnRouteSelected (routeInfo);
		});
	}

    //Called when user has clicked on a route in the route list
	void OnRouteSelected (LibPlacenote.MapInfo routeInfo)
	{
		selectedRouteInfo = routeInfo;
          routeSelectedPanel.SetActive (true);
	}

     //Called when user has selected a route and clicked Load
	public void OnLoadRouteClicked ()
	{
          ConfigureSession (); //Sets ARKit configuration for session
          FeaturesVisualizer.DisablePointcloud(); //Ensure feature points dont show to user
          FeaturesVisualizer.clearPointcloud(); //Clear any already visible features
		notificationPanelText.text = "Loading Route ID: " + selectedRouteId;

		LibPlacenote.Instance.LoadMap (selectedRouteId,
			(completed, faulted, percentage) => {

				if (completed) { //Mapping of route download finished
					routeSelectedPanel.SetActive (false);
					routeListPanel.SetActive (false);
					initialPanel.SetActive (false);
					mappingPanel.SetActive(false);
					exitButton.SetActive (true);
					LibPlacenote.Instance.StartSession (); //Start Placenote Session when route has successfully downloaded
					notificationPanelText.text = "Loaded Route ID: " + selectedRouteId;

				} else if (faulted) { //route failed to load
					notificationPanelText.text = "Failed to load Route ID: " + selectedRouteId;

				} else { //Route downloading
					notificationPanelText.text = "Route Download: " + percentage.ToString ("F2") + "/1.0";
				}
			}
		);
	}

    //Called when back button is pressed when on create route (enter name) screen
    public void OnRouteNameBackClick()
    {
        initialButtonsPanel.SetActive(true);
        enterRouteNamePanel.SetActive(false);
    }

    //Called when New Route is selected to load screen to enter name for route
    public void OnNewRouteClick()
    {
        FeaturesVisualizer.EnablePointcloud(); //allow feature points to show when mapping 
        initialButtonsPanel.SetActive(false);
        enterRouteNamePanel.SetActive(true);
    }

    //Called when ok is pressed after entering new route name
    public void OnRouteNameOkClick()
    {
        routeName = routeNameInputField.text;
        if (routeName is null)
        {
            Debug.Log("Route name null");
        }
        else
        {
            CreateNewRoute(); //Calls function to start mapping session for new route
            Debug.Log("creating route with name" + routeName);
        }
    }

    //Called within ok clicked function after route name has been entered to start new mapping session
    public void CreateNewRoute ()
	{ 
          ConfigureSession (); //Sets ARKit configuration for session
          initialPanel.SetActive (false);
          mappingPanel.SetActive (true);
          exitButton.SetActive(true);
		LibPlacenote.Instance.StartSession (); //Starts Placenote session
	}

    //ARKit Initialsation settings for when the application is NOT running in Unity Editor
	private void StartARKit ()
	{
        #if !UNITY_EDITOR
		notificationPanelText.text = "Initializing ARKit";
		Application.targetFrameRate = 60;
		ConfigureSession ();
        #endif
    }

    //Sets ARKit configuration settings for when the application is NOT running in Unity Editor
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

    //Called when save button is clicked after route has been finished mapping out
	public void OnSaveRouteClick ()
	{
		bool useLocation = Input.location.status == LocationServiceStatus.Running; //checks if location was used
		LocationInfo locationInfo = Input.location.lastData;
		notificationPanelText.text = "Saving...";
         //Stores route and metadata to placenote
		LibPlacenote.Instance.SaveMap (
			(routeId) => {
                LibPlacenote.Instance.StopSession (); //Stops the Placenote session
                FeaturesVisualizer.DisablePointcloud(); //Stop detecting feature points
                FeaturesVisualizer.clearPointcloud(); //Clear all feature points currently on screen
                saveRouteId = routeId;

                initialPanel.SetActive (true);
                initialButtonsPanel.SetActive(true);
                enterRouteNamePanel.SetActive(false);
                mappingPanel.SetActive (false);
                exitButton.SetActive(false);

                //Create instance of metadata
                LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();
                metadata.name = routeName; //set metadata route name
                notificationPanelText.text = "Saved Route Name: " + metadata.name;

                //Calls the Shape Manager class to get the objects from the route and return them in a JSON format to store in metadata
                JObject userdata = new JObject ();
                metadata.userdata = userdata; 
                JObject shapeList = GetComponent<ShapeManager>().Shapes2JSON();
                userdata["shapeList"] = shapeList;
                GetComponent<ShapeManager>().ClearShapes();

                //Store location with the route
                if (useLocation) {
				metadata.location = new LibPlacenote.MapLocation();
				metadata.location.latitude = locationInfo.latitude;
				metadata.location.longitude = locationInfo.longitude;
				metadata.location.altitude = locationInfo.altitude;
                }

                //set the metadata to the corresponding route id
                LibPlacenote.Instance.SetMetadata (routeId, metadata, (success) => {
				if (success) {
					Debug.Log("Meta data successfully saved");
				} else {
					Debug.Log("Meta data failed to save");
				}
                });
				currentRouteDetails = metadata;
			},
			(completed, faulted, percentage) => {
				if (completed) { //route upload finished
					notificationPanelText.text = "Upload Complete:" + currentRouteDetails.name;
				}
				else if (faulted) { //route upload failed
					notificationPanelText.text = "Upload of Route Named: " + currentRouteDetails.name + "faulted";
				}
				else { //route uploading
					notificationPanelText.text = "Uploading Route Named: " + currentRouteDetails.name + "(" + percentage.ToString("F2") + "/1.0)";
				}
			}
		);
	}

	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}

    //Runs when Placenote sends message to say the status has changed to identify the current state of session
	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) { //Runs when localised to reload shapes in correct location
            notificationPanelText.text = "Localized";
            GetComponent<ShapeManager>().LoadShapesJSON (selectedRouteInfo.metadata.userdata);
		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) { //Runs when in mapping session
			notificationPanelText.text = "Mapping: Tap to add Shapes";
		} else if (currStatus == LibPlacenote.MappingStatus.LOST)
        { //runs when walking a route but it cant detect any feature points at that moment
            notificationPanelText.text = "Searching for position lock";

		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
            if (GetComponent<ShapeManager>().shapeObjList.Count != 0) {
                GetComponent<ShapeManager>().ClearShapes ();
			}
		}
	}



}
