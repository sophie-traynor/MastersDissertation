using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SwipeTutorial : MonoBehaviour, IDragHandler, IEndDragHandler 
{
    //https://www.youtube.com/watch?v=rjFgThTjLso


    private Vector3 panelPosition;
    public float swipePercentThreshold = 0.2f;
    public float easing = 0.5f;
    public int totalPanels = 3;
    private int currentPanel = 1;

    public GameObject skipBtn;
    public GameObject doneBtn;

    public Image circle1;
    public Image circle2;
    public Image circle3;

    public Color activeColor;
    public Color notActiveColor;

    // Start is called before the first frame update
    void Start()
    {
        panelPosition = transform.position;
        
    }

    public void OnDrag(PointerEventData data) 
    {
        float xAxisDiff = data.pressPosition.x - data.position.x;
        transform.position = panelPosition - new Vector3(xAxisDiff,0,0);
    }

    public void OnEndDrag(PointerEventData data) 
    {
        float percentScreenWidthSwipe = (data.pressPosition.x - data.position.x) / Screen.width;
        if(Mathf.Abs(percentScreenWidthSwipe) >= swipePercentThreshold)
        {
            Vector3 newPosition = panelPosition;
            if(percentScreenWidthSwipe>0 && currentPanel < totalPanels)
            {
                currentPanel++;
                newPosition += new Vector3(-Screen.width,0,0);
                updateCircleProgressionTransparency();
            }
            else if(percentScreenWidthSwipe < 0 && currentPanel > 1) {
                currentPanel--;
                newPosition += new Vector3(Screen.width,0,0);
                updateCircleProgressionTransparency();
            }
            StartCoroutine(SmoothMove(transform.position, newPosition, easing));
            panelPosition = newPosition;
        }
        else
        {
            StartCoroutine(SmoothMove(transform.position, panelPosition, easing));
        }
    }

    IEnumerator SmoothMove(Vector3 startPos, Vector3 endPos, float seconds )
    {
        float t = 0f;
        while(t<=1.0)
        {
            t+= Time.deltaTime / seconds;
            transform.position=Vector3.Lerp(startPos,endPos,Mathf.SmoothStep(0f,1f,t));
            yield return null;
        }
    }

    //https://answers.unity.com/questions/1121691/how-to-modify-images-coloralpha.html
    public void updateCircleProgressionTransparency()
    {
        if (currentPanel == 1)
        {
            circle1.color = activeColor;
            circle2.color = notActiveColor;
            circle3.color = notActiveColor;
            skipBtn.SetActive(true);
            doneBtn.SetActive(false);
        }
        else if (currentPanel == 2)
        {
            circle1.color = notActiveColor;
            circle2.color = activeColor;
            circle3.color = notActiveColor;
            skipBtn.SetActive(true);
            doneBtn.SetActive(false);
        }
        else if (currentPanel == 3)
        {
            circle1.color = notActiveColor;
            circle2.color = notActiveColor;
            circle3.color = activeColor;
            skipBtn.SetActive(false);
            doneBtn.SetActive(true);
        }
    }
}
