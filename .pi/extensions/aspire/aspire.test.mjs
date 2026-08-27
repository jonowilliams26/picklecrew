import assert from "node:assert/strict";
import test from "node:test";

import {
	createAspireLifecycleExtension,
	findLanIpv4Addresses,
} from "./index.ts";

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

function createHarness(
	execute,
	{
		lanIpv4Addresses = [{ address: "192.168.1.42", interfaceName: "Wi-Fi" }],
		selectedLanAddress,
	} = {},
) {
	const commands = new Map();
	const notifications = [];
	const selections = [];
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

	createAspireLifecycleExtension({
		getLanIpv4Addresses: () => lanIpv4Addresses,
	})(pi);

	const context = {
		cwd: "/repo",
		ui: {
			notify: (message, level) => notifications.push({ message, level }),
			select: async (title, options) => {
				selections.push({ title, options });
				return selectedLanAddress;
			},
			setStatus: (key, value) => statuses.push({ key, value }),
		},
	};

	return { commands, context, executions, notifications, selections, statuses };
}

test("finds unique non-loopback LAN IPv4 addresses", () => {
	const addresses = findLanIpv4Addresses({
		interfaces: {
			"Wi-Fi": [
				{ address: "192.168.1.42", family: "IPv4", internal: false },
				{ address: "fe80::1", family: "IPv6", internal: false },
			],
			"Virtual adapter": [
				{ address: "192.168.1.42", family: "IPv4", internal: false },
				{ address: "169.254.1.2", family: "IPv4", internal: false },
			],
			Loopback: [{ address: "127.0.0.1", family: "IPv4", internal: true }],
		},
	});

	assert.deepEqual(addresses, [{ address: "192.168.1.42", interfaceName: "Wi-Fi" }]);
});

test("registers start, start-mobile, and stop commands", () => {
	const { commands } = createHarness(async () => ({ stdout: "", stderr: "", code: 0, killed: false }));

	assert.deepEqual([...commands.keys()], ["start", "start-mobile", "stop"]);
	assert.equal(commands.get("start").description, "Start the Aspire application");
	assert.equal(commands.get("start-mobile").description, "Start Aspire for mobile access on port 3000");
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

test("start-mobile discovers the LAN address and exposes the web frontend on port 3000", async () => {
	const dashboardUrl = "https://localhost:1234/login?t=token";
	const harness = createHarness(async (executable) => {
		if (executable === "aspire") {
			return {
				stdout: `{ "dashboardUrl": "${dashboardUrl}" }\n`,
				stderr: "",
				code: 0,
				killed: false,
			};
		}

		return { stdout: "", stderr: "", code: 0, killed: false };
	});

	await harness.commands.get("start-mobile").handler("", harness.context);

	assert.deepEqual(harness.executions, [
		{
			executable: "aspire",
			args: [
				"start",
				"--isolated",
				"--format",
				"Json",
				"--non-interactive",
				"--nologo",
				"--",
				"--web-host",
				"192.168.1.42",
				"--web-port",
				"3000",
			],
			options: { cwd: "/repo" },
		},
		expectedBrowserExecution(dashboardUrl),
	]);
	assert.deepEqual(harness.notifications, [
		{
			message: [
				"Aspire application started for mobile access",
				"Mobile URL: http://192.168.1.42:3000",
				"Dashboard opened in default browser",
			].join("\n"),
			level: "info",
		},
	]);
	assert.deepEqual(harness.statuses, [
		{ key: "aspire-lifecycle", value: "Starting Aspire for mobile access..." },
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

test("start-mobile asks which LAN address to use when multiple are available", async () => {
	const harness = createHarness(
		async () => ({ stdout: "", stderr: "Port conflict", code: 1, killed: false }),
		{
			lanIpv4Addresses: [
				{ address: "192.168.1.42", interfaceName: "Wi-Fi" },
				{ address: "172.18.48.1", interfaceName: "vEthernet (WSL)" },
			],
			selectedLanAddress: "192.168.1.42 (Wi-Fi)",
		},
	);

	await harness.commands.get("start-mobile").handler("", harness.context);

	assert.deepEqual(harness.selections, [
		{
			title: "Select the LAN address for mobile access",
			options: ["192.168.1.42 (Wi-Fi)", "172.18.48.1 (vEthernet (WSL))"],
		},
	]);
	assert.deepEqual(harness.executions[0].args.slice(-5), [
		"--",
		"--web-host",
		"192.168.1.42",
		"--web-port",
		"3000",
	]);
});

test("start-mobile reports when no LAN IPv4 address is available", async () => {
	const harness = createHarness(
		async () => ({ stdout: "", stderr: "", code: 0, killed: false }),
		{ lanIpv4Addresses: [] },
	);

	await harness.commands.get("start-mobile").handler("", harness.context);

	assert.deepEqual(harness.executions, []);
	assert.deepEqual(harness.notifications, [
		{ message: "No LAN IPv4 address was found on this device", level: "error" },
	]);
});
