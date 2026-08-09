using Microsoft.AspNetCore.Http;

namespace Api.Common;

public static class CookieHelpers
{
    public const string AuthCookieName = "auth_token";

    public static void SetAuthCookie(this HttpContext context, string token, DateTime expiresAt)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,

            Secure = context.Request.IsHttps,

            Expires = expiresAt
        };
        
        context.Response.Cookies.Append(AuthCookieName, token, options);
    }

    public static void ClearAuthCookie(this HttpContext context)
    {
        // Borrar una cookie es simplemente decirle al navegador que expiro en el pasado
        context.Response.Cookies.Delete(AuthCookieName);
    }
}
