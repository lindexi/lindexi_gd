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

        _imageList = imageList;

        var renderTexture = new RenderTexture(1920, 1080, 8);
        RenderTexture = renderTexture;

        Texture2D texture = new Texture2D(Width, Height);
        _texture2D = texture;

        var meshRender = gameObject.GetComponent<MeshRenderer>();
        meshRender.material.mainTexture = _texture2D;
    }

    private const int Width = 1920;
    private const int Height = 1080;

    private Texture2D _texture2D;

    public RenderTexture RenderTexture;


    private string[] _imageList;



    // Update is called once per frame
    void Update()
    {
        // 鼠标左键
        //if (Input.GetButtonDown("Fire1"))
        {
            for (int i = 0; i < Width; i++)
            {
                var c = new Color(Random.value, Random.value, Random.value);
                for (int j = 0; j < Height; j++)
                {
                    Color color = ((i & j) != 0 ? Color.white : Color.gray);
                    _texture2D.SetPixel(i, j, c);
                }
            }

            _texture2D.Apply();
        }
    }

    private int _count;
}
