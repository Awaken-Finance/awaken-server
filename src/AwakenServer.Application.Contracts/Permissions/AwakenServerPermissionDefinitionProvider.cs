using Volo.Abp.Authorization.Permissions;

namespace AwakenServer.Permissions
{
    public class AwakenServerPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var myGroup = context.AddGroup(AwakenServerPermissions.GroupName);
            //Define your own permissions here. Example:
            //myGroup.AddPermission(AwakenServerPermissions.MyPermission1, L("Permission:MyPermission1"));
        }

        /*private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AwakenServerResource>(name);
        }*/
    }
}
