using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class Move : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    public float Speed = 10;

    // Update is called once per frame
    void Update()
    {
        var horizontalAsixName = "Horizontal";
        var horizontal = Input.GetAxis(horizontalAsixName);

        var verticalAsixName = "Vertical";
        var vertical = Input.GetAxis(verticalAsixName);

        var z = 0f;

        if (Input.GetButton("Fire1"))
        {
            z = 1f;
        }
        else if (Input.GetButton("Fire2"))
        {
            z = -0.1f;
        }

        transform.Translate(new Vector3(horizontal, vertical, z) * Time.deltaTime * Speed, Space.World);
    }
}
