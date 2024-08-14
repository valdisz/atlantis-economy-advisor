namespace advisor.Authorization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using advisor.Persistence;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Authorization;

using KeyValue = System.Collections.Generic.KeyValuePair<HotChocolate.NameString, HotChocolate.Types.Relay.IdValue>;

public class OwnPlayerRequirement : IAuthorizationRequirement {

}

public class OwnPlayerAuthorizationHandler : AuthorizationHandler<OwnPlayerRequirement, IResolverContext> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnPlayerRequirement requirement, IResolverContext resource) {
        var ids = GetIdArguments(resource);
        if (ids.Count == 0) {
            return Task.CompletedTask;
        }

        var allowedIds = context.User.FindAll(WellKnownClaimTypes.PLAYER)
            .Select(x => long.Parse(x.Value))
            .ToList();

        if (allowedIds.Count == 0) {
            context.Fail(new AuthorizationFailureReason(this, "User does not own any player."));
            return Task.CompletedTask;
        }

        if (EnsurePlayerAccess(context, requirement, allowedIds, ids)
            || EnsurePlayerTurnAccess(context, requirement, allowedIds, ids)
            || EnsureUnitAccess(context, requirement, allowedIds, ids)
            || EnsureRegionAccess(context, requirement, allowedIds, ids)) {
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static bool EnsurePlayerAccess(AuthorizationHandlerContext context, OwnPlayerRequirement requirement, List<long> allowedIds, List<KeyValue> ids) {
        var id = GetId<long>("Player", "playerId", ids);
        if (id != default) {
            EnsureOwnPlayerId(context, requirement, allowedIds, id);
            return true;
        }

        return false;
    }

    private static bool EnsurePlayerTurnAccess(AuthorizationHandlerContext context, OwnPlayerRequirement requirement, List<long> allowedIds, List<KeyValue> ids) {
        var id = GetId<string>("PlayerTurn", "playerTurnId", ids);
        if (id != default) {
            var (playerId, _) = DbPlayerTurn.ParseId(id);
            EnsureOwnPlayerId(context, requirement, allowedIds, playerId);
            return true;
        }

        return false;
    }

    private static bool EnsureUnitAccess(AuthorizationHandlerContext context, OwnPlayerRequirement requirement, List<long> allowedIds, List<KeyValue> ids) {
        var id = GetId<string>("Unit", "unitId", ids);
        if (id != default) {
            var (playerId, _, _) = DbUnit.ParseId(id);
            EnsureOwnPlayerId(context, requirement, allowedIds, playerId);
            return true;
        }

        return false;
    }

    private static bool EnsureRegionAccess(AuthorizationHandlerContext context, OwnPlayerRequirement requirement, List<long> allowedIds, List<KeyValue> ids) {
        var id = GetId<string>("Region", "regionId", ids);
        if (id != default) {
            var regionId = RegionId.CreateFrom(id);
            EnsureOwnPlayerId(context, requirement, allowedIds, regionId.PlayerId);
            return true;
        }

        return false;
    }

    private static void EnsureOwnPlayerId(AuthorizationHandlerContext context, OwnPlayerRequirement requirement, List<long> allowedIds, long playerId) {
        if (allowedIds.Contains(playerId)) {
            context.Succeed(requirement);
        }
        else {
            context.Fail();
        }
    }

    private static T GetId<T>(string typeName, string cannonicalName, List<KeyValue> ids) {
        var matchingIds = ids.FindAll(x => x.Value.TypeName == typeName);
        if (matchingIds.Count == 0) {
            return default;
        }

        if (matchingIds.Count == 1) {
            return (T) matchingIds[0].Value.Value;
        }

        var i = matchingIds.FindIndex(x => x.Key == cannonicalName);
        if (i < 0) {
            return default;
        }

        return (T) ids[i].Value.Value;
    }

    private static List<KeyValue> GetIdArguments(IResolverContext context) {
        var args = context.Selection.Field.Arguments;
        var idSerializer = context.Service<IIdSerializer>();

        List<KeyValue> values = new ();

        void addArgument(IInputField arg) {
            var idValue = context.ArgumentLiteral<StringValueNode>(arg.Name);
            values.Add(new KeyValue(arg.Name, idSerializer.Deserialize(idValue.Value)));
        }

        foreach (var arg in args) {
            switch (arg.Type) {
                case NonNullType nonNull: {
                    if (nonNull.Type is IdType) {
                        addArgument(arg);
                    }
                    break;
                }

                case IdType:
                    addArgument(arg);
                    break;
            }
        }

        return values;
    }
}
