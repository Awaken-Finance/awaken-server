using Volo.Abp.Settings;

namespace AwakenServer.Settings
{
    public class AwakenServerSettingDefinitionProvider : SettingDefinitionProvider
    {
        public override void Define(ISettingDefinitionContext context)
        {
            //Define your own settings here. Example:
            //context.Add(new SettingDefinition(AwakenServerSettings.MySetting1));
        }
    }
}
