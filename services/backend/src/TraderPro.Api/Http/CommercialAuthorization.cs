using Microsoft.AspNetCore.Authorization;
using TraderPro.Application.Platform.Identity;

namespace TraderPro.Api.Http;

public enum TraderProCommercialAuthorizationKind
{
    Anonymous,
    CommercialUser,
    Owner,
    Operator,
    OperatorOrOwner,
}

public sealed record TraderProCommercialContextOptions(
    bool RequiresAuthenticatedContext,
    bool AllowRevokedTokenFamily,
    bool SecretBearingResponse,
    bool RequiresSecureTransport,
    TraderProCommercialAuthorizationKind AuthorizationKind);

internal sealed record CommercialRoleRequirement(
    TraderProCommercialAuthorizationKind RequiredKind) : IAuthorizationRequirement;

internal sealed class CommercialRoleAuthorizationHandler(
    IAuthenticatedTraderProContext currentIdentity) :
    AuthorizationHandler<CommercialRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CommercialRoleRequirement requirement)
    {
        if (currentIdentity.IsBound &&
            requirement.RequiredKind switch
            {
                TraderProCommercialAuthorizationKind.Owner => currentIdentity.IsOwner,
                TraderProCommercialAuthorizationKind.Operator =>
                    currentIdentity.IsOperator,
                _ => currentIdentity.IsOperatorOrOwner,
            })
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal static class CommercialAuthorizationRegistration
{
    public static IServiceCollection AddTraderProCommercialAuthorization(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler,
            CommercialRoleAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(
                TraderProAuthorizationPolicies.CommercialUser,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(
                        new CommercialRoleRequirement(TraderProCommercialAuthorizationKind.CommercialUser));
                })
            .AddPolicy(
                TraderProAuthorizationPolicies.Owner,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(
                        new CommercialRoleRequirement(TraderProCommercialAuthorizationKind.Owner));
                })
            .AddPolicy(
                TraderProAuthorizationPolicies.Operator,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(
                        new CommercialRoleRequirement(TraderProCommercialAuthorizationKind.Operator));
                })
            .AddPolicy(
                TraderProAuthorizationPolicies.OperatorOrOwner,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(
                        new CommercialRoleRequirement(TraderProCommercialAuthorizationKind.OperatorOrOwner));
                });
        return services;
    }
}

public static class CommercialEndpointConventionExtensions
{
    public static RouteHandlerBuilder WithTraderProAnonymousCommercialContext(
        this RouteHandlerBuilder builder,
        bool secretBearingResponse)
    {
        return builder.WithMetadata(
            new TraderProCommercialContextOptions(
                RequiresAuthenticatedContext: false,
                AllowRevokedTokenFamily: false,
                SecretBearingResponse: secretBearingResponse,
                RequiresSecureTransport: true,
                TraderProCommercialAuthorizationKind.Anonymous));
    }

    public static RouteHandlerBuilder RequireTraderProCommercialAuthorization(
        this RouteHandlerBuilder builder,
        string policy,
        bool allowRevokedTokenFamily = false,
        bool secretBearingResponse = false)
    {
        var kind = policy switch
        {
            TraderProAuthorizationPolicies.CommercialUser =>
                TraderProCommercialAuthorizationKind.CommercialUser,
            TraderProAuthorizationPolicies.Owner =>
                TraderProCommercialAuthorizationKind.Owner,
            TraderProAuthorizationPolicies.Operator =>
                TraderProCommercialAuthorizationKind.Operator,
            TraderProAuthorizationPolicies.OperatorOrOwner =>
                TraderProCommercialAuthorizationKind.OperatorOrOwner,
            _ => throw new ArgumentException(
                "A known TraderPro commercial policy is required.",
                nameof(policy)),
        };
        builder.RequireAuthorization(policy);
        return builder.WithMetadata(
            new TraderProCommercialContextOptions(
                RequiresAuthenticatedContext: true,
                AllowRevokedTokenFamily: allowRevokedTokenFamily,
                SecretBearingResponse: secretBearingResponse,
                RequiresSecureTransport: true,
                kind));
    }
}
