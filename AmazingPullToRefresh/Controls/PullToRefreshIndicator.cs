using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace AmazingPullToRefresh.Controls
{
    public class PullToRefreshIndicator : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(PullToRefreshIndicator), new PropertyMetadata(default(double), OnValueChanged));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IsRefreshingProperty = DependencyProperty.Register(
            "IsRefreshing", typeof(bool), typeof(PullToRefreshIndicator), new PropertyMetadata(default(bool)));

        public bool IsRefreshing
        {
            get { return (bool)GetValue(IsRefreshingProperty); }
            set { SetValue(IsRefreshingProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PullToRefreshIndicator)d).OnValueChanged(e);
        }

        private void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (IsRefreshing || PART_ProgressValue == null) return;
            var p = (double)e.NewValue;
            var w = PART_ProgressValue.StrokeThickness;
            var n = (PART_ProgressValue.ActualWidth - w) * Math.PI * p / w;
            PART_ProgressValue.StrokeDashArray = new DoubleCollection {p > 1 ? 1000 : n, 1000};
            VisualStateManager.GoToState(this, p < 1 ? "PullToRefresh" : "ReleaseToRefresh", true);
        }

        private Ellipse PART_ProgressValue;

        public PullToRefreshIndicator()
        {
            this.DefaultStyleKey = typeof(PullToRefreshIndicator);
        }

        protected override void OnApplyTemplate()
        {
            this.PART_ProgressValue = (Ellipse)GetTemplateChild(nameof(PART_ProgressValue));
        }

        public void BeginRrefresh()
        {
            VisualStateManager.GoToState(this, "Refreshing", true);
            IsRefreshing = true;
        }

        public void EndRefresh()
        {
            Value = 0;
            VisualStateManager.GoToState(this, "PullToRefresh", true);
            IsRefreshing = false;
        }
    }
}
