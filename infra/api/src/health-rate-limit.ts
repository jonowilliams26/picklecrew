const healthPath = "/health";
const healthPathWithTrailingSlash = "/health/";
const retryAfterSeconds = 60;

export async function rateLimitHealthRequest({
  request,
  rateLimiter,
  deploymentEnvironment,
}: {
  request: Request;
  rateLimiter: Pick<RateLimit, "limit">;
  deploymentEnvironment: Env["DEPLOYMENT_ENVIRONMENT"];
}): Promise<Response | undefined> {
  const pathname = normalizePathname(new URL(request.url).pathname);
  if (pathname !== healthPath && pathname !== healthPathWithTrailingSlash) {
    return undefined;
  }

  const clientIdentifier = request.headers.get("CF-Connecting-IP") ?? "unknown";

  try {
    const { success } = await rateLimiter.limit({ key: clientIdentifier });
    if (success) {
      return undefined;
    }

    return new Response("Too many requests", {
      status: 429,
      headers: { "Retry-After": retryAfterSeconds.toString() },
    });
  } catch {
    console.error(
      JSON.stringify({
        event: "health_rate_limit_check_failed",
        deploymentEnvironment,
      }),
    );

    return undefined;
  }
}

function normalizePathname(pathname: string): string {
  try {
    return decodeURIComponent(pathname).toLowerCase();
  } catch {
    return pathname.toLowerCase();
  }
}
