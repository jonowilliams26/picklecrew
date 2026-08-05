import type { ExtensionAPI, ExtensionCommandContext } from "@earendil-works/pi-coding-agent";

const STATUS_KEY = "git-default-branch";

type ExecResult = Awaited<ReturnType<ExtensionAPI["exec"]>>;

function commandError(action: string, result: ExecResult): Error {
	const detail = (result.stderr || result.stdout).trim();
	return new Error(detail ? `${action}: ${detail}` : `${action} failed with exit code ${result.code}`);
}

async function git(
	pi: ExtensionAPI,
	cwd: string,
	args: string[],
): Promise<ExecResult> {
	return pi.exec("git", args, { cwd });
}

async function requireGit(
	pi: ExtensionAPI,
	cwd: string,
	args: string[],
	action: string,
): Promise<ExecResult> {
	const result = await git(pi, cwd, args);
	if (result.code !== 0) throw commandError(action, result);
	return result;
}

async function chooseRemote(
	pi: ExtensionAPI,
	ctx: ExtensionCommandContext,
	repository: string,
): Promise<string | undefined> {
	const result = await requireGit(pi, repository, ["remote"], "Could not list Git remotes");
	const remotes = result.stdout.split(/\r?\n/).map((remote) => remote.trim()).filter(Boolean);

	if (remotes.length === 0) throw new Error("This repository has no Git remotes");
	if (remotes.includes("origin")) return "origin";
	if (remotes.length === 1) return remotes[0];

	if (!ctx.hasUI) {
		throw new Error(`This repository has multiple remotes and no origin: ${remotes.join(", ")}`);
	}

	return ctx.ui.select("Select the remote whose default branch should be updated", remotes);
}

async function getDefaultBranch(pi: ExtensionAPI, repository: string, remote: string): Promise<string> {
	const remoteHead = await git(pi, repository, [
		"symbolic-ref",
		"--quiet",
		"--short",
		`refs/remotes/${remote}/HEAD`,
	]);
	const prefix = `${remote}/`;
	const symbolicRef = remoteHead.stdout.trim();
	if (remoteHead.code === 0 && symbolicRef.startsWith(prefix)) {
		return symbolicRef.slice(prefix.length);
	}

	const head = await requireGit(
		pi,
		repository,
		["ls-remote", "--symref", remote, "HEAD"],
		`Could not determine ${remote}'s default branch`,
	);
	const match = head.stdout.match(/^ref:\s+refs\/heads\/(.+)\s+HEAD\s*$/m);
	if (!match) throw new Error(`Could not determine ${remote}'s default branch`);
	return match[1];
}

async function discardChanges(pi: ExtensionAPI, repository: string): Promise<void> {
	await requireGit(pi, repository, ["reset", "--hard", "HEAD"], "Could not discard tracked changes");
	await requireGit(pi, repository, ["clean", "-fd"], "Could not remove untracked files");

	const status = await requireGit(
		pi,
		repository,
		["status", "--porcelain=v1", "--untracked-files=all"],
		"Could not verify the working tree",
	);
	if (status.stdout.trim()) {
		throw new Error("Some changes remain after reset and clean; refusing to switch branches");
	}
}

async function switchToDefaultBranch(
	pi: ExtensionAPI,
	ctx: ExtensionCommandContext,
): Promise<{ branch: string; remoteBranch: string } | undefined> {
	await ctx.waitForIdle();

	const root = await git(pi, ctx.cwd, ["rev-parse", "--show-toplevel"]);
	if (root.code !== 0) throw new Error("The current directory is not inside a Git repository");
	const repository = root.stdout.trim();

	const status = await requireGit(
		pi,
		repository,
		["status", "--porcelain=v1", "--untracked-files=all"],
		"Could not inspect the working tree",
	);
	const changes = status.stdout.split(/\r?\n/).filter(Boolean);

	if (changes.length > 0) {
		if (!ctx.hasUI) {
			throw new Error("The repository has uncommitted changes; refusing to discard them without confirmation");
		}

		const confirmed = await ctx.ui.confirm(
			"Discard all uncommitted changes?",
			`${changes.length} changed path(s) will be permanently discarded, including untracked files. Continue?`,
		);
		if (!confirmed) {
			ctx.ui.notify("Branch switch cancelled", "info");
			return;
		}

		ctx.ui.setStatus(STATUS_KEY, "Discarding changes...");
		await discardChanges(pi, repository);
	}

	const remote = await chooseRemote(pi, ctx, repository);
	if (!remote) {
		ctx.ui.notify("Branch switch cancelled", "info");
		return;
	}

	ctx.ui.setStatus(STATUS_KEY, `Fetching ${remote}...`);
	await requireGit(pi, repository, ["fetch", "--prune", remote], `Could not fetch ${remote}`);
	const branch = await getDefaultBranch(pi, repository, remote);
	const remoteBranch = `${remote}/${branch}`;

	const localBranch = await git(pi, repository, ["show-ref", "--verify", "--quiet", `refs/heads/${branch}`]);
	ctx.ui.setStatus(STATUS_KEY, `Switching to ${branch}...`);
	if (localBranch.code === 0) {
		await requireGit(pi, repository, ["switch", branch], `Could not switch to ${branch}`);
	} else {
		await requireGit(
			pi,
			repository,
			["switch", "--track", "-c", branch, remoteBranch],
			`Could not create ${branch} from ${remoteBranch}`,
		);
	}

	ctx.ui.setStatus(STATUS_KEY, `Pulling ${remoteBranch}...`);
	await requireGit(
		pi,
		repository,
		["pull", "--ff-only", remote, branch],
		`Could not fast-forward ${branch} from ${remoteBranch}`,
	);

	return { branch, remoteBranch };
}

export default function gitDefaultBranchExtension(pi: ExtensionAPI) {
	const command = {
		description: "Switch to the updated default branch, reload Pi, and start a new session",
		handler: async (_args: string, ctx: ExtensionCommandContext) => {
			let switched: { branch: string; remoteBranch: string } | undefined;
			try {
				switched = await switchToDefaultBranch(pi, ctx);
			} catch (error) {
				const message = error instanceof Error ? error.message : String(error);
				ctx.ui.notify(message, "error");
			} finally {
				ctx.ui.setStatus(STATUS_KEY, undefined);
			}

			if (!switched) return;

			// Session replacement reloads and rebinds Pi's extensions and resources.
			// Use only the replacement context after this succeeds; the original is stale.
			const newSession = await ctx.newSession({
				withSession: async (replacementCtx) => {
					replacementCtx.ui.notify(
						`Switched to ${switched.branch}, pulled the latest changes from ${switched.remoteBranch}, reloaded Pi, and started a new session`,
						"info",
					);
				},
			});

			if (newSession.cancelled) {
				ctx.ui.notify("New session cancelled", "info");
			}
		},
	};

	pi.registerCommand("main", command);
	pi.registerCommand("master", command);
}
