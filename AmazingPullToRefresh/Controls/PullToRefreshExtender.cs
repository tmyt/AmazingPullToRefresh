using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using AmazingPullToRefresh.Extensions;
using Windows.UI;

namespace AmazingPullToRefresh.Controls
{
    public class PullToRefreshExtender : DependencyObject
    {
        public static readonly DependencyProperty IsRefreshEnabledProperty = DependencyProperty.Register(
            "IsRefreshEnabled", typeof(bool), typeof(PullToRefreshExtender), new PropertyMetadata(true, OnIsRefreshEnabledChanged));

        public bool IsRefreshEnabled
        {
            get { return (bool)GetValue(IsRefreshEnabledProperty); }
            set { SetValue(IsRefreshEnabledProperty, value); }
        }

        public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
            "Threshold", typeof(double), typeof(PullToRefreshExtender), new PropertyMetadata(60.0));

        public double Threshold
        {
            get { return (double)GetValue(ThresholdProperty); }
            set { SetValue(ThresholdProperty, value); }
        }

        private static void OnIsRefreshEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var i = (PullToRefreshExtender)d;
            if (i._indicator == null) return;
            i._indicator.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public delegate void RefreshRequestedEventHandler(object sender, RefreshRequestedEventArgs e);
        public event RefreshRequestedEventHandler RefreshRequested;

        // 引っ張って更新エリアの高さ
        private const double IndicatorHeight = 50;

        // ターゲットScrollViewer
        private Grid _container;
        private ScrollViewer _scrollViewer;
        private ScrollContentPresenter _presenter;
        private PullToRefreshIndicator _indicator;

        // 更新処理中？
        private bool _isRefreshing;
        // 慣性スクロールでの移動を無視する？
        private bool _inertiaIgnoring;
        // Manipulationを開始したx, y 座標
        private double _manipulationStartedX, _manipulationStartedY;
        // 慣性スクロールで境界エフェクトを表示し始めた時刻
        private long _inertiaStarted;

        private void TargetView_Loaded(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).Loaded -= TargetView_Loaded;
            // try access scrollviewer
            var sv = ((DependencyObject)sender).FindFirstChild<ScrollViewer>();
            if (sv != null)
            {
                UpdateProperties(sv);
            }
        }

        private void ScrollContentPresenter_OnManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            _manipulationStartedX = _scrollViewer.HorizontalOffset;
            _manipulationStartedY = _scrollViewer.VerticalOffset;
        }

        private void ScrollContentPresenter_OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            var tr = _presenter.RenderTransform as TranslateTransform;
            // 閾値に達していない状態で慣性スクロールが始まった場合引っ張って更新の計算をスキップする
            _inertiaIgnoring = tr.Y < Threshold && !_isRefreshing;
        }

        private void ScrollContentPresenter_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.PointerDeviceType != PointerDeviceType.Touch) return;
            // 境界エフェクトを出す距離を計算
            var overhangX = _manipulationStartedX - e.Cumulative.Translation.X;
            var overhangY = _manipulationStartedY - e.Cumulative.Translation.Y;
            // 更新中のヘッダオフセットを計算
            var refreshingOffset = _isRefreshing ? IndicatorHeight : 0;
            // ScrollViewerを正しい位置へスクロール
            _scrollViewer.ChangeView(_manipulationStartedX - e.Cumulative.Translation.X, _manipulationStartedY - e.Cumulative.Translation.Y, null);
            // 境界エフェクトの計算をします。
            var tr = _presenter.RenderTransform as TranslateTransform;
            // スクロールが無効の時は処理しない
            if (_scrollViewer.HorizontalScrollMode == ScrollMode.Disabled) { }
            // 左端よりさらにスクロールされた場合は、距離の1/4を境界エフェクトとして表示
            else if (overhangX < 0) { tr.X = (-overhangX) / 4; }
            // 右端よりさらにスクロールされた場合は、同様に距離の1/4を境界エフェクトとして表示
            else if (overhangX > _scrollViewer.ScrollableWidth) { tr.X = (_scrollViewer.ScrollableWidth - overhangX) / 4; }
            // どちらでもない場合はRenderTransformを初期化
            else { tr.X = 0; }
            // 水平スクロールと同じ感じ
            if (_scrollViewer.VerticalScrollMode == ScrollMode.Disabled) { }
            // 更新中のヘッダオフセットを考慮して境界エフェクトを表示
            else if (overhangY < 0) { tr.Y = refreshingOffset + (-overhangY) / 4; }
            // 下端の処理
            else if (overhangY > _scrollViewer.ScrollableHeight) { tr.Y = (_scrollViewer.ScrollableHeight - overhangY) / 4; }
            // どちらでもなく、更新処理中でなければRenderTransformを初期化
            else if (!_isRefreshing) { tr.Y = 0; }
            // 引っ張って更新のインジケータを更新する
            if (!_isRefreshing)
            {
                ((TranslateTransform)_indicator.RenderTransform).Y = -IndicatorHeight + tr.Y;
                _indicator.Value = tr.Y / Threshold;
            }
            // 慣性スクロール中で、境界エフェクトを表示すべき条件が整った
            if ((Math.Abs(tr.X) > 0 || tr.Y > refreshingOffset || tr.Y < 0) && e.IsInertial)
            {
                // 初回は時刻を記録
                if (_inertiaStarted == 0)
                {
                    _inertiaStarted = DateTime.UtcNow.Ticks;
                }
                // 慣性スクロールで境界エフェクトを300ms以上表示した場合Manipulationを終了
                if ((DateTime.UtcNow.Ticks - _inertiaStarted) > 1000000) // 100ms
                {
                    e.Complete();
                }
            }
        }

        private void ScrollContentPresenter_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var tr = _presenter.RenderTransform as TranslateTransform;
            // check refresh
            if (IsRefreshEnabled && !_isRefreshing && !_inertiaIgnoring && tr.Y > Threshold)
            {
                FireRefresh();
            }
            _inertiaIgnoring = false;
            _inertiaStarted = 0;
            // それなりにアニメーションさせる
            BeginReleaseAnimation();

            // メッセージを更新
            if (_isRefreshing)
            {
                _indicator.BeginRrefresh();
            }
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _container.Clip.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        private void BeginReleaseAnimation()
        {
            var tr = _presenter.RenderTransform as TranslateTransform;
            var btr = _indicator.RenderTransform as TranslateTransform;
            var sb = new Storyboard();
            var xanim = new DoubleAnimation
            {
                From = tr.X,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            var yanim = new DoubleAnimation
            {
                From = tr.Y,
                To = _isRefreshing ? IndicatorHeight : 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            var bdranim = new DoubleAnimation
            {
                From = btr.Y,
                To = _isRefreshing ? 0 : -IndicatorHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(xanim, _presenter);
            Storyboard.SetTargetProperty(xanim, "(UIElement.RenderTransform).(TranslateTransform.X)");
            Storyboard.SetTarget(yanim, _presenter);
            Storyboard.SetTargetProperty(yanim, "(UIElement.RenderTransform).(TranslateTransform.Y)");
            Storyboard.SetTarget(bdranim, _indicator);
            Storyboard.SetTargetProperty(bdranim, "(UIElement.RenderTransform).(TranslateTransform.Y)");
            sb.Children.Add(xanim);
            sb.Children.Add(yanim);
            sb.Children.Add(bdranim);
            sb.Begin();
        }

        private void FireRefresh()
        {
            var e = new RefreshRequestedEventArgs();
            RefreshRequested?.Invoke(this, e);
            if (e.Cancel) return;
            _isRefreshing = true;
        }

        private void UpdateProperties(ScrollViewer sv)
        {
            // find child
            _scrollViewer = sv;
            _presenter = _scrollViewer.FindFirstChild<ScrollContentPresenter>();
            _container = (Grid)VisualTreeHelper.GetParent(_presenter);

            // add indicator
            _indicator = new PullToRefreshIndicator();
            _indicator.Height = IndicatorHeight;
            _indicator.VerticalAlignment = VerticalAlignment.Top;
            _indicator.RenderTransform = new TranslateTransform { Y = -IndicatorHeight };
            _indicator.Visibility = IsRefreshEnabled ? Visibility.Visible : Visibility.Collapsed;
            _container.Children.Insert(0, _indicator);

            // update properties
            _container.SizeChanged += Grid_SizeChanged;
            _container.Clip = new RectangleGeometry { Rect = new Rect(0, 0, _container.ActualWidth, _container.ActualHeight) };
            _presenter.ManipulationMode = ManipulationModes.TranslateY | ManipulationModes.TranslateRailsY |
                                          ManipulationModes.TranslateInertia | ManipulationModes.System;
            _presenter.ManipulationStarting += ScrollContentPresenter_OnManipulationStarting;
            _presenter.ManipulationInertiaStarting += ScrollContentPresenter_OnManipulationInertiaStarting;
            _presenter.ManipulationDelta += ScrollContentPresenter_OnManipulationDelta;
            _presenter.ManipulationCompleted += ScrollContentPresenter_OnManipulationCompleted;
            _presenter.RenderTransform = new TranslateTransform();
            _presenter.Background = new SolidColorBrush(Colors.Transparent);
        }

        public void CompleteRefresh()
        {
            if (!_isRefreshing) return;
            _isRefreshing = false;
            // それなりなアニメーションを実行する
            BeginReleaseAnimation();
            // メッセージを更新
            _indicator.EndRefresh();
        }

        #region Interop for PullToRefreshAdorner
        internal void Attach(DependencyObject element)
        {
            // try access scrollviewer
            var sv = element.FindFirstChild<ScrollViewer>();
            if (sv == null)
            {
                // if null, wait loaded
                ((FrameworkElement)element).Loaded += TargetView_Loaded;
                return;
            }
            UpdateProperties(sv);
        }

        internal void Detach(DependencyObject element)
        {
            if (_indicator == null) return;

            // remove indicator
            _container.Children.Remove(_indicator);

            // restore properties
            _container.SizeChanged -= Grid_SizeChanged;
            _container.Clip = null;
            _presenter.ManipulationMode = ManipulationModes.System;
            _presenter.ManipulationStarting -= ScrollContentPresenter_OnManipulationStarting;
            _presenter.ManipulationInertiaStarting -= ScrollContentPresenter_OnManipulationInertiaStarting;
            _presenter.ManipulationDelta -= ScrollContentPresenter_OnManipulationDelta;
            _presenter.ManipulationCompleted -= ScrollContentPresenter_OnManipulationCompleted;
            _presenter.RenderTransform = null;
        }
        #endregion
    }
}
