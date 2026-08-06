import { Container } from "@cloudflare/containers";

import { rateLimitHealthRequest } from "./health-rate-limit";

const apiPort = 8080;

export class ApiContainer extends Container<Env> {
  override defaultPort = apiPort;
  override requiredPorts = [apiPort];
  override pingEndpoint = "localhost/alive";
  override sleepAfter =
    this.env.DEPLOYMENT_ENVIRONMENT === "development" ? "1m" : "10m";
  override envVars = {
    ASPNETCORE_ENVIRONMENT: "Production",
    ASPNETCORE_URLS: `http://0.0.0.0:${apiPort}`,
    DEPLOYMENT_ENVIRONMENT: this.env.DEPLOYMENT_ENVIRONMENT,
    ConnectionStrings__picklecrew: this.env.DATABASE_CONNECTION_STRING,
  };
}

export default {
  async fetch(request, env): Promise<Response> {
    const rateLimitResponse = await rateLimitHealthRequest({
      request,
      rateLimiter: env.HEALTH_RATE_LIMITER,
      deploymentEnvironment: env.DEPLOYMENT_ENVIRONMENT,
    });
    if (rateLimitResponse) {
      return rateLimitResponse;
    }

    const container = env.API_CONTAINER.getByName(
      `picklecrew-api-${env.DEPLOYMENT_ENVIRONMENT}-oceania-v1`,
      { locationHint: "oc" },
    );

    try {
      return await container.fetch(request);
    } catch (error: unknown) {
      console.error(
        JSON.stringify({
          event: "api_container_request_failed",
          deploymentEnvironment: env.DEPLOYMENT_ENVIRONMENT,
          error: error instanceof Error ? error.message : String(error),
        }),
      );

      return new Response("Service unavailable", {
        status: 503,
        headers: { "Retry-After": "5" },
      });
    }
  },
} satisfies ExportedHandler<Env>;
