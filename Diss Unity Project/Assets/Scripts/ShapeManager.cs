using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.iOS;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

//Code has been adapted from Placenote's Sample Application
//https://github.com/Placenote/PlacenoteSDK-Unity/blob/master/Assets/Placenote/Examples/RandomShapes/ShapeManager.cs

//Shape Classes which store information about each shape (prefab)
[System.Serializable]
public class ShapeInfo
{
    public float px;
    public float py;
    public float pz;
    public float qx;
    public float qy;
    public float qz;
    public float qw;
    public int shapeType;
}
[System.Serializable]
public class ShapeList
{
    public ShapeInfo[] shapes;
}


//Main Class
public class ShapeManager : MonoBehaviour
{

    public List<GameObject> ShapePrefabs = new List<GameObject>();
    public List<ShapeInfo> shapeInfoList = new List<ShapeInfo>();
    public List<GameObject> shapeObjList = new List<GameObject>();

    public int objType; //Each prefab has a index associated for its type
    public int factCount;

    void Start()
    {
        objType = 0;
        factCount = 7; //fact prefabs start from index 7 
    }

    //Functions to add relevant prefab to screen
    public void OnAddArrowClick()
    {
        objType = 0;
        AddShape();
    }
    public void OnPlaceMacClick()
    {
        objType = 1;
        AddShape();
    }
    public void OnPlaceWindowsClick()
    {
        objType = 2;
        AddShape();
    }
    public void OnPlaceLinuxClick()
    {
        objType = 3;
        AddShape();
    }
    public void OnPlaceLibraryClick()
    {
        objType = 4;
        AddShape();
    }
    public void OnPlaceVendingMachineClick()
    {
        objType = 5;
        AddShape();
    }
    public void OnPlaceOfficeClick()
    {
        objType = 6;
        AddShape();
    }
    public void OnPlaceFactClick()
    {
        //Go through list of facts and restart if all have been used
        if (factCount == 17)
        {
            factCount = 7;
        }
        objType = factCount;
        factCount++;
        AddShape();
    }


    public void AddShape()
    {
        //get rotation and position relative to the camera
        Vector3 placePosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
        Quaternion placeRotation = Camera.main.transform.rotation;
        //Set shape position data based on values retrieved from camera
        int type = objType;
        ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = placePosition.x;
        shapeInfo.py = placePosition.y;
        shapeInfo.pz = placePosition.z;
        shapeInfo.qx = placeRotation.x;
        shapeInfo.qy = placeRotation.y;
        shapeInfo.qz = placeRotation.z;
        shapeInfo.qw = placeRotation.w;
        shapeInfo.shapeType = type;

        shapeInfoList.Add(shapeInfo);

        GameObject shape = ShapeFromInfo(shapeInfo); //Call function to place the shape
        shapeObjList.Add(shape); //Add the shape to the list
    }


    public GameObject ShapeFromInfo(ShapeInfo info)
    {
        GameObject shape = Instantiate(ShapePrefabs[info.shapeType]); //get relevant shape to place
        //Place shape with position,rotation and scale
        shape.transform.position = new Vector3(info.px, info.py, info.pz);
        shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
        shape.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        return shape;
    }

    //Destroy all shapes and clear list
    public void ClearShapes()
    {
        foreach (var obj in shapeObjList)
        {
            Destroy(obj);
        }
        shapeObjList.Clear();
        shapeInfoList.Clear();
    }

    //Converts shape list into JObjct to be stored when saving a route
    public JObject Shapes2JSON()
    {
        ShapeList shapeList = new ShapeList();
        shapeList.shapes = new ShapeInfo[shapeInfoList.Count];
        for (int i = 0; i < shapeInfoList.Count; i++)
        {
            shapeList.shapes[i] = shapeInfoList[i];
        }

        return JObject.FromObject(shapeList);
    }

    //Convert JSON of shapes into objects to place in AR and add to shape
    public void LoadShapesJSON(JToken mapMetadata)
    {
        ClearShapes();
        if (mapMetadata is JObject && mapMetadata["shapeList"] is JObject)
        {
            ShapeList shapeList = mapMetadata["shapeList"].ToObject<ShapeList>();
            if (shapeList.shapes == null)
            {
                Debug.Log("no shapes dropped");
                return;
            }

            foreach (var shapeInfo in shapeList.shapes)
            {
                shapeInfoList.Add(shapeInfo);
                GameObject shape = ShapeFromInfo(shapeInfo);
                shapeObjList.Add(shape);
            }
        }
    }



}
