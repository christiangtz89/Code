namespace pcms.Application.Auth;

public static class PermissionImplications
{
    public static HashSet<string> EffectiveCodes(IEnumerable<string> permissionCodes)
    {
        var effectiveCodes = permissionCodes.ToHashSet(StringComparer.Ordinal);
        foreach (var permissionCode in effectiveCodes.ToArray())
        {
            if (permissionCode.EndsWith(".Manage", StringComparison.Ordinal))
                effectiveCodes.Add(permissionCode[..^7] + ".View");
        }

        if (effectiveCodes.Contains(PermissionCodes.InventoryManage))
            effectiveCodes.Add(PermissionCodes.InventoryScanOutgoing);

        return effectiveCodes;
    }

    public static bool Satisfies(IEnumerable<string> grantedCodes, string requiredCode) =>
        EffectiveCodes(grantedCodes).Contains(requiredCode);
}
