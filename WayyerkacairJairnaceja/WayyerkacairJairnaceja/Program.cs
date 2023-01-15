using System.Text.Json.Serialization;

namespace WayyerkacairJairnaceja;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

class ElementManager
{
    public ElementId GenerateId(Element element)
    {
        lock (Locker)
        {
            var id = ElementList.Count;
            ElementList.Add(element);
            return new ElementId(id);
        }
    }

    public Element GetElement(ElementId id)
    {
        lock (Locker)
        {
            return ElementList.First(t => t.Id == id);
        }
    }

    public List<Element> ElementList { private set; get; } = new List<Element>();

    private object Locker => ElementList;
}

readonly record struct ElementId(int Id);

class Element
{
    internal Element()
    {
        // 序列化使用
        ElementManager = null!;
    }

    public Element(ElementManager elementManager)
    {
        ElementManager = elementManager;
        Id = elementManager.GenerateId(this);
    }

    public ElementId Id { private set; get; }

    public IInputValue? InputValue
    {
        set
        {
            lock (_lockObject)
            {
                _inputValue = value;
            }
        }
        get
        {
            lock (_lockObject)
            {
                return _inputValue;
            }
        }
    }

    public IOutputValue? OutputValue
    {
        set
        {
            lock (_lockObject)
            {
                _outputValue = value;
            }
        }
        get
        {
            lock (_lockObject)
            {
                return _outputValue;
            }
        }
    }

    public List<ElementId> InputElementIdList { set; get; } = new List<ElementId>();
    public List<ElementId> OutputElementIdList { set; get; } = new List<ElementId>();

    public IEnumerable<Element> InputElementList
    {
        get
        {
            foreach (var elementId in InputElementIdList)
            {
                yield return ElementManager.GetElement(elementId);
            }
        }
    }

    public IEnumerable<Element> OutputElementList
    {
        get
        {
            foreach (var elementId in OutputElementIdList)
            {
                yield return ElementManager.GetElement(elementId);
            }
        }
    }

    [JsonIgnore]
    private ElementManager ElementManager { set; get; }

    /// <summary>
    /// 序列化保存使用
    /// </summary>
    /// <param name="elementManager"></param>
    internal void InternalSetElementManager(ElementManager elementManager) => ElementManager = elementManager;

    [JsonIgnore]
    private readonly object _lockObject = new object();
    private IInputValue? _inputValue;
    private IOutputValue? _outputValue;
}

interface IInputValue
{

}

interface IOutputValue
{

}