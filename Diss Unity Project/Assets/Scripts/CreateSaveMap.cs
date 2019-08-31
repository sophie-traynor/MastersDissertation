using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CreateSaveMap
{
    public class CreateSaveMap : MonoBehaviour, PlacenoteListener
    {
        //Session Handler for Unity ARKit
        private UnityARSessionNativeInterface session;

        //Game Object References
        public GameObject buttonsPanel;
        public GameObject mapPanel;

        //Stores ID for the map
        private string mapID;
        private LibPlacenote.MapMetadata metadata;

        private bool modelsLoaded = false;

        //print notifications
        public Text notification;

        void Start()
        {
            //Starts the ARKit session
            session = UnityARSessionNativeInterface.GetARSessionNativeInterface();
            startARKit();

            //Display feature points 
            FeaturesVisualizer.EnablePointcloud();

            //Register placenote listeners (onStatusChange and onPose)
            LibPlacenote.Instance.RegisterListener(this);

            notification.text = "Click Start to Create New Map";
        }

        //Activate map session when start is clicked
        public void startCreatingMap()
        {
            notification.text = "Scan environment and add models";

            buttonsPanel.SetActive(false);
            mapPanel.SetActive(true);
            LibPlacenote.Instance.StartSession();
        }

        //Initialise ARKit
        private void startARKit()
        {
            Application.targetFrameRate = 60;
            ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration();
            config.planeDetection = UnityARPlaneDetection.Horizontal;
            config.alignment = UnityARAlignment.UnityARAlignmentGravity;
            config.getPointCloudData = true;
            config.enableLightEstimation = true;
            session.RunWithConfig(config);
        }

        //upload map to Placenote storage when save is clicked
        public void saveMap()
        {
            mapPanel.SetActive(false);
            buttonsPanel.SetActive(true);

            FeaturesVisualizer.clearPointcloud();

            if (!LibPlacenote.Instance.Initialized())
            {
                notification.text = "Not Initialised";
                return;
            }

            //upload the map 
            LibPlacenote.Instance.SaveMap(
            (storedMapID) =>
            {
                mapID = storedMapID;
                LibPlacenote.Instance.StopSession();
                saveMapIDToFile(storedMapID);
            },
            (completed, faulted, percentage) =>
            {
                if (completed)
                {
                    notification.text = "mapID uploaded ->" + mapID;

                //upload meta data for map
                LibPlacenote.MapMetadataSettable metaData = createMetaDataObject();
                    LibPlacenote.Instance.SetMetadata(mapID, metaData, (success) =>
                    {
                        if (success)
                        {
                            notification.text = "metadata saved";
                        }
                        else
                        {
                            notification.text = "metadata not saved";
                        }
                    });
                    GetComponent<ModelManager>().ClearModels();
                }
                else if (faulted)
                {
                    notification.text = "map upload failed";
                }
                else
                {
                    notification.text = "upload progress -> " + percentage.ToString("F2" + "/1.0");
                }
            });
        }

        //localise device and load map
        public void loadMap()
        {
            if (!LibPlacenote.Instance.Initialized())
            {
                notification.text = "not initialized";
                return;
            }

            // Reading the last saved MapID from file
            mapID = loadMapIDFromFile();

            if (mapID == null)
            {
                notification.text = "No Maps Saved!";
                return;
            }

            //buttonsPanel.SetActive(false);
            //mapPanel.SetActive(false);


            LibPlacenote.Instance.LoadMap(mapID,
            (completed, faulted, percentage) =>
            {
                if (completed)
                {
                //download metadata when map is loaded
                LibPlacenote.Instance.GetMetadata(mapID, (LibPlacenote.MapMetadata obj) =>
                    {
                        if (obj != null)
                        {
                            metadata = obj;

                        //localise map
                        LibPlacenote.Instance.StartSession();
                            notification.text = "Localising map ->" + mapID;
                        }
                        else
                        {
                            notification.text = "metadata download failed";
                            return;
                        }
                    });

                }
                else if (faulted)
                {
                    notification.text = "Failed to load map -> " + mapID;
                }
                else
                {
                    notification.text = "Download progress -> " + percentage.ToString("F2") + "/1.0)";
                }
            }

            );
        }


        public LibPlacenote.MapMetadataSettable createMetaDataObject()
        {
            LibPlacenote.MapMetadataSettable metaData = new LibPlacenote.MapMetadataSettable();

            metaData.name = "My test map";

            //locates the GPS of device to save with map
            bool useLocation = Input.location.status == LocationServiceStatus.Running;
            LocationInfo locationInfo = Input.location.lastData;
            if (useLocation)
            {
                metaData.location = new LibPlacenote.MapLocation();
                metaData.location.latitude = locationInfo.latitude;
                metaData.location.longitude = locationInfo.longitude;
                metaData.location.altitude = locationInfo.altitude;
            }

            JObject userdata = new JObject();
            JObject modelList = GetComponent<ModelManager>().Models2JSON();
            userdata["modelList"] = modelList;

            metaData.userdata = userdata;
            return metaData;
        }

        public void clickExit()
        {
            LibPlacenote.Instance.StopSession();
            FeaturesVisualizer.clearPointcloud();
            GetComponent<ModelManager>().ClearModels();

            modelsLoaded = false;

            buttonsPanel.SetActive(true);
            mapPanel.SetActive(false);
        }

        // Runs when a new pose is received from Placenote.    
        public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose) { }

        // Runs when LibPlacenote sends a status change message like Localized!
        public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
        {
            if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST)
            {
                notification.text = "Localized!";

                if (!modelsLoaded)
                {
                    modelsLoaded = true;
                    JToken modelData = metadata.userdata;
                    GetComponent<ModelManager>().LoadModelsFromJSON(modelData);

                }

                // Placenote will automatically correct the camera position on localization.
            }
        }

        private void saveMapIDToFile(string storedMapID)
        {
            string path = Application.persistentDataPath + "/mapID.txt";
            Debug.Log(path);
            StreamWriter writer = new StreamWriter(path, false);
            writer.WriteLine(storedMapID);
            writer.Close();
        }

        private string loadMapIDFromFile()
        {
            string path = Application.persistentDataPath + "/mapID.txt";
            Debug.Log(path);

            if (System.IO.File.Exists(path))
            {
                StreamReader reader = new StreamReader(path);
                string returnValue = reader.ReadLine();

                Debug.Log(returnValue);
                reader.Close();

                return returnValue;
            }
            else
            {
                return null;
            }


        }

    }

}
