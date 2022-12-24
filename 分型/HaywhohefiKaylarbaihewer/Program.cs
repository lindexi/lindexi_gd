namespace HaywhohefiKaylarbaihewer;

internal class Program
{
    static void Main(string[] args)
    {
        Manager manager = new Manager();
        manager.Start();

        Console.WriteLine("Hello, World!");
    }
}

class Manager : IKeyManager
{
    public Manager()
    {
        KeyList = new List<Key>();
        for (int i = 0; i < 10; i++)
        {
            var key = Random.Shared.Next(Key.MaxKeyValue);
            KeyList.Add(new Key(key));
        }
    }

    public void Start()
    {
        for (int i = 0; i < 10000; i++)
        {
            var element = CreateElement();
            ElementList.Add(element);
        }

        var indexList = new int[3];

        var recreateCount = 0;

        for (int i = 0; i < 10000; i++)
        {
            UpdateRandomIndexList(indexList);
            var key = BuildByKey(this, indexList);

            ElementList.RemoveAll(element => BuildByKey(element, indexList) != key);

            //foreach (var element in ElementList)
            //{
            //    if (element.KeyList.Count > 10)
            //    {
            //    }
            //}

            bool addElement = false;
            var currentCount = ElementList.Count;

            if (currentCount == 0)
            {
                Console.WriteLine("灭");
                recreateCount++;
            }

            while (ElementList.Count < 10000)
            {
                for (int index = 0; index < currentCount; index++)
                {
                    var element = ElementList[index];
                    ElementList.Add(element.Create());
                }

                if (!addElement || currentCount==0)
                {
                    addElement = true;
                    var addCount = 10000 - ElementList.Count;
                    addCount = Math.Min(addCount, 100);
                    for (int index = 0; index < addCount; index++)
                    {
                        var element = CreateElement();
                        ElementList.Add(element);
                    }
                }
            }
        }

        for (var i = 0; i < KeyList.Count; i++)
        {
            var key = GetKey(i);
            ElementList.RemoveAll(element => element.GetKey(i).N != key.N);
        }
    }

    private void UpdateRandomIndexList(int[] indexList)
    {
        for (var i = 0; i < indexList.Length; i++)
        {
            indexList[i] = Random.Shared.Next();
        }
    }

    private int BuildByKey(IKeyManager keyManager, IList<int> indexList)
    {
        var n = 0;
        foreach (var index in indexList)
        {
            var key = keyManager.GetKey(index);
            n += key.N;
        }

        return n;
    }

    private Element CreateElement()
    {
        Element element = new Element(new Random(Random.Shared.Next()));
        element.BuildKey();
        return element;
    }

    public Key GetKey(int n)
    {
        var index = n % KeyList.Count;
        return KeyList[index];
    }

    public List<Key> KeyList { get; }

    public List<Element> ElementList { get; } = new List<Element>();
}

interface IKeyManager
{
    Key GetKey(int n);
}

class Element : IKeyManager
{
    public Element(Random random)
    {
        Random = random;
    }

    public Random Random { get; }

    public void BuildKey()
    {
        const int count = 10;
        while (KeyList.Count<count)
        {
            var key = Random.Next(Key.MaxKeyValue);
            KeyList.Add(new Key(key));
        }

        //KeyList[Random.Next(count)] = new Key(Random.Next(Key.MaxKeyValue));

        var updateCount = Random.Next(1, count);
        for (int i = 0; i < updateCount; i++)
        {
            //var key = Random.Next(Key.MaxKeyValue);
            //KeyList[i] = new Key(key);
            KeyList[Random.Next(count)] = new Key(Random.Next(Key.MaxKeyValue));
        }
    }

    public Key GetKey(int n)
    {
        var index = n % KeyList.Count;
        return KeyList[index];
    }

    public List<Key> KeyList { get; } = new List<Key>();

    public Element Create()
    {
        Element element = new Element(Random);
        foreach (var key in KeyList)
        {
            element.KeyList.Add(key);
        }

        element.BuildKey();

        return element;
    }
}

readonly record struct Key(int N)
{
    public const int MaxKeyValue = 100;
}