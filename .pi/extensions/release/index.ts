import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";

export type ReleaseType = "major" | "minor" | "patch";

export type Version = Readonly<{
	major: number;
	minor: number;
	patch: number;
}>;

type Success<TValue> = Readonly<{
	ok: true;
	value: TValue;
}>;

type Failure<TError> = Readonly<{
	ok: false;
	error: TError;
}>;

type Result<TValue, TError> = Success<TValue> | Failure<TError>;

type InvalidReleaseTypeError = Readonly<{
	type: "invalid-release-type";
}>;

type GitHubCommandError = Readonly<{
	type: "github-command-failed";
	details: string;
}>;

const RELEASE_TYPES: readonly ReleaseType[] = ["major", "minor", "patch"];
const RELEASE_TYPE_DESCRIPTIONS: Readonly<Record<ReleaseType, string>> = {
	major: "Increment the major version and reset minor and patch to zero",
	minor: "Increment the minor version and reset patch to zero",
	patch: "Increment the patch version",
};
const EMPTY_VERSION: Version = { major: 0, minor: 0, patch: 0 };
const COMMAND_TIMEOUT_MS = 120_000;

function success<TValue>(value: TValue): Success<TValue> {
	return { ok: true, value };
}

function failure<TError>(error: TError): Failure<TError> {
	return { ok: false, error };
}

export function parseReleaseType(args: string): Result<ReleaseType, InvalidReleaseTypeError> {
	const value = args.trim();
	const releaseType = RELEASE_TYPES.find((candidate) => candidate === value);

	return releaseType ? success(releaseType) : failure({ type: "invalid-release-type" });
}

function parseVersionTag(tag: string): Version | undefined {
	const match = /^v(\d+)\.(\d+)\.(\d+)$/.exec(tag);
	if (!match) {
		return undefined;
	}

	return {
		major: Number(match[1]),
		minor: Number(match[2]),
		patch: Number(match[3]),
	};
}

function compareVersions(left: Version, right: Version): number {
	return left.major - right.major || left.minor - right.minor || left.patch - right.patch;
}

export function findLatestVersion(tags: readonly string[]): Version {
	return tags
		.map(parseVersionTag)
		.filter((version): version is Version => version !== undefined)
		.reduce((latest, version) => (compareVersions(version, latest) > 0 ? version : latest), EMPTY_VERSION);
}

export function incrementVersion({
	version,
	releaseType,
}: {
	version: Version;
	releaseType: ReleaseType;
}): Version {
	switch (releaseType) {
		case "major":
			return { major: version.major + 1, minor: 0, patch: 0 };
		case "minor":
			return { major: version.major, minor: version.minor + 1, patch: 0 };
		case "patch":
			return { major: version.major, minor: version.minor, patch: version.patch + 1 };
	}
}

export function formatVersionTag(version: Version): string {
	return `v${version.major}.${version.minor}.${version.patch}`;
}

export function buildCreateReleaseArguments(tag: string): string[] {
	return ["release", "create", tag, "--generate-notes", "--target", "main"];
}

async function runGitHubCommand({
	pi,
	args,
	cwd,
}: {
	pi: ExtensionAPI;
	args: string[];
	cwd: string;
}): Promise<Result<string, GitHubCommandError>> {
	try {
		const result = await pi.exec("gh", args, { cwd, timeout: COMMAND_TIMEOUT_MS });
		if (result.code !== 0) {
			return failure({
				type: "github-command-failed",
				details: result.stderr.trim() || result.stdout.trim() || `exit code ${result.code}`,
			});
		}

		return success(result.stdout.trim());
	} catch (cause: unknown) {
		return failure({
			type: "github-command-failed",
			details: cause instanceof Error ? cause.message : String(cause),
		});
	}
}

async function listRemoteTags({ pi, cwd }: { pi: ExtensionAPI; cwd: string }): Promise<Result<string[], GitHubCommandError>> {
	const result = await runGitHubCommand({
		pi,
		cwd,
		args: [
			"api",
			"--paginate",
			"--method",
			"GET",
			"repos/{owner}/{repo}/tags",
			"-f",
			"per_page=100",
			"--jq",
			".[].name",
		],
	});
	if (!result.ok) {
		return result;
	}

	return success(result.value.split("\n").filter(Boolean));
}

export default function releaseExtension(pi: ExtensionAPI): void {
	let releaseInProgress = false;

	pi.registerCommand("release", {
		description: "Create a production GitHub release from main (major, minor, or patch)",
		getArgumentCompletions: (prefix) => {
			const normalizedPrefix = prefix.trim();
			const matches = RELEASE_TYPES.filter((releaseType) => releaseType.startsWith(normalizedPrefix));
			return matches.length > 0
				? matches.map((releaseType) => ({
						value: releaseType,
						label: releaseType,
						description: RELEASE_TYPE_DESCRIPTIONS[releaseType],
					}))
				: null;
		},
		handler: async (args, ctx) => {
			const parsedReleaseType = parseReleaseType(args);
			if (!parsedReleaseType.ok) {
				ctx.ui.notify("Usage: /release major|minor|patch", "error");
				return;
			}

			if (!ctx.hasUI) {
				ctx.ui.notify("/release requires an interactive confirmation", "error");
				return;
			}

			if (releaseInProgress) {
				ctx.ui.notify("A release is already in progress", "warning");
				return;
			}

			releaseInProgress = true;
			ctx.ui.setStatus("release", "Resolving next release version...");

			try {
				await ctx.waitForIdle();

				const tagsResult = await listRemoteTags({ pi, cwd: ctx.cwd });
				if (!tagsResult.ok) {
					ctx.ui.notify(`Failed to read GitHub tags: ${tagsResult.error.details}`, "error");
					return;
				}

				const latestVersion = findLatestVersion(tagsResult.value);
				const nextVersion = incrementVersion({
					version: latestVersion,
					releaseType: parsedReleaseType.value,
				});
				const tag = formatVersionTag(nextVersion);
				const confirmed = await ctx.ui.confirm(
					"Create production release?",
					`Create ${tag} from main with generated release notes?`,
				);
				if (!confirmed) {
					ctx.ui.notify("Release cancelled", "info");
					return;
				}

				ctx.ui.setStatus("release", `Creating ${tag}...`);
				const createResult = await runGitHubCommand({
					pi,
					cwd: ctx.cwd,
					args: buildCreateReleaseArguments(tag),
				});
				if (!createResult.ok) {
					ctx.ui.notify(`Failed to create ${tag}: ${createResult.error.details}`, "error");
					return;
				}

				const releaseUrl = createResult.value;
				ctx.ui.notify(releaseUrl ? `Created ${tag}: ${releaseUrl}` : `Created ${tag}`, "info");
			} finally {
				releaseInProgress = false;
				ctx.ui.setStatus("release", undefined);
			}
		},
	});
}
