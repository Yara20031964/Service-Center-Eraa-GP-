using System.Collections.Concurrent;
using System.Reflection;

namespace KHDMA.API.Swagger;

/// <summary>
/// Builds the schema name Swagger uses for a type.
/// </summary>
/// <remarks>
/// Swashbuckle keys schemas by the short type name, which throws once two types
/// in different namespaces share one - this codebase has three such pairs
/// (BookingDetailDto, BookingListDto and ReviewDto each exist under DTOs.Admin
/// and under their feature folder). Declaring response types made both halves of
/// every pair reachable, so the whole document failed to generate.
///
/// Qualifying every name would make the document unreadable, so only genuinely
/// ambiguous names get their namespace leaf prefixed: BookingListDto stays
/// BookingListDto, and its admin twin becomes AdminBookingListDto.
/// </remarks>
public static class SchemaIds
{
    // Simple names that appear on more than one type, so only those pay for
    // disambiguation. Built once from the assemblies that actually hold DTOs.
    private static readonly Lazy<HashSet<string>> Ambiguous = new(() =>
    {
        var assemblies = new[]
        {
            // global:: because inside KHDMA.API.Swagger a bare "Domain" binds to
            // KHDMA.Domain, not the root-level Domain namespace ApiResponse lives in.
            typeof(KHDMA.Application.DTOs.Booking.BookingListDto).Assembly,
            typeof(global::Domain.Common.ApiResponse<object>).Assembly,
        }.Distinct();

        return assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => t.IsPublic || t.IsNestedPublic)
            .Select(t => t.Name)
            .GroupBy(name => name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);
    });

    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    public static string For(Type type) => Cache.GetOrAdd(type, Build);

    private static string Build(Type type)
    {
        if (type.IsGenericType)
        {
            // ApiResponse`1[BookingDetailDto] -> ApiResponseOfBookingDetailDto
            var tick = type.Name.IndexOf('`');
            var baseName = tick < 0 ? type.Name : type.Name[..tick];
            var args = string.Join("And", type.GetGenericArguments().Select(For));
            return $"{baseName}Of{args}";
        }

        if (type.IsArray)
            return $"{For(type.GetElementType()!)}Array";

        if (!Ambiguous.Value.Contains(type.Name))
            return type.Name;

        // Prefixing unconditionally stutters: DTOs.Booking.BookingDetailDto would
        // become BookingBookingDetailDto. When the folder already names the type,
        // the plain name is the natural one - so only the odd one out (the Admin
        // twin) gets qualified, giving BookingDetailDto and AdminBookingDetailDto.
        var leaf = NamespaceLeaf(type);
        return type.Name.StartsWith(leaf, StringComparison.Ordinal)
            ? type.Name
            : leaf + type.Name;
    }

    private static string NamespaceLeaf(Type type)
    {
        var ns = type.Namespace;
        if (string.IsNullOrEmpty(ns)) return string.Empty;

        var lastDot = ns.LastIndexOf('.');
        return lastDot < 0 ? ns : ns[(lastDot + 1)..];
    }

    // A type that fails to load must not take the whole document down with it.
    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
