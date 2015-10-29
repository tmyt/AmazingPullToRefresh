using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace AmazingPullToRefresh.Extensions
{
    public static class VisualTreeHelperExtensions
    {
        public static T FindFirstChild<T>(this DependencyObject d)
            where T : DependencyObject
        {
            var q = new Queue<DependencyObject>();
            q.Enqueue(d);
            while (q.Count > 0)
            {
                var e = q.Dequeue();
                var n = VisualTreeHelper.GetChildrenCount(e);
                for (var i = 0; i < n; ++i)
                {
                    var c = VisualTreeHelper.GetChild(e, i);
                    if (c is T) return (T)c;
                    q.Enqueue(c);
                }
            }
            return null;
        }
    }
}
