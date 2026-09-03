# Fly Redis operations

This image provides Fly CLI and `redis-cli` in the same Linux container. Fly CLI can create a
private tunnel to a Fly-managed Upstash Redis database and then start `redis-cli` through that
tunnel.

## Build the image

Run from the repository root:

```powershell
docker build --tag picklecrew-fly-redis tools/fly-redis
```

## Start an interactive container

Start the container without passing credentials:

```powershell
docker run --rm --interactive --tty --name picklecrew-fly-redis picklecrew-fly-redis
```

The container opens an Alpine shell containing `flyctl` and `redis-cli`.

## Clear an environment's cache

```sh
flyctl redis connect
FLUSHDB
flyctl apps restart <app-name>
``
