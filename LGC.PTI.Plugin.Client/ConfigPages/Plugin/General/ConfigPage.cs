using Genetec.Sdk.Queries;
using LGC.PIT.Plugin.Common;
using LGC.PIT.Plugin.Common.Configs;
using System.Linq;

namespace LGC.PTI.Plugin.Client.ConfigPages.Plugin.General;

/// <summary>
/// Provides the configuration page for the LGC PTI plugin in Security Center Config Tool.
/// </summary>
public class ConfigPage : PluginConfigPageBase<ConfigPage, LGCPluginConfig, ConfigPageView>
{
    #region Fields

    /// <summary>
    /// The currently selected cardholder group, or null if none is selected.
    /// </summary>
    private CardholderGroupItem? _selectedCardholderGroup;

    /// <summary>
    /// Indicates whether a connection test operation is currently in progress.
    /// </summary>
    private bool _isTesting;

    /// <summary>
    /// The status message from the most recent connection test.
    /// </summary>
    private string _testStatusMessage = string.Empty;

    /// <summary>
    /// The status icon representing the result of the most recent connection test (e.g., "✔" or "✘").
    /// </summary>
    private string _testStatusIcon = string.Empty;

    /// <summary>
    /// The background color used to display the connection test result.
    /// </summary>
    private Brush _testStatusBackground = Brushes.Transparent;

    /// <summary>
    /// The visibility state of the connection test status display.
    /// </summary>
    private Visibility _testStatusVisibility = Visibility.Collapsed;

    /// <summary>
    /// The command that initiates a connection test operation.
    /// </summary>
    private ICommand? _testConnectionCommand;

    /// <summary>
    /// Indicates whether cardholder groups are currently being loaded or refreshed.
    /// Used to prevent triggering dirty state during programmatic updates.
    /// </summary>
    private bool _isLoadingCardholderGroups;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the plugin configuration page.
    /// </summary>
    public ConfigPage() : base(Globals.PluginTypeGuid)
    {
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the configuration page name displayed in Security Center Config Tool.
    /// </summary>
    protected override string Name { get; } = "Configuration";

    /// <summary>
    /// Gets the available Security Center cardholder groups.
    /// </summary>
    public IReadOnlyCollection<CardholderGroupItem> AllCardholderGroups { get; private set; } = [];

    /// <summary>
    /// Gets or sets the selected cardholder group.
    /// </summary>
    public CardholderGroupItem? SelectedCardholderGroup
    {
        get => _selectedCardholderGroup;
        set
        {
            if (_selectedCardholderGroup?.Guid == value?.Guid)
                return;

            _selectedCardholderGroup = value;

            if (!_isLoadingCardholderGroups)
            {
                var newId = value?.Guid ?? Guid.Empty;

                if (PluginConfig.CardholderGroupId != newId)
                {
                    PluginConfig.CardholderGroupId = newId;
                    IsDirty = true;
                }
            }

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the employee API endpoint.
    /// </summary>
    public string EmployeeUrl
    {
        get => PluginConfig.EmployeeUrl ?? string.Empty;
        set
        {
            if (PluginConfig.EmployeeUrl == value)
                return;

            PluginConfig.EmployeeUrl = value;
            IsDirty = true;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the OAuth token endpoint.
    /// </summary>
    public string TokenURL
    {
        get => PluginConfig.TokenURL ?? string.Empty;
        set
        {
            if (PluginConfig.TokenURL == value)
                return;

            PluginConfig.TokenURL = value;
            IsDirty = true;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the OAuth client identifier.
    /// </summary>
    public string ClientId
    {
        get => PluginConfig.ClientId ?? string.Empty;
        set
        {
            if (PluginConfig.ClientId == value)
                return;

            PluginConfig.ClientId = value;
            IsDirty = true;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the OAuth client secret.
    /// </summary>
    public string ClientSecret
    {
        get => PluginConfig.ClientSecret ?? string.Empty;
        set
        {
            if (PluginConfig.ClientSecret == value)
                return;

            PluginConfig.ClientSecret = value;
            IsDirty = true;
            OnPropertyChanged();
        }
    }

    ///// <summary>
    ///// Gets or sets the Security Center login username.
    ///// </summary>
    //public string SCLoginUsername
    //{
    //    get => PluginConfig.SCLoginUsername ?? string.Empty;
    //    set
    //    {
    //        if (PluginConfig.SCLoginUsername == value)
    //            return;

    //        PluginConfig.SCLoginUsername = value;
    //        IsDirty = true;
    //        OnPropertyChanged();
    //    }
    //}

    ///// <summary>
    ///// Gets or sets the Security Center login password.
    ///// </summary>
    //public string SCLoginPassword
    //{
    //    get => PluginConfig.SCLoginPassword ?? string.Empty;
    //    set
    //    {
    //        if (PluginConfig.SCLoginPassword == value)
    //            return;

    //        PluginConfig.SCLoginPassword = value;
    //        IsDirty = true;
    //        OnPropertyChanged();
    //    }
    //}

    #endregion

    #region Connection Test Properties

    /// <summary>
    /// Gets whether a connection test is currently running.
    /// </summary>
    public bool IsTesting
    {
        get => _isTesting;
        private set
        {
            if (_isTesting == value)
                return;

            _isTesting = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotTesting));
        }
    }

    /// <summary>
    /// Gets whether a connection test can be started.
    /// </summary>
    public bool IsNotTesting => !_isTesting;

    /// <summary>
    /// Gets the message returned by the latest connection test.
    /// </summary>
    public string TestStatusMessage
    {
        get => _testStatusMessage;
        private set
        {
            _testStatusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the status icon for the latest connection test.
    /// </summary>
    public string TestStatusIcon
    {
        get => _testStatusIcon;
        private set
        {
            _testStatusIcon = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the background used to indicate the connection test result.
    /// </summary>
    public Brush TestStatusBackground
    {
        get => _testStatusBackground;
        private set
        {
            _testStatusBackground = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the visibility of the connection test result.
    /// </summary>
    public Visibility TestStatusVisibility
    {
        get => _testStatusVisibility;
        private set
        {
            _testStatusVisibility = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the command that tests the API connection.
    /// </summary>
    public ICommand TestConnectionCommand =>
        _testConnectionCommand ??= new AsyncRelayCommand(RunTestAsync);

    #endregion

    #region Public Methods

    /// <summary>
    /// Retrieves the available cardholder groups from Security Center.
    /// </summary>
    /// <param name="engine">The Security Center SDK engine.</param>
    /// <returns>The cardholder groups ordered by name.</returns>
    public static IReadOnlyCollection<CardholderGroupItem> GetCardholerGroups(IEngine engine)
    {
        return
        [
            .. engine
                .GetEntities(EntityType.CardholderGroup)
                .OfType<CardholderGroup>()
                .OrderBy(x => x.Name)
                .Select(x => new CardholderGroupItem
                {
                    Guid = x.Guid,
                    Name = x.Name,
                    Entity = x
                })
        ];
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Initializes SDK subscriptions and loads cardholder groups when the page is activated.
    /// </summary>
    protected override void Activate()
    {
        base.Activate();

        Workspace.Sdk.EntitiesInvalidated += EngineOnEntitiesInvalidated;

        PrefetchCardholderGroupsSafe(Workspace.Sdk);

        var selectedCardholderGroupId = PluginConfig.CardholderGroupId;
        _isLoadingCardholderGroups = true;


        try
        {
            var cardholderGroupSource = GetCardholerGroups(Workspace.Sdk);

            AllCardholderGroups = cardholderGroupSource;
            OnPropertyChanged(nameof(AllCardholderGroups));
            RestoreSelectedCardholderGroup();
        }
        finally
        {
            _isLoadingCardholderGroups = false;
        }
    }

    /// <summary>
    /// Restores the previously selected cardholder group from the plugin configuration.
    /// Updates the SelectedCardholderGroup property to match the configuration's CardholderGroupId,
    /// or sets it to null if no matching group is found.
    /// </summary>
    private void RestoreSelectedCardholderGroup()
    {
        _selectedCardholderGroup = AllCardholderGroups
            .FirstOrDefault(x => x.Guid == PluginConfig.CardholderGroupId);

        OnPropertyChanged(nameof(SelectedCardholderGroup));
    }

    /// <summary>
    /// Processes updates to cardholder groups from Security Center.
    /// </summary>
    /// <param name="entities">The collection of updated cardholder group entities.</param>
    private void ProcessRoleUpdate(IEnumerable<Entity> entities)
    {
    }

    /// <summary>
    /// Removes SDK subscriptions when the page is deactivated.
    /// </summary>
    protected override void Deactivate()
    {
        base.Deactivate();

        Workspace.Sdk.EntitiesInvalidated -= EngineOnEntitiesInvalidated;
    }

    /// <summary>
    /// Reloads the plugin configuration.
    /// </summary>
    protected override void Refresh()
    {
        _isLoadingCardholderGroups = true;

        try
        {
            RefreshImpl();
            RestoreSelectedCardholderGroup();
        }
        finally
        {
            _isLoadingCardholderGroups = false;
        }

    }

    /// <summary>
    /// Persists the plugin configuration.
    /// </summary>
    protected override void Save()
    {
        SaveImpl();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Prefetches cardholder group configuration data from Security Center.
    /// </summary>
    /// <param name="engine">The Security Center SDK engine.</param>
    private static void PrefetchCardholderGroupsSafe(IEngine engine)
    {
        try
        {
            var query = (EntityConfigurationQuery)engine.ReportManager
                .CreateReportQuery(ReportType.EntityConfiguration);

            query.EntityTypeFilter.Add(EntityType.CardholderGroup);
            query.DownloadAllRelatedData = true;
            query.StrictResults = true;

            query.Query();
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    /// Processes local Security Center entity updates.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The entity invalidation event arguments.</param>
    private void EngineOnEntitiesInvalidated(object sender, EntitiesInvalidatedEventArgs e)
    {
        if (e.IsLocalUpdate)
        {
            ProcessRoleUpdate(e.Entities);
        }
    }

    /// <summary>
    /// Tests the configured API credentials and updates the connection status.
    /// </summary>
    private async Task RunTestAsync()
    {
        IsTesting = true;
        TestStatusVisibility = Visibility.Collapsed;

        var result = await LGCConnectionTester.TestAsync(
            tokenUrl: TokenURL,
            clientId: ClientId,
            clientSecret: ClientSecret);

        TestStatusIcon = result.Success ? "✔" : "✘";
        TestStatusMessage = result.Message;

        TestStatusBackground = result.Success
            ? new SolidColorBrush(Color.FromRgb(34, 139, 34))
            : new SolidColorBrush(Color.FromRgb(180, 40, 40));

        TestStatusVisibility = Visibility.Visible;

        IsTesting = false;

        if (result.Success)
        {
            await Task.Delay(10_000);
            TestStatusVisibility = Visibility.Collapsed;
        }
    }

    #endregion
}