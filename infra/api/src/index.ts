import { Container } from "@cloudflare/containers";

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
    const container = env.API_CONTAINER.getByName(
      `picklecrew-api-${env.DEPLOYMENT_ENVIRONMENT}`,
    );

    try {
      await container.startAndWaitForPorts({ ports: [apiPort] });
    } catch (error: unknown) {
      console.error(
        JSON.stringify({
          event: "api_container_start_failed",
          deploymentEnvironment: env.DEPLOYMENT_ENVIRONMENT,
          error: error instanceof Error ? error.message : String(error),
        }),
      );

      return new Response("Service unavailable", {
        status: 503,
        headers: { "Retry-After": "5" },
      });
    }

    return container.fetch(request);
  },
} satisfies ExportedHandler<Env>;
