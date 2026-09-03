# syntax=docker/dockerfile:1

ARG FLYCTL_VERSION=v0.4.96
ARG REDIS_VERSION=7.4-alpine

FROM flyio/flyctl:${FLYCTL_VERSION} AS fly
FROM redis:${REDIS_VERSION}

COPY --from=fly /flyctl /usr/local/bin/flyctl

ENTRYPOINT []
CMD ["/bin/sh"]
