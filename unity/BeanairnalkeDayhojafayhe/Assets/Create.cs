using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Create : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            // 鼠标左键
            var cube = GameObject.Instantiate(Cube);
            cube.transform.Translate(transform.position);
            cube.transform.Translate(0, 0, 10);
        }
    }

    public GameObject Cube;
}
