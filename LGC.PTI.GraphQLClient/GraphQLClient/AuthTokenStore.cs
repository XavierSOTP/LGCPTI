using LGC.PTI.Common;
using LGC.PTI.Domain;
using NLog;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.Cache;
using SoftSolute.Common.Utilities.ObjectSS;
using System;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LGC.PTI.Infrastructure
{



    public class AuthTokenStore : IAuthTokenStore
    {
        private const string INITIAL_URL = "http://www.NotAValidUrl.com/";
        private string _token;
        private Uri _url;
        private string _user;
        private string _password;
        private long _lastUpdate;
        //private int _isResetting;
        private int _validInfoRetries;
        private bool _notificationTriggered;
        public SemaphoreSlim _editTokenSemaphore;
        private readonly ILogger _logger;
        private const int MAX_VALID_INFO_RETRY = 5;

        public AuthTokenStore(ILogger logger)
        {
            _token = string.Empty;
            _user = string.Empty;
            _password = string.Empty;
            _url = new Uri(INITIAL_URL);
            LastUpdate = DateTime.MinValue.ToUniversalTime();
            _logger = logger;
            _editTokenSemaphore = new SemaphoreSlim(1);

        }

        #region Events
        public event EventHandler ApiInfoComplete;

        public event EventHandler TokenUpdated;

        #endregion

        #region Public Props
        public DateTime LastUpdate
        {
            get => ObjectHelper.ReadInterlockExchangeDate(ref _lastUpdate);
            set => ObjectHelper.SafeInterLockExchange(ref _lastUpdate, value);
        }


        public Uri Url
        {
            get => _url;
            set
            {
                ObjectHelper.SafeInterLockExchange(ref _url, value);
                DelayNofification();
            }
        }


        public string User
        {
            get => _user;
            set
            {
                ObjectHelper.SafeInterLockExchange(ref _user, value);
                DelayNofification();

            }
        }

        public string Password
        {
            get => _password;

        }


        #endregion

        #region Public Methods





        public void UpdatePassword(string value, bool notify = true)
        {
            ObjectHelper.SafeInterLockExchange(ref _password, value);
            if (notify)
            {
                DelayNofification();
            }

        }
        public bool IsRecentlyUpdated()
        {
            var diff = DateTime.UtcNow - LastUpdate;
            return diff <= TimeSpan.FromSeconds(10);
        }


        public Task StartEditTokenAsync([CallerMemberName] string caller = default)
        {
            _logger.Debug($"{nameof(StartEditTokenAsync)} requested by {caller}");
            return _editTokenSemaphore.WaitAsync();
        }

        public void TryEndEditToken([CallerMemberName] string caller = default)
        {
            _logger.Debug($"{nameof(TryEndEditToken)} requested by {caller}");
            if (_editTokenSemaphore.CurrentCount == 0)
            {
                _editTokenSemaphore?.Release();
            }

        }

        public async Task<string> GetTokenAsync()
        {
            while (_editTokenSemaphore.CurrentCount == 0)
            {
                await Task.Delay(10);
            }
            return _token;
        }


        public async Task UpdateTokenAsync(string token)
        {
            try
            {
                var newToken = token ?? string.Empty;
                await StartEditTokenAsync();
                ObjectHelper.SafeInterLockExchange(ref _token, newToken);
                LastUpdate = DateTime.UtcNow;
                TokenUpdated?.Invoke(this, EventArgs.Empty);

            }
            finally
            {
                TryEndEditToken();
            }

        }

        #endregion

        #region Private Methods

        private async void DelayNofification()
        {
            await StartEditTokenAsync();
            if (!CacheHelper.ExistInBackOff(nameof(AuthTokenStore), nameof(AuthTokenStore)))
            {
                CacheHelper.AddToBackOff(
                    key: nameof(AuthTokenStore),
                    nameof(AuthTokenStore),
                    100,
                    ShouldTriggerUpdate,
                    CacheExpiration.Sliding);
            }
        }


        private async void WaitDataFromUI(int delay)
        {
            await StartEditTokenAsync();

            CacheHelper.AddToBackOff(
                key: nameof(AuthTokenStore),
                nameof(AuthTokenStore),
                delay,
                ShouldTriggerUpdate,
                CacheExpiration.Absolute);
        }

        private void ShouldTriggerUpdate(CacheEntryRemovedArguments obj)
        {
            try
            {
                if (_notificationTriggered)
                {
                    _notificationTriggered = false;
                    return;
                }
                if (IsValidInfo())
                {
                    ApiInfoComplete?.Invoke(this, EventArgs.Empty);
                    _notificationTriggered = true;
                }
                else if (_validInfoRetries < MAX_VALID_INFO_RETRY)
                {
                    Interlocked.Increment(ref _validInfoRetries);
                    _logger.Info("Waiting Api Data from UI");
                    WaitDataFromUI(100);
                }
                else
                {
                    _logger.Error($"{nameof(Password)} is null,Please set the password in the Role and restart the service");
                    Environment.Exit(exitCode: 160);
                }
            }
            finally
            {

                TryEndEditToken();
            }

        }

        private bool IsValidInfo()
        {
            return Url.IsNotNullOrDefault() && User.IsNotNullOrEmpty() && Password.IsNotNullOrEmpty();
        }


        #endregion


    }
}