import assert from "node:assert/strict";
import test from "node:test";

import aspireLifecycleExtension from "./index.ts";

function expectedBrowserExecution(url) {
	switch (process.platform) {
		case "win32":
			return {
				executable: "rundll32.exe",
				args: ["url.dll,FileProtocolHandler", url],
				options: { cwd: "/repo" },
			};
		case "darwin":
			return { executable: "open", args: [url], options: { cwd: "/repo" } };
		default:
			return { executable: "xdg-open", args: [url], options: { cwd: "/repo" } };
	}
}

function createHarness(execute) {
	const commands = new Map();
	const notifications = [];
	const statuses = [];
	const executions = [];
	const pi = {
		registerCommand(name, options) {
			commands.set(name, options);
		},
		async exec(executable, args, options) {
			executions.push({ executable, args, options });
			return execute(executable, args, options);
		},
	};

	aspireLifecycleExtension(pi);

	const context = {
		cwd: "/repo",
		ui: {
			notify: (message, level) => notifications.push({ message, level }),
			setStatus: (key, value) => statuses.push({ key, value }),
		},
	};

	return { commands, context, executions, notifications, statuses };
}

test("registers start and stop commands", () => {
	const { commands } = createHarness(async () => ({ stdout: "", stderr: "", code: 0, killed: false }));

	assert.deepEqual([...commands.keys()], ["start", "stop"]);
	assert.equal(commands.get("start").description, "Start the Aspire application");
	assert.equal(commands.get("stop").description, "Stop the Aspire application");
});

test("start opens the Aspire dashboard in the default browser", async () => {
	const dashboardUrl = "https://localhost:1234/login?t=token";
	const harness = createHarness(async (executable) => {
		if (executable === "aspire") {
			return {
				stdout: `Starting Aspire AppHost in the background...\n{\n  "dashboardUrl": "${dashboardUrl}"\n}\n`,
				stderr: "",
				code: 0,
				killed: false,
			};
		}

		return { stdout: "", stderr: "", code: 0, killed: false };
	});

	await harness.commands.get("start").handler("", harness.context);

	assert.deepEqual(harness.executions, [
		{
			executable: "aspire",
			args: ["start", "--isolated", "--format", "Json", "--non-interactive", "--nologo"],
			options: { cwd: "/repo" },
		},
		expectedBrowserExecution(dashboardUrl),
	]);
	assert.deepEqual(harness.notifications, [
		{
			message: "Aspire application started\nDashboard opened in default browser",
			level: "info",
		},
	]);
	assert.deepEqual(harness.statuses, [
		{ key: "aspire-lifecycle", value: "Starting Aspire..." },
		{ key: "aspire-lifecycle", value: undefined },
	]);
});

test("reports a browser launch failure without reporting Aspire start as failed", async () => {
	const harness = createHarness(async (executable) => {
		if (executable === "aspire") {
			return {
				stdout: '{ "dashboardUrl": "https://localhost:1234" }\n',
				stderr: "",
				code: 0,
				killed: false,
			};
		}

		return { stdout: "", stderr: "Could not open browser", code: 1, killed: false };
	});

	await harness.commands.get("start").handler("", harness.context);

	assert.deepEqual(harness.notifications, [
		{ message: "Aspire application started", level: "info" },
		{ message: "Failed to open Aspire dashboard: exit code 1", level: "warning" },
	]);
});

test("reports when Aspire does not return a dashboard URL", async () => {
	const harness = createHarness(async () => ({
		stdout: '{ "status": "running" }\n',
		stderr: "",
		code: 0,
		killed: false,
	}));

	await harness.commands.get("start").handler("", harness.context);

	assert.equal(harness.executions.length, 1);
	assert.deepEqual(harness.notifications, [
		{ message: "Aspire application started", level: "info" },
		{
			message: "Failed to open Aspire dashboard: dashboard URL was not returned by Aspire",
			level: "warning",
		},
	]);
});

test("stop runs Aspire stop from the project directory", async () => {
	const harness = createHarness(async () => ({ stdout: "Stopped\n", stderr: "", code: 0, killed: false }));

	await harness.commands.get("stop").handler("", harness.context);

	assert.deepEqual(harness.executions, [
		{
			executable: "aspire",
			args: ["stop", "--non-interactive", "--nologo"],
			options: { cwd: "/repo" },
		},
	]);
	assert.deepEqual(harness.notifications.at(-1), {
		message: "Aspire application stopped\nStopped",
		level: "info",
	});
});

test("reports Aspire command failures", async () => {
	const harness = createHarness(async () => ({
		stdout: "",
		stderr: "No running AppHost\n",
		code: 1,
		killed: false,
	}));

	await harness.commands.get("stop").handler("", harness.context);

	assert.deepEqual(harness.notifications, [
		{
			message: "Failed to stop Aspire: No running AppHost",
			level: "error",
		},
	]);
});

test("rejects command arguments without invoking Aspire", async () => {
	const harness = createHarness(async () => ({ stdout: "", stderr: "", code: 0, killed: false }));

	await harness.commands.get("start").handler("unexpected", harness.context);

	assert.deepEqual(harness.executions, []);
	assert.deepEqual(harness.notifications, [{ message: "Usage: /start", level: "error" }]);
});
