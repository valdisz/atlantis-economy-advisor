namespace advisor;

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

public class DefaultFilesMiddleware {
    public DefaultFilesMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv) {
        this.next = next;
        this.hostingEnv = hostingEnv;
    }

    private readonly RequestDelegate next;
    private readonly IWebHostEnvironment hostingEnv;

    private readonly Regex filePattern = new(@"\.\w+$");
    private readonly PathString[] apiSegments = [
        "/graphql",
        "/api",
        "/account",
        "/hangfire",
        "/system",
    ];

    public Task Invoke(HttpContext context) {
        var method = context.Request.Method.ToLowerInvariant();
        if (method != "get" && method != "head") return next(context);

        var path = context.Request.Path;
        if (apiSegments.Any(x => path.StartsWithSegments(x))) {
            return next(context);
        }

        if (!path.HasValue || !IsFile(path)) {
            context.Request.Path = new PathString("/index.html");
            return next(context);
        }

        return next(context);
    }

    public bool IsFile(string path) {
        var lastSlash = path.LastIndexOf("/");
        var match = filePattern.IsMatch(lastSlash >= 0 ? path[lastSlash..] : path);

        return match;
    }
}
