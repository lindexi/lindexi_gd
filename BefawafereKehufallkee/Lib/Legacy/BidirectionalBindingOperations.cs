namespace BefawafereKehufallkee;

/// <summary>
/// CLR 双向绑定操作
/// </summary>
public class BidirectionalBindingOperations
{
    private static readonly List<BidirectionalBinding> BidirectionalBindingPool = new List<BidirectionalBinding>();

    public static void SetBinding(
        object source, string sourcePropertyPath,
        object target, string targetPropertyPath)
    {
        var binding = new BidirectionalBinding(source, sourcePropertyPath, target, targetPropertyPath);
        BidirectionalBindingPool.Add(binding);
        CleanBindingPool();
    }

    public static void SetBinding(object target, string targetPropertyPath, BidirectionalBinding binding)
    {
        binding.BindableTarget = new WeakReference(target);
        binding.TargetPath = targetPropertyPath;
        binding.InnerSetBinding();

        BidirectionalBindingPool.Add(binding);
        CleanBindingPool();
    }

    private static void CleanBindingPool()
    {
        BidirectionalBindingPool.RemoveAll(binding => !binding.IsAlive());
    }
}