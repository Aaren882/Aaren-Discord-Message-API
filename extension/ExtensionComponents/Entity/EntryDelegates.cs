using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static ExtensionComponents.LocalServices;
using static ExtensionComponents.ExtensionStartup;

namespace ExtensionComponents.Entity;

public abstract class EntryDelegatesBase
{
    public required Dictionary<string, InitAction> ActionsDict;

    public static Dictionary<string, InitAction> GetActionsMap(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type actionType
    )
    {
        var methods = actionType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
        
#if DEBUG
        foreach (var method in methods)
            Tracer(nameof(GetActionsMap), method.Name);
#endif
        
        return methods.ToDictionary(
            prop => prop.Name, 
            prop => (InitAction)Delegate.CreateDelegate(
                typeof(InitAction),
                null,
                prop
            )!
        );
    }
}
