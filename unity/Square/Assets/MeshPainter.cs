using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MeshPainter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var imageList = Directory.GetFiles(@"D:\lindexi\image");
        foreach (var temp in imageList)
        {
            Debug.Log(temp);
        }

        _imageList = imageList;
    }

    private string[] _imageList;

    // Update is called once per frame
    void Update()
    {
        // 鼠标左键
        if (Input.GetButtonDown("Fire1"))
        {
            var meshRender = gameObject.GetComponent<MeshRenderer>();
            var path = _imageList[_count];
            _count++;

            if (_count == _imageList.Length)
            {
                _count = 0;
            }

            Texture2D texture = new Texture2D(1920, 1080);
            texture.LoadImage(File.ReadAllBytes(path));
            
            meshRender.material.mainTexture = texture;
        }
    }

    private int _count;
}
