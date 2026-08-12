import assert from "node:assert/strict";
import test from "node:test";

import releaseExtension, {
	buildCreateReleaseArguments,
	findLatestVersion,
	formatVersionTag,
	incrementVersion,
	parseReleaseType,
} from "./index.ts";

test("parseReleaseType accepts exactly one supported release type", () => {
	assert.deepEqual(parseReleaseType(" major "), { ok: true, value: "major" });
	assert.deepEqual(parseReleaseType("minor"), { ok: true, value: "minor" });
	assert.deepEqual(parseReleaseType("patch"), { ok: true, value: "patch" });
});

test("parseReleaseType rejects missing or unsupported arguments", () => {
	assert.equal(parseReleaseType("").ok, false);
	assert.equal(parseReleaseType("major patch").ok, false);
	assert.equal(parseReleaseType("Major").ok, false);
});

test("findLatestVersion returns the highest stable semantic version tag", () => {
	assert.deepEqual(findLatestVersion(["v1.9.9", "v2.0.0-beta.1", "other", "v1.10.0", "v0.20.0"]), {
		major: 1,
		minor: 10,
		patch: 0,
	});
});

test("findLatestVersion starts at zero when no stable version tag exists", () => {
	assert.deepEqual(findLatestVersion(["v1.0.0-rc.1", "release"]), { major: 0, minor: 0, patch: 0 });
});

test("incrementVersion applies semantic version rules", () => {
	const version = { major: 1, minor: 2, patch: 3 };

	assert.equal(formatVersionTag(incrementVersion({ version, releaseType: "major" })), "v2.0.0");
	assert.equal(formatVersionTag(incrementVersion({ version, releaseType: "minor" })), "v1.3.0");
	assert.equal(formatVersionTag(incrementVersion({ version, releaseType: "patch" })), "v1.2.4");
});

test("buildCreateReleaseArguments generates notes and targets main", () => {
	assert.deepEqual(buildCreateReleaseArguments("v1.2.4"), [
		"release",
		"create",
		"v1.2.4",
		"--generate-notes",
		"--target",
		"main",
	]);
});

test("release command creates the next release after confirmation", async () => {
	let command;
	const executions = [];
	const notifications = [];
	const pi = {
		registerCommand(name, options) {
			assert.equal(name, "release");
			command = options;
		},
		async exec(executable, args, options) {
			executions.push({ executable, args, options });
			if (args[0] === "api") {
				return { stdout: "v0.0.5\nv0.0.4\n", stderr: "", code: 0, killed: false };
			}
			return { stdout: "https://github.com/example/repo/releases/tag/v0.0.6\n", stderr: "", code: 0, killed: false };
		},
	};
	releaseExtension(pi);

	assert.deepEqual(command.getArgumentCompletions(""), [
		{
			value: "major",
			label: "major",
			description: "Increment the major version and reset minor and patch to zero",
		},
		{
			value: "minor",
			label: "minor",
			description: "Increment the minor version and reset patch to zero",
		},
		{
			value: "patch",
			label: "patch",
			description: "Increment the patch version",
		},
	]);

	await command.handler("patch", {
		cwd: "/repo",
		hasUI: true,
		waitForIdle: async () => {},
		ui: {
			confirm: async () => true,
			notify: (message, level) => notifications.push({ message, level }),
			setStatus: () => {},
		},
	});

	assert.deepEqual(executions[1], {
		executable: "gh",
		args: ["release", "create", "v0.0.6", "--generate-notes", "--target", "main"],
		options: { cwd: "/repo", timeout: 120_000 },
	});
	assert.deepEqual(notifications.at(-1), {
		message: "Created v0.0.6: https://github.com/example/repo/releases/tag/v0.0.6",
		level: "info",
	});
});
