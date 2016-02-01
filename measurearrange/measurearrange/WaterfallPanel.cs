using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace measurearrange
{
    public class WaterfallPanel : Windows.UI.Xaml.Controls.Panel
    {
        public int ColumnNum { get; set; }

        public WaterfallPanel()
        {

        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // 记录每个流的长度。因为我们用选取最短的流来添加下一个元素。  
            KeyValuePair<double, int>[] flowLens = new KeyValuePair<double, int>[ColumnNum];
            foreach (int idx in Enumerable.Range(0, ColumnNum))
            {
                flowLens[idx] = new KeyValuePair<double, int>(0.0, idx);
            }

            // 我们就用2个纵向流来演示，获取每个流的宽度。  
            double flowWidth = availableSize.Width / ColumnNum;

            // 为子控件提供沿着流方向上，无限大的空间  
            Size elemMeasureSize = new Size(flowWidth, double.PositiveInfinity);

            foreach (UIElement elem in Children)
            {
                // 让子控件计算它的大小。  
                elem.Measure(elemMeasureSize);
                Size elemSize = elem.DesiredSize;

                double elemLen = elemSize.Height;
                var pair = flowLens[0];

                // 子控件添加到最短的流上，并重新计算最短流。  
                // 因为我们为了求得流的长度，必须在计算大小这一步时就应用一次布局。但实际的布局还是会在Arrange步骤中完成。  
                flowLens[0] = new KeyValuePair<double, int>(pair.Key + elemLen, pair.Value);
                flowLens = flowLens.OrderBy(p => p.Key).ToArray();
            }
            return new Size(availableSize.Width, flowLens.Last().Key);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // 同样记录流的长度。  
            KeyValuePair<double, int>[] flowLens = new KeyValuePair<double, int>[ColumnNum];

            double flowWidth = finalSize.Width / ColumnNum;

            // 要用到流的横坐标了，我们用一个数组来记录（其实最初是想多加些花样，用数组来方便索引横向偏移。不过本例中就只进行简单的乘法了）  
            double[] xs = new double[ColumnNum];

            foreach (int idx in Enumerable.Range(0, ColumnNum))
            {
                flowLens[idx] = new KeyValuePair<double, int>(0.0, idx);
                xs[idx] = idx * flowWidth;
            }

            foreach (UIElement elem in Children)
            {
                // 直接获取子控件大小。  
                Size elemSize = elem.DesiredSize;
                double elemLen = elemSize.Height;

                var pair = flowLens[0];
                double chosenFlowLen = pair.Key;
                int chosenFlowIdx = pair.Value;

                // 此时，我们需要设定新添加的空间的位置了，其实比measure就多了一个Point信息。接在流中上一个元素的后面。  
                Point pt = new Point(xs[chosenFlowIdx], chosenFlowLen);

                // 调用Arrange进行子控件布局。并让子控件利用上整个流的宽度。  
                elem.Arrange(new Rect(pt, new Size(flowWidth, elemSize.Height)));

                // 重新计算最短流。  
                flowLens[0] = new KeyValuePair<double, int>(chosenFlowLen + elemLen, chosenFlowIdx);
                flowLens = flowLens.OrderBy(p => p.Key).ToArray();
            }

            // 直接返回该方法的参数。  
            return finalSize;
        }
    }
}
