using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SolideCubeElement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        HideControllArrow();
    }

    public GameObject cube;
    public GameObject controlArrowX;
    public GameObject controlArrowY;
    public GameObject controlArrowZ;

    public void ShowControlArrow()
    {
        controlArrowX.SetActive(true);
        controlArrowY.SetActive(true);
        controlArrowZ.SetActive(true);
    }

    public void HideControllArrow()
    {
        controlArrowX.SetActive(false);
        controlArrowY.SetActive(false);
        controlArrowZ.SetActive(false);
    }

    public bool IsClickedSelf { set; get; }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsClickedSelf)
            {
                ShowControlArrow();
            }
            else
            {
                HideControllArrow();
            }

            IsClickedSelf = false;
        }
    }
}
