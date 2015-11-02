using Windows.Foundation;

namespace AmazingPullToRefresh.Controls
{
    public class RefreshRequestedEventArgs
    {
        private DeferralCompletedHandler _deferralCompletedHandler;
        private bool _isDeferred;

        internal bool IsDeferred => _isDeferred;

        internal RefreshRequestedEventArgs(DeferralCompletedHandler deferralCompletedHandler)
        {
            _deferralCompletedHandler = deferralCompletedHandler;
        }

        public bool Cancel { get; set; }

        public Deferral GetDeferral()
        {
            _isDeferred = true;
            return new Deferral(_deferralCompletedHandler);
        }
    }
}
