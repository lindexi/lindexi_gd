using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public float Speed = 1;

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

        transform.Translate(new Vector3(horizontal, vertical) * Time.deltaTime * Speed);
    }
}
