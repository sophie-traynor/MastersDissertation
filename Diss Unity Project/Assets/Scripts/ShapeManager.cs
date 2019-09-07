using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.iOS;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

 //Shape Info Classes

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

public class ShapeManager : MonoBehaviour {

    public List<GameObject> ShapePrefabs = new List<GameObject>();
    public List<ShapeInfo> shapeInfoList = new List<ShapeInfo>();
    public List<GameObject> shapeObjList = new List<GameObject>();
   
    public int objType = 0;

    void Start () {

	}

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



    public void AddShape()
    {
        Vector3 placePosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
        Quaternion placeRotation = Camera.main.transform.rotation;

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

        GameObject shape = ShapeFromInfo(shapeInfo);
        shapeObjList.Add(shape);
    }


    public GameObject ShapeFromInfo(ShapeInfo info)
    {
        GameObject shape = Instantiate(ShapePrefabs[info.shapeType]);
        shape.transform.position = new Vector3(info.px, info.py, info.pz);
        shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
        if (objType == 0)
        {
            shape.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        }
        else if (objType == 1 || objType == 2 || objType == 3)
        {
            shape.transform.localScale = new Vector3(0.03f, 0.08f, 0.01f);
        }
        return shape;
    }

    public void ClearShapes()
    {
        foreach (var obj in shapeObjList)
        {
            Destroy(obj);
        }
        shapeObjList.Clear();
        shapeInfoList.Clear();
    }


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
