using LGC.PIT.Plugin.Common.Configs;

namespace LGC.PTI.App.Plugin.Server;

public partial class PluginModule : BasePluginModule<LGCPluginConfig>
{
    #region Fields

    private ExternalProcessManager _manualAppManager;

    protected override Action ReleaseManagedResources { get; } =
        () =>
        {
            // Release any managed resources here if needed.
            // This is a placeholder for future resource management logic.
        };

    #endregion Fields

    #region Public Methods

    [DebugMethod(DisplayName = "¿Está corriendo LGCApp?")]
    [UserCommand("Sample Plugin")]
    public string IsTransactAppRunning()
    {
        if (_manualAppManager == null)
            return "ℹ️ No se ha lanzado ningún proceso desde el plugin aún.";

        return _manualAppManager.IsRunning()
            ? "✅ LGCApp is still running."
            : "❌ LGCApp is not running.";
    }

    [DebugMethod(DisplayName = "¿Está corriendo LGCApp Eff?")]
    [UserCommand("Sample Plugin")]
    public string IsLGCAppEffRunning()
    {
        return (string)IsLGCAppRunningEff().Run();
    }

    [DebugMethod(DisplayName = "Modify Plugin Config from Server")]
    [UserCommand("Sample Plugin")]
    public string ModifyPluginConfigFromServer()
    {
        var pluginRole = Engine.GetEntity(PluginGuid) as Role;

        PluginTodoCode.Instance.CodeReviewRequired("get from here the directory information and Port");
        if (pluginRole == null)
        {
            return "Error";
        }

        var newPlugin = new LGCPluginConfig
        {
        };

        Engine.TransactionManager.QueueUpdate(() => pluginRole.SpecificConfiguration = SerializationHelper.SerializeToString(newPlugin));
        return "Success";
    }

    [DebugMethod(DisplayName = "Lunch LGCApp manually")]
    [UserCommand("Sample Plugin")]
    public string StartLGCAppManually()
    {
        try
        {
            string exePath = @"C:\Users\isguser\source\repos\SoftSolute\KIA-SAP-Genetec\SoftSolute.Genetec.SAP.App\bin\Debug\SoftSolute.Genetec.SAP.App.exe";
            string arguments = "1234";

            Logger?.TraceWarning(exePath);

            _manualAppManager = new ExternalProcessManager(exePath, arguments);
            _manualAppManager.ProcessExited += (s, e) =>
            {
                Logger?.TraceWarning("⚠️ LGCApp manual mode unexpected closed");
            };

            bool started = _manualAppManager.Start();

            return started
                ? "✅ LGCApp launched manually via debug command."
                : "⚠️ LGCApp was already running.";
        }
        catch (Exception ex)
        {
            return $"❌ Error starting LGCApp: {ex.Message}";
        }
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void OnQueryReceived(ReportQueryReceivedEventArgs args)
    {
        throw new NotImplementedException();
    }

    #endregion Protected Methods

    #region Private Methods

    private static Eff<string> IsLGCAppRunningEff() =>
                    Prelude.Eff(() => "✅ LGC is running (simulated check).");

    #endregion Private Methods
}