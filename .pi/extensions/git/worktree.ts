import { randomBytes } from "node:crypto";
import type { ExtensionAPI, ExtensionCommandContext } from "@earendil-works/pi-coding-agent";

const STATUS_KEY = "git-worktree";
const MAX_BRANCH_NAME_LENGTH = 64;
const MAX_AGENT_NAME_LENGTH = 32;
const RANDOM_SUFFIX_BYTES = 3;

type ExecResult = Awaited<ReturnType<ExtensionAPI["exec"]>>;

type Success<TValue> = Readonly<{
	ok: true;
	value: TValue;
}>;

type Failure<TError> = Readonly<{
	ok: false;
	error: TError;
}>;

type Result<TValue, TError> = Success<TValue> | Failure<TError>;

type WorktreeError =
	| Readonly<{ type: "not-in-herdr" }>
	| Readonly<{ type: "missing-task-name" }>
	| Readonly<{ type: "invalid-task-name"; taskName: string }>
	| Readonly<{ type: "not-a-repository" }>
	| Readonly<{ type: "no-remotes" }>
	| Readonly<{ type: "remote-selection-required"; remotes: readonly string[] }>
	| Readonly<{ type: "cancelled" }>
	| Readonly<{ type: "default-branch-not-found"; remote: string }>
	| Readonly<{ type: "command-failed"; action: string; detail: string }>
	| Readonly<{ type: "command-exception"; action: string; cause: unknown }>
	| Readonly<{ type: "invalid-herdr-response"; action: string }>;

type CreatedWorktree = Readonly<{
	paneId: string;
	path: string;
}>;

function success<TValue>(value: TValue): Success<TValue> {
	return { ok: true, value };
}

function failure<TError>(error: TError): Failure<TError> {
	return { ok: false, error };
}

function isRecord(value: unknown): value is Record<string, unknown> {
	return typeof value === "object" && value !== null;
}

function toSlug(taskName: string): string {
	return taskName
		.normalize("NFKD")
		.replace(/[\u0300-\u036f]/g, "")
		.toLowerCase()
		.replace(/[^a-z0-9]+/g, "-")
		.replace(/^-+|-+$/g, "");
}

function withSuffix({
	value,
	suffix,
	maxLength,
}: {
	value: string;
	suffix: string;
	maxLength: number;
}): string {
	const maxValueLength = maxLength - suffix.length - 1;
	const truncated = value
		.slice(0, maxValueLength)
		.replace(/-+$/g, "");
	return `${truncated}-${suffix}`;
}

function toBranchName({ taskName, suffix }: { taskName: string; suffix: string }): string | undefined {
	const slug = toSlug(taskName);
	return slug ? withSuffix({ value: slug, suffix, maxLength: MAX_BRANCH_NAME_LENGTH }) : undefined;
}

function toAgentName({ taskName, suffix }: { taskName: string; suffix: string }): string {
	const slug = toSlug(taskName);
	const prefixed = /^[a-z]/.test(slug) ? slug : `pi-${slug || "worktree"}`;
	return withSuffix({ value: prefixed, suffix, maxLength: MAX_AGENT_NAME_LENGTH });
}

function commandDetail(result: ExecResult): string {
	return (result.stderr || result.stdout).trim() || `exit code ${result.code ?? "unknown"}`;
}

async function runCommand({
	pi,
	command,
	args,
	cwd,
	action,
	timeout,
}: {
	pi: ExtensionAPI;
	command: string;
	args: string[];
	cwd?: string;
	action: string;
	timeout?: number;
}): Promise<Result<ExecResult, WorktreeError>> {
	try {
		const result = await pi.exec(command, args, { cwd, timeout });
		if (result.code !== 0) {
			return failure({
				type: "command-failed",
				action,
				detail: commandDetail(result),
			});
		}
		return success(result);
	} catch (cause: unknown) {
		return failure({ type: "command-exception", action, cause });
	}
}

async function findRepository({
	pi,
	cwd,
}: {
	pi: ExtensionAPI;
	cwd: string;
}): Promise<Result<string, WorktreeError>> {
	const result = await runCommand({
		pi,
		command: "git",
		args: ["rev-parse", "--show-toplevel"],
		cwd,
		action: "Could not find the Git repository",
	});
	if (!result.ok) {
		if (result.error.type === "command-failed") {
			return failure({ type: "not-a-repository" });
		}
		return result;
	}
	return success(result.value.stdout.trim());
}

async function chooseRemote({
	pi,
	ctx,
	repository,
}: {
	pi: ExtensionAPI;
	ctx: ExtensionCommandContext;
	repository: string;
}): Promise<Result<string, WorktreeError>> {
	const result = await runCommand({
		pi,
		command: "git",
		args: ["remote"],
		cwd: repository,
		action: "Could not list Git remotes",
	});
	if (!result.ok) return result;

	const remotes = result.value.stdout
		.split(/\r?\n/)
		.map((remote) => remote.trim())
		.filter(Boolean);

	if (remotes.length === 0) return failure({ type: "no-remotes" });
	if (remotes.includes("origin")) return success("origin");
	if (remotes.length === 1) return success(remotes[0]);
	if (!ctx.hasUI) return failure({ type: "remote-selection-required", remotes });

	const remote = await ctx.ui.select("Select the remote for the new worktree", remotes);
	return remote ? success(remote) : failure({ type: "cancelled" });
}

async function getDefaultBranch({
	pi,
	repository,
	remote,
}: {
	pi: ExtensionAPI;
	repository: string;
	remote: string;
}): Promise<Result<string, WorktreeError>> {
	const result = await runCommand({
		pi,
		command: "git",
		args: ["ls-remote", "--symref", remote, "HEAD"],
		cwd: repository,
		action: `Could not determine ${remote}'s default branch`,
	});
	if (!result.ok) return result;

	const match = result.value.stdout.match(/^ref:\s+refs\/heads\/(.+)\s+HEAD\s*$/m);
	return match
		? success(match[1])
		: failure({ type: "default-branch-not-found", remote });
}

function parseCreatedWorktree(stdout: string): Result<CreatedWorktree, WorktreeError> {
	let response: unknown;
	try {
		response = JSON.parse(stdout);
	} catch {
		return failure({ type: "invalid-herdr-response", action: "create the worktree" });
	}

	if (!isRecord(response) || !isRecord(response.result)) {
		return failure({ type: "invalid-herdr-response", action: "create the worktree" });
	}

	const rootPane = response.result.root_pane;
	const worktree = response.result.worktree;
	if (
		!isRecord(rootPane)
		|| typeof rootPane.pane_id !== "string"
		|| !isRecord(worktree)
		|| typeof worktree.path !== "string"
	) {
		return failure({ type: "invalid-herdr-response", action: "create the worktree" });
	}

	return success({ paneId: rootPane.pane_id, path: worktree.path });
}

function formatError(error: WorktreeError): string {
	switch (error.type) {
		case "not-in-herdr":
			return "/worktree must be run from Pi inside a Herdr pane";
		case "missing-task-name":
			return "Usage: /worktree <task name>";
		case "invalid-task-name":
			return `Could not create a Git branch name from task: ${error.taskName}`;
		case "not-a-repository":
			return "The current directory is not inside a Git repository";
		case "no-remotes":
			return "This repository has no Git remotes";
		case "remote-selection-required":
			return `This repository has multiple remotes and no origin: ${error.remotes.join(", ")}`;
		case "cancelled":
			return "Worktree creation cancelled";
		case "default-branch-not-found":
			return `Could not determine ${error.remote}'s default branch`;
		case "command-failed":
			return `${error.action}: ${error.detail}`;
		case "command-exception": {
			const detail = error.cause instanceof Error ? error.cause.message : String(error.cause);
			return `${error.action}: ${detail}`;
		}
		case "invalid-herdr-response":
			return `Herdr returned an invalid response while trying to ${error.action}`;
	}
}

async function createWorktree({
	pi,
	ctx,
	taskName,
}: {
	pi: ExtensionAPI;
	ctx: ExtensionCommandContext;
	taskName: string;
}): Promise<Result<{ branch: string; path: string }, WorktreeError>> {
	if (process.env.HERDR_ENV !== "1" || !process.env.HERDR_PANE_ID) {
		return failure({ type: "not-in-herdr" });
	}

	const suffix = randomBytes(RANDOM_SUFFIX_BYTES).toString("hex");
	const branch = toBranchName({ taskName, suffix });
	if (!branch) return failure({ type: "invalid-task-name", taskName });
	const workspaceName = `${taskName}-${suffix}`;

	const repository = await findRepository({ pi, cwd: ctx.cwd });
	if (!repository.ok) return repository;

	const remote = await chooseRemote({ pi, ctx, repository: repository.value });
	if (!remote.ok) return remote;

	ctx.ui.setStatus(STATUS_KEY, `Fetching ${remote.value}...`);
	const fetch = await runCommand({
		pi,
		command: "git",
		args: ["fetch", "--prune", remote.value],
		cwd: repository.value,
		action: `Could not fetch ${remote.value}`,
	});
	if (!fetch.ok) return fetch;

	const defaultBranch = await getDefaultBranch({
		pi,
		repository: repository.value,
		remote: remote.value,
	});
	if (!defaultBranch.ok) return defaultBranch;

	const base = `${remote.value}/${defaultBranch.value}`;
	ctx.ui.setStatus(STATUS_KEY, `Creating ${branch} from ${base}...`);
	const create = await runCommand({
		pi,
		command: "herdr",
		args: [
			"worktree",
			"create",
			"--cwd",
			repository.value,
			"--branch",
			branch,
			"--base",
			base,
			"--label",
			workspaceName,
			"--focus",
		],
		action: "Could not create the Herdr worktree",
	});
	if (!create.ok) return create;

	const worktree = parseCreatedWorktree(create.value.stdout);
	if (!worktree.ok) return worktree;

	ctx.ui.setStatus(STATUS_KEY, "Naming the Herdr pane...");
	const rename = await runCommand({
		pi,
		command: "herdr",
		args: ["pane", "rename", worktree.value.paneId, taskName],
		action: `Worktree created at ${worktree.value.path}, but the pane could not be named`,
	});
	if (!rename.ok) return rename;

	ctx.ui.setStatus(STATUS_KEY, "Launching Pi in the worktree...");
	const start = await runCommand({
		pi,
		command: "herdr",
		args: [
			"agent",
			"start",
			toAgentName({ taskName, suffix }),
			"--kind",
			"pi",
			"--pane",
			worktree.value.paneId,
			"--timeout",
			"30000",
		],
		action: `Worktree created at ${worktree.value.path}, but Pi could not be launched`,
		timeout: 35_000,
	});
	if (!start.ok) return start;

	return success({ branch, path: worktree.value.path });
}

export default function gitWorktreeExtension(pi: ExtensionAPI): void {
	pi.registerCommand("worktree", {
		description: "Create a worktree from the latest remote default branch and launch Pi in Herdr",
		handler: async (args, ctx) => {
			const taskName = args.trim();
			if (!taskName) {
				ctx.ui.notify(formatError({ type: "missing-task-name" }), "error");
				return;
			}

			await ctx.waitForIdle();
			try {
				const result = await createWorktree({ pi, ctx, taskName });
				if (!result.ok) {
					ctx.ui.notify(formatError(result.error), result.error.type === "cancelled" ? "info" : "error");
					return;
				}

				ctx.ui.notify(
					`Created ${result.value.branch} at ${result.value.path} and launched Pi in a new Herdr pane`,
					"info",
				);
			} finally {
				ctx.ui.setStatus(STATUS_KEY, undefined);
			}
		},
	});
}
