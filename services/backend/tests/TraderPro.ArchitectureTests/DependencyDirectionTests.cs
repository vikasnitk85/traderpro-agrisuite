using System.Reflection;
using System.Xml.Linq;
using TraderPro.Api;
using TraderPro.Application;
using TraderPro.Domain;
using TraderPro.Infrastructure;
using TraderPro.Worker;

namespace TraderPro.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["TraderPro.Domain"] = [],
            ["TraderPro.Application"] = ["TraderPro.Domain"],
            ["TraderPro.Infrastructure"] =
                ["TraderPro.Application", "TraderPro.Domain"],
            ["TraderPro.Api"] =
                ["TraderPro.Application", "TraderPro.Infrastructure"],
            ["TraderPro.Worker"] =
                ["TraderPro.Application", "TraderPro.Infrastructure"],
        };

    private static readonly IReadOnlyDictionary<Assembly, string[]> AllowedTraderProReferences =
        new Dictionary<Assembly, string[]>
        {
            [typeof(DomainAssemblyMarker).Assembly] = [],
            [typeof(ApplicationAssemblyMarker).Assembly] =
                ["TraderPro.Domain"],
            [typeof(InfrastructureAssemblyMarker).Assembly] =
                ["TraderPro.Application", "TraderPro.Domain"],
            [typeof(ApiAssemblyMarker).Assembly] =
                ["TraderPro.Application", "TraderPro.Infrastructure"],
            [typeof(WorkerAssemblyMarker).Assembly] =
                ["TraderPro.Application", "TraderPro.Infrastructure"],
        };

    [Fact]
    public void Source_projects_reference_only_allowed_TraderPro_layers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src");
        var expectedProjects = AllowedProjectReferences.Keys
            .OrderBy(project => project, StringComparer.Ordinal)
            .ToArray();
        var discoveredProjects = Directory
            .EnumerateFiles(
                sourceRoot,
                "*.csproj",
                SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(project => project is not null)
            .Cast<string>()
            .OrderBy(project => project, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedProjects, discoveredProjects);

        foreach (var (projectName, allowedReferences) in AllowedProjectReferences)
        {
            var projectPath = Path.Combine(
                sourceRoot,
                projectName,
                $"{projectName}.csproj");
            var actualReferences = GetProjectBuildFiles(
                    repositoryRoot,
                    projectPath)
                .SelectMany(path => XDocument.Load(path).Descendants())
                .Where(element => element.Name.LocalName == "ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(reference => reference is not null)
                .Cast<string>()
                .Select(reference => reference.Replace('\\', '/'))
                .Select(reference => Path.GetFileNameWithoutExtension(reference))
                .Where(reference => reference is not null)
                .Cast<string>()
                .ToArray();
            var disallowedReferences = actualReferences
                .Except(allowedReferences, StringComparer.Ordinal)
                .Select(reference => $"{projectName} -> {reference}")
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(disallowedReferences);
        }
    }

    [Fact]
    public void Domain_project_declares_no_forbidden_dependencies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var domainProjectPath = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Domain",
            "TraderPro.Domain.csproj");
        var forbiddenReferences = GetProjectBuildFiles(
                repositoryRoot,
                domainProjectPath)
            .SelectMany(path => XDocument.Load(path).Descendants())
            .Where(element =>
                element.Name.LocalName is
                    "FrameworkReference" or
                    "PackageReference" or
                    "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(reference => reference is not null)
            .Cast<string>()
            .Select(GetDependencyName)
            .Where(IsForbiddenDomainReference)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void Source_assemblies_reference_only_allowed_TraderPro_layers()
    {
        foreach (var (assembly, allowedReferences) in AllowedTraderProReferences)
        {
            var actualReferences = GetTraderProReferences(assembly);
            var disallowedReferences = actualReferences
                .Except(allowedReferences, StringComparer.Ordinal)
                .Select(reference => $"{assembly.GetName().Name} -> {reference}")
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray();

            Assert.Empty(disallowedReferences);
        }
    }

    [Fact]
    public void Domain_has_no_forbidden_framework_provider_or_host_references()
    {
        var forbiddenReferences = typeof(DomainAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(reference => reference is not null)
            .Cast<string>()
            .Where(IsForbiddenDomainReference)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TraderPro.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root containing TraderPro.sln.");
    }

    private static IEnumerable<string> GetProjectBuildFiles(
        string repositoryRoot,
        string projectPath)
    {
        yield return projectPath;

        DirectoryInfo? directory = new(
            Path.GetDirectoryName(projectPath) ??
            throw new DirectoryNotFoundException(
                $"Could not locate the project directory for {projectPath}."));

        while (directory is not null)
        {
            foreach (var fileName in new[]
                     {
                         "Directory.Build.props",
                         "Directory.Build.targets",
                     })
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                {
                    yield return candidate;
                }
            }

            if (directory.FullName.Equals(
                    repositoryRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            directory = directory.Parent;
        }
    }

    private static string GetDependencyName(string reference)
    {
        var normalizedReference = reference.Replace('\\', '/');

        return normalizedReference.EndsWith(
            ".csproj",
            StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(normalizedReference) ??
              normalizedReference
            : normalizedReference;
    }

    private static string[] GetTraderProReferences(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(reference =>
                reference is not null &&
                reference.StartsWith(
                    "TraderPro.",
                    StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsForbiddenDomainReference(string reference)
    {
        return reference.Contains(
                   "AspNetCore",
                   StringComparison.OrdinalIgnoreCase) ||
               reference.Contains(
                   "EntityFrameworkCore",
                   StringComparison.OrdinalIgnoreCase) ||
               reference.StartsWith(
                   "Npgsql",
                   StringComparison.OrdinalIgnoreCase) ||
               reference.Contains("Postgre", StringComparison.OrdinalIgnoreCase) ||
               reference.Contains("SignalR", StringComparison.OrdinalIgnoreCase) ||
               reference.Equals(
                   "TraderPro.Api",
                   StringComparison.OrdinalIgnoreCase) ||
               reference.Equals(
                   "TraderPro.Infrastructure",
                   StringComparison.OrdinalIgnoreCase);
    }
}
