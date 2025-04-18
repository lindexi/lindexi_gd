namespace QearnallfalweljuGuwolerjajearfairhe;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        var list = new List<DataModel>();
        for (int i = 0; i < 100; i++)
        {
            list.Add(new DataModel(i.ToString()));
        }

        FooListView.ItemsSource = list;
    }
}

public class FooDataTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item)
    {
        UIElement view = new ReadonlyValueEditorView(item);

#if HAS_UNO
        return new DataTemplate(() => view);
#else
        return null!;
#endif
    }
}

public class ReadonlyValueEditorView : UserControl
{
    public ReadonlyValueEditorView(object initialValue)
    {
        //this.DataContext(
        //    new BindableObjectEditorModel()
        //        .WithModel(x => x.Model.Value = initialValue),

        //    (v, vm) => v
        //        .Content(new TextBlock()
        //            .Text(x => x.Binding(() => vm.Value))
        //        ));
        this.DataContext(new ObjectEditorModel()
        {
            Value = initialValue
        }, (view, model) => view.Content(new TextBlock()
            .Text(x => x.Binding(() => model.Value))));
    }
}

public partial record ObjectEditorModel
{
    public ObjectEditorModel()
    {
    }

    public ObjectEditorModel(object? value)
    {
        Value = value;
    }

    public object? Value { get; set; }
}

//internal static class EditorViewExtensions
//{
//    public static TBindable WithModel<TBindable>(this TBindable bindableModel, Action<TBindable> setter)
//        where TBindable : viewmodel
//    {
//        setter(bindableModel);
//        return bindableModel;
//    }
//}

public record DataModel(string Value);
