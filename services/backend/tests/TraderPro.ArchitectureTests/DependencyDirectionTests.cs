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

    [Fact]
    public void Application_does_not_reference_infrastructure()
    {
        var references = typeof(ApplicationAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(reference => reference is not null)
            .Cast<string>();

        Assert.DoesNotContain("TraderPro.Infrastructure", references);
    }

    [Fact]
    public void Application_has_no_http_entity_framework_or_infrastructure_dependencies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Application",
            "TraderPro.Application.csproj");
        var declared = GetProjectBuildFiles(repositoryRoot, projectPath)
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
            .Where(IsForbiddenApplicationReference)
            .ToArray();
        var runtime = typeof(ApplicationAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(reference => reference is not null)
            .Cast<string>()
            .Where(IsForbiddenApplicationReference)
            .ToArray();

        Assert.Empty(declared);
        Assert.Empty(runtime);
    }

    [Fact]
    public void Persistence_implementation_lives_only_in_infrastructure()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src");
        var persistenceMarkers = new[]
        {
            "DbContext",
            "IEntityTypeConfiguration<",
            "Microsoft.EntityFrameworkCore.Migrations",
        };
        var misplacedFiles = EnumerateSourceFiles(sourceRoot)
            .Where(path => persistenceMarkers.Any(
                marker => File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Where(path => !path.StartsWith(
                Path.Combine(sourceRoot, "TraderPro.Infrastructure"),
                StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(misplacedFiles);
    }

    [Fact]
    public void Api_contains_no_entity_configuration_or_migration_classes()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Api");
        var forbiddenMarkers = new[]
        {
            "IEntityTypeConfiguration<",
            ": Migration",
            "MigrationBuilder",
        };
        var offendingFiles = EnumerateSourceFiles(apiRoot)
            .Where(path => forbiddenMarkers.Any(
                marker => File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Api_endpoints_do_not_mutate_the_database_context()
    {
        var repositoryRoot = FindRepositoryRoot();
        var endpointRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Api",
            "Http");
        var forbiddenPersistenceMarkers = new[]
        {
            "TraderProDbContext",
            "Microsoft.EntityFrameworkCore",
            "SaveChanges",
            "DbSet<",
        };
        var offendingFiles = EnumerateSourceFiles(endpointRoot)
            .Where(path => forbiddenPersistenceMarkers.Any(
                marker => File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Platform_spike_infrastructure_does_not_depend_on_other_modules()
    {
        var repositoryRoot = FindRepositoryRoot();
        var platformRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Infrastructure",
            "Modules",
            "Platform");
        var offendingFiles = EnumerateSourceFiles(platformRoot)
            .Where(path => File.ReadLines(path).Any(
                line =>
                    line.TrimStart().StartsWith(
                        "using TraderPro.Infrastructure.Modules.",
                        StringComparison.Ordinal) &&
                    !line.TrimStart().StartsWith(
                        "using TraderPro.Infrastructure.Modules.Platform",
                        StringComparison.Ordinal) &&
                    !line.TrimStart().StartsWith(
                        "using TraderPro.Infrastructure.Modules.Shared",
                        StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Procurement_poc_infrastructure_does_not_depend_on_inventory_sales_or_finance()
    {
        var repositoryRoot = FindRepositoryRoot();
        var procurementPocRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Infrastructure",
            "Modules",
            "Procurement",
            "Poc");
        var forbidden = new[]
        {
            "TraderPro.Infrastructure.Modules.Inventory",
            "TraderPro.Infrastructure.Modules.Sales",
            "TraderPro.Infrastructure.Modules.Finance",
            "TraderPro.Domain.Inventory",
            "TraderPro.Domain.Sales",
            "TraderPro.Domain.Finance",
            "PurchaseBill",
            "BillingRepository",
        };
        var offendingFiles = EnumerateSourceFiles(procurementPocRoot)
            .Where(path => forbidden.Any(
                marker => File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Procurement_poc_application_does_not_reference_platform_command_probe()
    {
        var repositoryRoot = FindRepositoryRoot();
        var procurementPocRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Application",
            "Procurement",
            "Poc");
        var offendingFiles = EnumerateSourceFiles(procurementPocRoot)
            .Where(path => File.ReadAllText(path).Contains(
                "TraderPro.Application.Platform.CommandProbes",
                StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Authenticated_commercial_context_is_an_application_abstraction()
    {
        var repositoryRoot = FindRepositoryRoot();
        var abstraction = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Application",
            "Platform",
            "Identity",
            "AuthenticatedTraderProContext.cs");
        var implementation = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src",
            "TraderPro.Infrastructure",
            "Modules",
            "Platform",
            "Identity",
            "CurrentAuthenticatedTraderProContext.cs");

        Assert.True(File.Exists(abstraction));
        Assert.Contains(
            "interface IAuthenticatedTraderProContext",
            File.ReadAllText(abstraction),
            StringComparison.Ordinal);
        Assert.True(File.Exists(implementation));
    }

    [Fact]
    public void Commercial_identity_does_not_depend_on_temporary_spike_context()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Platform",
                "Identity"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Platform",
                "Identity"),
        };
        var forbidden = new[]
        {
            "ITemporaryWorkspaceContextResolver",
            "ITemporaryDeviceContextResolver",
            "ICurrentDeviceAccessor",
            "CurrentDeviceAccessor",
            "SpikeRequestContext",
            "X-TraderPro-Workspace-ID",
            "X-TraderPro-Device-ID",
        };
        var offendingFiles = roots
            .SelectMany(EnumerateSourceFiles)
            .Where(path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Identity_secret_hashing_is_implemented_only_in_infrastructure()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(
            repositoryRoot,
            "services",
            "backend",
            "src");
        var offendingFiles = EnumerateSourceFiles(sourceRoot)
            .Where(path => File.ReadAllText(path).Contains(
                "IdentitySecretCryptography",
                StringComparison.Ordinal))
            .Where(path => !path.StartsWith(
                Path.Combine(sourceRoot, "TraderPro.Infrastructure"),
                StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Identity_boundary_does_not_implement_business_modules()
    {
        var repositoryRoot = FindRepositoryRoot();
        var identityRoots = Directory
            .EnumerateDirectories(
                Path.Combine(
                    repositoryRoot,
                    "services",
                    "backend",
                    "src"),
                "Identity",
                SearchOption.AllDirectories);
        var forbidden = new[]
        {
            "TraderPro.Domain.Procurement",
            "TraderPro.Domain.Inventory",
            "TraderPro.Domain.Sales",
            "TraderPro.Domain.Finance",
            "PurchaseBill",
            "BillingRepository",
        };
        var offendingFiles = identityRoots
            .SelectMany(EnumerateSourceFiles)
            .Where(path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offendingFiles);
    }

    [Fact]
    public void Commercial_master_services_use_authenticated_context_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Operations"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Procurement",
                "MasterData"),
        };
        var files = roots.SelectMany(EnumerateSourceFiles).ToArray();
        var forbidden = new[]
        {
            "ITemporaryWorkspaceContextResolver",
            "ITemporaryDeviceContextResolver",
            "ICurrentDeviceAccessor",
            "CurrentDeviceAccessor",
            "SpikeRequestContext",
            "X-TraderPro-Workspace-ID",
            "X-TraderPro-Device-ID",
        };

        Assert.Contains(
            files,
            path => File.ReadAllText(path).Contains(
                "IAuthenticatedTraderProContext",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            files,
            path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)));
    }

    [Fact]
    public void Commercial_master_scope_does_not_introduce_deferred_modules()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Domain",
                "Operations"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Domain",
                "Procurement",
                "MasterData"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Operations"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Procurement",
                "MasterData"),
        };
        var forbidden = new[]
        {
            "Supplier",
            "ProductGroup",
            "SupplierProduct",
            "InventoryMovement",
            "PurchaseBill",
            "Settlement",
            "SalesOrder",
            "JournalEntry",
            "BillingRepository",
        };
        var offending = roots
            .SelectMany(EnumerateSourceFiles)
            .Where(path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void Commercial_master_events_remain_outside_mobile_sync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Operations"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Procurement",
                "MasterData"),
        };
        var offending = roots
            .SelectMany(EnumerateSourceFiles)
            .Where(path => File.ReadAllText(path).Contains(
                "OutboxEventStream.MobileSync",
                StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void Supplier_and_catalog_boundaries_use_authenticated_context_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Catalog"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Procurement",
                "Suppliers"),
        };
        var files = roots.SelectMany(EnumerateSourceFiles).ToArray();
        var forbidden = new[]
        {
            "ITemporaryWorkspaceContextResolver",
            "ITemporaryDeviceContextResolver",
            "ICurrentDeviceAccessor",
            "SpikeRequestContext",
            "X-TraderPro-Workspace-ID",
            "X-TraderPro-Device-ID",
        };

        Assert.All(
            files,
            path => Assert.DoesNotContain(
                forbidden,
                marker => File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)));
        Assert.Contains(
            files,
            path => File.ReadAllText(path).Contains(
                "IAuthenticatedTraderProContext",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Supplier_and_catalog_domain_and_application_keep_persistence_out()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Domain",
                "Catalog"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Domain",
                "Procurement",
                "Suppliers"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Catalog"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Procurement",
                "Suppliers"),
        };
        var forbidden = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "TraderProDbContext",
            "Microsoft.AspNetCore",
            "TraderPro.Infrastructure",
        };
        var offending = roots
            .SelectMany(EnumerateSourceFiles)
            .Where(path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void Supplier_and_catalog_scope_does_not_add_deferred_business_workflows()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Domain",
                "Catalog"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Domain",
                "Procurement",
                "Suppliers"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Catalog"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Application",
                "Procurement",
                "Suppliers"),
        };
        var forbidden = new[]
        {
            "ReceivingSession",
            "InventoryMovement",
            "PurchaseBill",
            "SupplierBalance",
            "Settlement",
            "SalesOrder",
            "JournalEntry",
            "MillingRun",
            "BillingRepository",
        };
        var offending = roots
            .SelectMany(EnumerateSourceFiles)
            .Where(path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void Supplier_and_catalog_events_use_only_internal_outbox()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Catalog"),
            Path.Combine(
                repositoryRoot,
                "services",
                "backend",
                "src",
                "TraderPro.Infrastructure",
                "Modules",
                "Procurement",
                "Suppliers"),
        };
        var files = roots.SelectMany(EnumerateSourceFiles).ToArray();

        Assert.DoesNotContain(
            files,
            path => File.ReadAllText(path).Contains(
                "OutboxEventStream.MobileSync",
                StringComparison.Ordinal));
        Assert.Contains(
            files,
            path => File.ReadAllText(path).Contains(
                "OutboxEventStream.Internal",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Commercial_receiving_domain_and_application_keep_production_boundaries()
    {
        var repositoryRoot = FindRepositoryRoot();
        var roots = new[]
        {
            Path.Combine(repositoryRoot, "services", "backend", "src", "TraderPro.Domain", "Procurement", "Receiving"),
            Path.Combine(repositoryRoot, "services", "backend", "src", "TraderPro.Application", "Procurement", "Receiving"),
        };
        var forbidden = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Npgsql",
            "TraderProDbContext",
            "TraderPro.Infrastructure",
            "Procurement.Poc",
            "ITemporaryWorkspaceContextResolver",
            "ICurrentDeviceAccessor",
        };

        var offending = roots.SelectMany(EnumerateSourceFiles)
            .Where(path => forbidden.Any(marker => File.ReadAllText(path).Contains(marker, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void Commercial_receiving_uses_authenticated_context_and_adds_no_posting_modules()
    {
        var repositoryRoot = FindRepositoryRoot();
        var root = Path.Combine(repositoryRoot, "services", "backend", "src", "TraderPro.Infrastructure", "Modules", "Procurement", "Receiving");
        var files = EnumerateSourceFiles(root).ToArray();
        var forbidden = new[]
        {
            "PurchaseBill",
            "InventoryMovement",
            "SupplierPayable",
            "SalesOrder",
            "FinancePosting",
            "ProductionRun",
            "ReceivingSessionPoc",
            "MobileSyncOperationPoc",
        };

        Assert.Contains(files, path => File.ReadAllText(path).Contains("IAuthenticatedTraderProContext", StringComparison.Ordinal));
        Assert.DoesNotContain(
            files,
            path => forbidden.Any(marker =>
                File.ReadAllText(path).Contains(marker, StringComparison.Ordinal)));
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

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj"));
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

    private static bool IsForbiddenApplicationReference(string reference)
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
               reference.Equals(
                   "TraderPro.Infrastructure",
                   StringComparison.OrdinalIgnoreCase);
    }
}
