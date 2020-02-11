using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        CircleTransform = GameObject.FindWithTag("Circle").transform;
    }

    public Transform CircleTransform;

    public bool StartFly { set; get; } = false;

    // Update is called once per frame
    void Update()
    {
        if (StartFly)
        {
            transform.Translate(new Vector3(0, 0.1f, 0));

            if (Vector3.Distance(transform.position, new Vector3(0, -2, 0)) < 0.05)
            {
                StartFly = false;

                transform.parent = CircleTransform;
            }
        }
    }
}
