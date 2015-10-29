using Windows.UI.Xaml;

namespace AmazingPullToRefresh.Controls
{
    public class PullToRefreshAdorner : DependencyObject
    {
        public static readonly DependencyProperty ExtenderProperty = DependencyProperty.RegisterAttached(
            "Extender", typeof(PullToRefreshExtender), typeof(PullToRefreshAdorner), new PropertyMetadata(default(PullToRefreshExtender)));

        public static void SetExtender(DependencyObject element, PullToRefreshExtender value)
        {
            ((PullToRefreshExtender)element.GetValue(ExtenderProperty))?.Detach(element);
            element.SetValue(ExtenderProperty, value);
            value?.Attach(element);
        }

        public static PullToRefreshExtender GetExtender(DependencyObject element)
        {
            return (PullToRefreshExtender)element.GetValue(ExtenderProperty);
        }
    }
}
