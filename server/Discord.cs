namespace advisor;

using Microsoft.AspNetCore.Authentication.OAuth;

public static class DiscordDefaults {
    public const string AuthenticationScheme = "discord";
}

public static class ExternalAuthentication {
    public const string AuthenticationScheme = "external";
}

public class DiscordOptions {
    public string Address { get; set; }
    public int ApiVersion { get; set; }

    public OAuthOptions OAuth { get; set; }
}
