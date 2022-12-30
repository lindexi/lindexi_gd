using System.ComponentModel;
using System.Windows;

namespace BefawafereKehufallkee;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var application = new Application();
        application.Startup += Application_Startup;
        application.Run();
    }

    private static void Application_Startup(object sender, StartupEventArgs e)
    {
    }

    public class ClrBindingHelper
    {
        /// <summary>
        /// 设置源到目标的单向绑定
        /// </summary>
        /// <param name="source">数据源，通常指底层提供的数据类</param>
        /// <param name="sourcePropertyPath">数据源中，属性的路径（名称）</param>
        /// <param name="target">目标，通常指 ViewModel，需要跟随源的变化而更新数据</param>
        /// <param name="targetPropertyPath">目标类中，属性的路径（名称）</param>
        public static void SetBindingByOneWay(
            object source, string sourcePropertyPath,
            object target, string targetPropertyPath)
        {
            BidirectionalBindingOperations.SetBinding(target, targetPropertyPath, new BidirectionalBinding(source, sourcePropertyPath)
            {
                Direction = BindingDirection.OneWay,
                InitMode = BindingInitMode.SourceToTarget
            });
        }

    }

    /// <summary>
    /// 实现两个 CLR 属性的双向绑定
    /// </summary>
    public class BidirectionalBinding
    {
        private delegate object ConverterDelegate(object obj, Type targetType, object parameter, out bool success);

        private Type _sourcePropertyType;
        private Type _targetPropertyType;

        /// <summary>
        /// 绑定源，指被绑定对象。
        /// </summary>
        public WeakReference BindableSource { get; set; }

        /// <summary>
        /// 绑定到源的属性的属性名
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// 绑定目标，指将值绑定到源的对象。
        /// </summary>
        public WeakReference BindableTarget { get; set; }

        /// <summary>
        /// 目标的用于绑定到源的属性名。
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// 绑定方向，默认 TwoWay。  
        /// </summary>
        public BindingDirection Direction { get; set; } = BindingDirection.TwoWay;

        /// <summary>
        /// 初始化模式，默认 SourceToTarget。
        /// SourceToTarget：初始值以 Source 为准；TargetToSource：初始值以 Target 为准。
        /// </summary>
        public BindingInitMode InitMode { get; set; } = BindingInitMode.SourceToTarget;

        public IClrValueConverter ValueConverter { get; set; }

        internal BidirectionalBinding(object bindableSource, string sourcePath, object bindableTarget,
            string targetPath)
        {
            BindableSource = new WeakReference(bindableSource);
            SourcePath = sourcePath;
            BindableTarget = new WeakReference(bindableTarget);
            TargetPath = targetPath;

            InnerSetBinding();
        }

        public BidirectionalBinding(object bindableSource, string sourcePath)
        {
            BindableSource = new WeakReference(bindableSource);
            SourcePath = sourcePath;
        }


        /// <summary>
        /// 在所有参数都准备好之后，使用此方法建立绑定
        /// </summary>
        internal void InnerSetBinding()
        {
            VerifyProperty();

            var source = BindableSource.Target;
            var target = BindableTarget.Target;

            if (source is null || target is null)
            {
                return;
            }

            var sourceNotifyPropertyChanged = (INotifyPropertyChanged) source;
            var targetNotifyPropertyChanged = (INotifyPropertyChanged) target;

            sourceNotifyPropertyChanged.PropertyChanged += Source_OnPropertyChanged;
            targetNotifyPropertyChanged.PropertyChanged += Target_OnPropertyChanged;

            // 初始化赋值。
            switch (InitMode)
            {
                // 初始值以 Source 为准。
                case BindingInitMode.SourceToTarget:

                    bool success1 = true;
                    var sourceValue = ValueConverter == null
                        ? GetValue(source, SourcePath)
                        : GetValue(source, SourcePath, ValueConverter.Convert, _targetPropertyType, null,
                            out success1);

                    if (success1)
                    {
                        SetValue(target, TargetPath, sourceValue);
                    }

                    break;

                // 初始值以 Target 为准。
                case BindingInitMode.TargetToSource:

                    bool success2 = true;
                    var targetValue = ValueConverter == null
                        ? GetValue(target, TargetPath)
                        : GetValue(target, TargetPath, ValueConverter.ConvertBack, _sourcePropertyType,
                            null, out success2);

                    if (success2)
                    {
                        SetValue(source, SourcePath, targetValue);
                    }

                    break;
            }
        }

        internal bool IsAlive()
        {
            return BindableSource.IsAlive && BindableTarget.IsAlive;
        }

        private void Source_OnPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (!BindableTarget.IsAlive)
            {
                return;
            }

            if (propertyChangedEventArgs.PropertyName != SourcePath)
            {
                return;
            }

            var success = true;
            var value = ValueConverter == null
                ? GetValue(BindableSource.Target, SourcePath)
                : GetValue(BindableSource.Target, SourcePath, ValueConverter.Convert, _targetPropertyType, null,
                    out success);
            if (success)
            {
                SetValue(BindableTarget.Target, TargetPath, value);
            }
        }

        private void Target_OnPropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (Direction == BindingDirection.OneWay)
            {
                return;
            }

            if (!BindableSource.IsAlive)
            {
                return;
            }

            if (propertyChangedEventArgs.PropertyName != TargetPath)
            {
                return;
            }

            var success = true;
            var value = ValueConverter == null
                ? GetValue(BindableTarget.Target, TargetPath)
                : GetValue(BindableTarget.Target, TargetPath, ValueConverter.ConvertBack, _sourcePropertyType, null,
                    out success);
            if (success)
            {
                SetValue(BindableSource.Target, SourcePath, value);
            }
        }

        private void VerifyProperty()
        {
            if (BindableSource == null)
            {
                throw new InvalidOperationException($"{nameof(BindableSource)} can not be null");
            }

            if (string.IsNullOrWhiteSpace(SourcePath))
            {
                throw new InvalidOperationException($"{SourcePath} can not be null");
            }

            if (BindableTarget == null)
            {
                throw new InvalidOperationException($"{nameof(BindableTarget)} can not be null");
            }

            if (string.IsNullOrWhiteSpace(TargetPath))
            {
                throw new InvalidOperationException($"{TargetPath} can not be null");
            }

            if (!(BindableSource.Target is INotifyPropertyChanged))
            {
                throw new InvalidOperationException($"{BindableSource} not implement interface INotifyPropertyChanged");
            }

            if (!(BindableTarget.Target is INotifyPropertyChanged))
            {
                throw new InvalidOperationException($"{BindableTarget} not implement interface INotifyPropertyChanged");
            }

            _sourcePropertyType = BindableSource.Target.GetType().GetProperty(SourcePath)?.PropertyType;
            if (_sourcePropertyType == null)
            {
                throw new InvalidOperationException($"Can not get property {SourcePath} of {BindableSource.Target}.");
            }

            _targetPropertyType = BindableTarget.Target.GetType().GetProperty(TargetPath)?.PropertyType;
            if (_targetPropertyType == null)
            {
                throw new InvalidOperationException($"Can not get property {TargetPath} of {BindableTarget.Target}.");
            }

            if (_sourcePropertyType != _targetPropertyType && ValueConverter == null)
            {
                throw new InvalidOperationException(
                    "Can Not binding different type of two properties without implementing a converter.");
            }
        }

        private void SetValue(object obj, string propertyPath, object value)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyPath);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"{obj.GetType().Name} dose not have property {propertyPath}");
            }

            propertyInfo.SetValue(obj, value);
        }

        private object GetValue(object obj, string propertyPath)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyPath);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"{obj.GetType().Name} dose not have property {propertyPath}");
            }

            return propertyInfo.GetValue(obj);
        }

        private object GetValue(object obj, string propertyPath, ConverterDelegate converter, Type targetType,
            object parameter, out bool success)
        {
            success = true;

            if (converter == null)
            {
                return GetValue(obj, propertyPath);
            }

            return converter(GetValue(obj, propertyPath), targetType, parameter, out success);
        }
    }

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

    public interface IClrValueConverter
    {
        object Convert(object value, Type targetType, object parameter, out bool success);
        object ConvertBack(object value, Type targetType, object parameterm, out bool success);
    }

    /// <summary>
    /// 绑定初始化时的值传递方向
    /// </summary>
    public enum BindingInitMode
    {
        /// <summary>
        /// 使用被绑定对象的值设置当前对象的值（默认）
        /// </summary>
        SourceToTarget = 0,

        /// <summary>
        /// 使用当前对象的值设置被绑定对象的值
        /// </summary>
        TargetToSource = 1,
    }
}