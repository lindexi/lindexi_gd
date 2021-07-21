using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class SunshineFallDown : MonoBehaviour
{
    public GameObject SunshinePrefabs;

    public float MinX = -1;
    public float MinZ = -0.7f;
    public float MaxX = 1.2f;
    public float MaxZ = 0.5f;

    // Start is called before the first frame update
    async void Start()
    {
        for (int i = 0; Application.isPlaying; i++)
        {
            var sunshine = Instantiate(SunshinePrefabs);
            sunshine.transform.position = new Vector3(Random.Range(MinX, MaxX), y: 2, Random.Range(MinZ, MaxZ));

            var rigidbody = sunshine.GetComponent<Rigidbody>();
            rigidbody.velocity = new Vector3(0, Random.Range(-0.2f, -0.1f), 0);

            sunshine.AddComponent<DestroyObjectPlane>();

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
