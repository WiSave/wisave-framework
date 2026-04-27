using System.Reflection;

namespace WiSave.Framework.EventSourcing;

public sealed class AssemblyEventTypeRegistry : IEventTypeRegistry
{
    private readonly Dictionary<string, Type> _map;

    private AssemblyEventTypeRegistry(Dictionary<string, Type> map)
    {
        _map = map;
    }

    public static AssemblyEventTypeRegistry FromAssemblies(
        IEnumerable<Assembly> assemblies,
        Func<Type, bool>? includeType = null)
    {
        var map = assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => includeType?.Invoke(type) ?? true)
            .ToDictionary(type => type.Name, type => type, StringComparer.Ordinal);

        return new AssemblyEventTypeRegistry(map);
    }

    public Type? Resolve(string eventTypeName) => _map.GetValueOrDefault(eventTypeName);
}
