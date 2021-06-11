using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationWithKeyboard : MonoBehaviour
{
    public float Speed = 5;

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

        transform.Rotate(new Vector3(0, horizontal) * Time.deltaTime * Speed, Space.World);
    }
}
