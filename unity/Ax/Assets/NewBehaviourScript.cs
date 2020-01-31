using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var horizontalAsixName = "Horizontal";
        var horizontal = Input.GetAxis(horizontalAsixName);

        var verticalAsixName = "Vertical";
        var vertical = Input.GetAxis(verticalAsixName);

        var z = 0f;
        if (Input.GetMouseButtonDown(0))
        {
            z = 1f;
        }
        else if(Input.GetMouseButtonDown(1))
        {
            z = -1f;
        }

        transform.Translate(new Vector3(horizontal, vertical, z) * 0.5f);

        if (Input.GetKeyDown(KeyCode.Q))
        {
            transform.Rotate(new Vector3(0, -10));
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            transform.Rotate(new Vector3(0, 10));
        }

    }
}
