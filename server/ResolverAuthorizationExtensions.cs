namespace advisor;

using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;

public static class ResolverAuthorizationExtensions {
    public static async Task<bool> AuthorizeAsync(this IResolverContext resolver, string policyName) {
        var auth = resolver.Service<IAuthorizationService>();
        var result = await auth.AuthorizeAsync(resolver.GetUser(), resolver, policyName);

        if (result.Succeeded) {
            return true;
        }

        var failure = result.Failure;

        if (failure.FailureReasons.Any()) {
            foreach (var reson in failure.FailureReasons) {
                resolver.ReportError(ErrorBuilder.New()
                    .SetMessage(reson.Message)
                    .SetCode(ErrorCodes.Authentication.NotAuthorized)
                    .SetPath(resolver.Path)
                    .AddLocation(resolver.Selection.SyntaxNode)
                    .Build()
                );
            }
        }
        else {
            resolver.ReportError(ErrorBuilder.New()
                .SetMessage("The current user is not authorized to access this resource.")
                .SetCode(ErrorCodes.Authentication.NotAuthorized)
                .SetPath(resolver.Path)
                .AddLocation(resolver.Selection.SyntaxNode)
                .Build()
            );
        }

        return false;
    }
}
