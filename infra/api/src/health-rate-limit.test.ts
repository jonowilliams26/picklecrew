import { afterEach, describe, expect, it, vi } from "vitest";

import { rateLimitHealthRequest } from "./health-rate-limit";

const deploymentEnvironment = "development";

function createRateLimiter(success = true) {
  const limit = vi.fn<RateLimit["limit"]>();
  limit.mockResolvedValue({ success });

  return { limit, rateLimiter: { limit } };
}

function createRequest(path: string, init?: RequestInit): Request {
  return new Request(`https://api.dev.picklecrew.app${path}`, init);
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe("rateLimitHealthRequest", () => {
  it.each([
    ["the canonical path", "/health"],
    ["a case variant", "/HEALTH"],
    ["a query string", "/health?probe=1"],
    ["a trailing slash", "/health/"],
    ["an encoded path", "/%68ealth"],
  ])("invokes the limiter for %s", async (_description, path) => {
    const { limit, rateLimiter } = createRateLimiter();

    await rateLimitHealthRequest({
      request: createRequest(path),
      rateLimiter,
      deploymentEnvironment,
    });

    expect(limit).toHaveBeenCalledOnce();
  });

  it.each(["HEAD", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"])(
    "invokes the limiter for %s requests",
    async (method) => {
      const { limit, rateLimiter } = createRateLimiter();

      await rateLimitHealthRequest({
        request: createRequest("/health", { method }),
        rateLimiter,
        deploymentEnvironment,
      });

      expect(limit).toHaveBeenCalledOnce();
    },
  );

  it.each([
    ["the liveness endpoint", "/alive"],
    ["another API endpoint", "/api/games"],
    ["a malformed encoded path", "/%E0%A4%A"],
  ])("bypasses the limiter for %s", async (_description, path) => {
    const { limit, rateLimiter } = createRateLimiter();

    const response = await rateLimitHealthRequest({
      request: createRequest(path),
      rateLimiter,
      deploymentEnvironment,
    });

    expect(response).toBeUndefined();
    expect(limit).not.toHaveBeenCalled();
  });

  it("uses CF-Connecting-IP as the limiter key", async () => {
    const { limit, rateLimiter } = createRateLimiter();

    await rateLimitHealthRequest({
      request: createRequest("/health", {
        headers: { "CF-Connecting-IP": "203.0.113.10" },
      }),
      rateLimiter,
      deploymentEnvironment,
    });

    expect(limit).toHaveBeenCalledWith({ key: "203.0.113.10" });
  });

  it("uses unknown as the limiter key when CF-Connecting-IP is missing", async () => {
    const { limit, rateLimiter } = createRateLimiter();

    await rateLimitHealthRequest({
      request: createRequest("/health"),
      rateLimiter,
      deploymentEnvironment,
    });

    expect(limit).toHaveBeenCalledWith({ key: "unknown" });
  });

  it("returns no response when the request is allowed", async () => {
    const { rateLimiter } = createRateLimiter(true);

    const response = await rateLimitHealthRequest({
      request: createRequest("/health"),
      rateLimiter,
      deploymentEnvironment,
    });

    expect(response).toBeUndefined();
  });

  it("returns a generic rate-limit response when the request is rejected", async () => {
    const { rateLimiter } = createRateLimiter(false);

    const response = await rateLimitHealthRequest({
      request: createRequest("/health"),
      rateLimiter,
      deploymentEnvironment,
    });

    expect(response?.status).toBe(429);
    expect(response?.headers.get("Retry-After")).toBe("60");
    await expect(response?.text()).resolves.toBe("Too many requests");
  });

  it("fails open without logging request data when the binding throws", async () => {
    const limit = vi.fn<RateLimit["limit"]>();
    limit.mockRejectedValue(
      new Error("203.0.113.10 https://api.dev.picklecrew.app/health"),
    );
    const errorLog = vi.spyOn(console, "error").mockImplementation(() => undefined);

    const response = await rateLimitHealthRequest({
      request: createRequest("/health", {
        headers: { "CF-Connecting-IP": "203.0.113.10" },
      }),
      rateLimiter: { limit },
      deploymentEnvironment,
    });

    expect(response).toBeUndefined();
    expect(errorLog).toHaveBeenCalledWith(
      JSON.stringify({
        event: "health_rate_limit_check_failed",
        deploymentEnvironment,
      }),
    );
    expect(errorLog.mock.calls.flat().join(" ")).not.toContain("203.0.113.10");
    expect(errorLog.mock.calls.flat().join(" ")).not.toContain("/health");
  });
});
