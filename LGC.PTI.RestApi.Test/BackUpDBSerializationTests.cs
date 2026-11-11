using LGC.PTI.Domain.TypeRepresentation;
using SoftSolute.Common.Utilities.CollectionSS;
using SoftSolute.Common.Utilities.FileSS;
using SoftSolute.Common.Utilities.Tasks;
using SoftSolute.Sdk.Common.CustomEntitiesSS;
using SoftSolute.Sdk.Common.Models;
using System.IO;
using System.Text;

namespace LGC.PTI.RestApi.Test;

public class BackUpDBSerializationTests
{
    #region Public Methods

    [Test]
    public void Populate()
    {
        var cfs = _roleDefinition.GetCustomFields();
        var list = new List<CustomFieldDataBO>();
        cfs.ForEach(cf => { cf.CustomFieldId = Guid.NewGuid(); list.Add(cf); });
        _roleDefinition.Populate(list);
        _cRoleTypeBO1.CustomFields = _roleDefinition.GetCustomFields().ToList();
        var app = new SdkApplicationConfig(_cRoleTypeBO1);
        app.ValidateConfig();
        var json = JsonConvert.SerializeObject(app, new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented
        });
        _logger.Info(json);
    }

    [Test]
    public async Task SerializeAsync()
    {
        var path = @"H:\SSBackUP\Git\Actives\QSL-PTI\LGC.PTI\LGC.PTI.Tests1\Resources";
        var x = ProjectResourcesHelper.GetResourcesPath();
        var crCFs = CredentialDefinition.Create().GetCustomFields();
        var chCFs = CardholderDefinition.Create().GetCustomFields();
        var cfields = crCFs.Concat(chCFs).ToList();
        var app = new SdkApplicationConfig(_cRoleTypeBO1)
        {
            ExternalCustomFields = cfields
        };
        app.ValidateConfig();
        var json = JsonConvert.SerializeObject(app, new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented
        });

        var fileName = Path.Combine(path, "DBBackUP.json");
        _logger.Info(fileName);
        _logger.Info(json);
        GuardFilePathVadility(fileName);
        await FileHelper.WriteAsync(fileName, json, Encoding.UTF8).IgnoreCtx();
    }

    [SetUp]
    public void Setup()
    {
        _roleDefinition = RoleDefinition.Create();
        _roleTypeId = _roleDefinition.Id;
        var iconPath = @"vending-machine.png";
        var capabilities = new List<string> { "CreateDelete", "HasRunningState", "IsVisible" };
        _roleDescriptor = new DescriptorTypeConfigDataBO(_roleTypeId, _roleDefinition.DisplayName, capabilities.AsNotEmptySeq());
        _roleDescriptor.SetIconsPath(iconPath, iconPath);

        _cRoleTypeBO1 = new CustomEntityTypeSSBO(_roleDescriptor)
        {
            CustomFields = _roleDefinition.GetCustomFields().ToList()
        };
    }

    #endregion Public Methods

    #region Private Fields

    private static readonly ILogger _logger = AssemblyInitializer.Logger;
    private static CustomEntityTypeSSBO _cRoleTypeBO1;
    private static RoleDefinition _roleDefinition;
    private static DescriptorTypeConfigDataBO _roleDescriptor;
    private static Guid _roleTypeId;

    #endregion Private Fields

    #region Private Methods

    private static void GuardFilePathVadility(NotEmptyString filePath)
    {
        Guard.Against.Predicate(filePath, f => Path.GetFileName(f) != default && Path.GetDirectoryName(f) != null);
    }

    #endregion Private Methods
}