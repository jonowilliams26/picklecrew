import assert from "node:assert/strict";
import test from "node:test";

import aspireLifecycleExtension from "./index.ts";

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

test("start runs Aspire in isolated mode from the project directory", async () => {
	const harness = createHarness(async () => ({
		stdout: "Dashboard: https://localhost:1234\n",
		stderr: "",
		code: 0,
		killed: false,
	}));

	await harness.commands.get("start").handler("", harness.context);

	assert.deepEqual(harness.executions, [
		{
			executable: "aspire",
			args: ["start", "--isolated"],
			options: { cwd: "/repo" },
		},
	]);
	assert.deepEqual(harness.notifications, [
		{
			message: "Aspire application started\nDashboard: https://localhost:1234",
			level: "info",
		},
	]);
	assert.deepEqual(harness.statuses, [
		{ key: "aspire-lifecycle", value: "Starting Aspire..." },
		{ key: "aspire-lifecycle", value: undefined },
	]);
});

test("stop runs Aspire stop from the project directory", async () => {
	const harness = createHarness(async () => ({ stdout: "Stopped\n", stderr: "", code: 0, killed: false }));

	await harness.commands.get("stop").handler("", harness.context);

	assert.deepEqual(harness.executions, [
		{
			executable: "aspire",
			args: ["stop"],
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
