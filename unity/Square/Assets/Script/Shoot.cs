using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    public GameObject Sphere;
    public GameObject Player;

    public float Speed = 5;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            var sphere = GameObject.Instantiate(Sphere);

            sphere.transform.Translate(base.transform.position);
            sphere.transform.Translate(new Vector3(0, 0, 1));

            //// 向着用户看到方向
            //sphere.transform.Rotate(new Vector3(0, Player.transform.rotation.y), Space.World);

            var rigidbody = sphere.GetComponent<Rigidbody>();
            rigidbody.velocity = new Vector3(0, 0, 1) * Speed;
            //rigidbody.MoveRotation(Player.transform.rotation);
        }
    }
}
