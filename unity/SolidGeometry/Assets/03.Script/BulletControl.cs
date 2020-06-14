using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletControl : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().AddForce(transform.forward * speed);
    }

    public int damage = 20;
    public float speed = 100.0f;

    // Update is called once per frame
    void Update()
    {
        
    }
}
