AmazingPullToRefresh
====

Features
----

- Enables PullToRefresh to ListView, GridView, and more.
- Good pull feeling.

Usage
----

1. Add package from NuGet.
2. Write XAML & CS
3. Done!

Example
----

- MainPage.xaml
```xml
<ListView xmlns:uwp="using:AmazingPullToRefresh.Controls">
    <uwp:PullToRefreshAdorner.Extender>
        <uwp:PullToRefreshExtender RefreshRequested="PullToRefreshExtender_RefreshRequested" />
    </uwp:PullToRefreshAdorner.Extender>
</ListView>
```

- MainPage.xaml.cs
```cs
private async void PullToRefreshExtender_RefreshRequested(object sender, RefreshRequestedEventArgs e)
{
    var deferral = e.GetDeferral();
    await Task.Delay(2500); // something
    deferral.Complete();
}
```

License
----

This library released under the MIT License.