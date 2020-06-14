using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolidCubeClick : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    public GameObject solideCubeElement;

    private void OnMouseDown()
    {
        var element = solideCubeElement.GetComponent<SolideCubeElement>();
        element.IsClickedSelf = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
