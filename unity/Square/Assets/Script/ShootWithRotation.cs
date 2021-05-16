using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootWithRotation : MonoBehaviour
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

            // 向着用户看到方向
            sphere.transform.rotation = Player.transform.rotation;
           
            var rigidbody = sphere.GetComponent<Rigidbody>();

            rigidbody.velocity = sphere.transform.forward * Speed;

            //// 旋转发射
            //var rotation = Player.transform.eulerAngles.y;
            //var vx = 1 * Mathf.Cos(rotation);
            //var vy = -1 * Mathf.Sin(rotation);
            //rigidbody.velocity = new Vector3(vx, 0, vy) * Speed;
        }
    }
}
