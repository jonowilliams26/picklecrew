import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";
import { networkInterfaces, platform } from "node:os";

type LifecycleCommand = "start" | "start-mobile" | "stop";
type LifecycleAction = "start" | "stop";

type BrowserCommand = Readonly<{
	executable: string;
	args: readonly string[];
}>;

type LanIpv4Address = Readonly<{
	address: string;
	interfaceName: string;
}>;

type CommandConfiguration = Readonly<{
	action: LifecycleAction;
	args: readonly string[];
	description: string;
	inProgressMessage: string;
	requiresWebHost?: boolean;
	successMessage: string;
}>;

const STATUS_KEY = "aspire-lifecycle";
const MOBILE_WEB_PORT = 3000;
const DASHBOARD_URL_PATTERN = /"dashboardUrl"\s*:\s*"(https?:\/\/[^"\r\n]+)"/i;
const LIFECYCLE_COMMANDS = ["start", "start-mobile", "stop"] as const satisfies readonly LifecycleCommand[];
const START_ARGUMENTS = [
	"start",
	"--isolated",
	"--format",
	"Json",
	"--non-interactive",
	"--nologo",
] as const;
const COMMAND_CONFIGURATIONS: Readonly<Record<LifecycleCommand, CommandConfiguration>> = {
	start: {
		action: "start",
		args: START_ARGUMENTS,
		description: "Start the Aspire application",
		inProgressMessage: "Starting Aspire...",
		successMessage: "Aspire application started",
	},
	"start-mobile": {
		action: "start",
		args: START_ARGUMENTS,
		description: `Start Aspire for mobile access on port ${MOBILE_WEB_PORT}`,
		inProgressMessage: "Starting Aspire for mobile access...",
		requiresWebHost: true,
		successMessage: "Aspire application started for mobile access",
	},
	stop: {
		action: "stop",
		args: ["stop", "--non-interactive", "--nologo"],
		description: "Stop the Aspire application",
		inProgressMessage: "Stopping Aspire...",
		successMessage: "Aspire application stopped",
	},
};

export function findLanIpv4Addresses({
	interfaces = networkInterfaces(),
}: {
	interfaces?: ReturnType<typeof networkInterfaces>;
} = {}): readonly LanIpv4Address[] {
	const addresses: LanIpv4Address[] = [];
	const seenAddresses = new Set<string>();

	for (const [interfaceName, interfaceAddresses] of Object.entries(interfaces)) {
		for (const interfaceAddress of interfaceAddresses ?? []) {
			if (
				interfaceAddress.family !== "IPv4"
				|| interfaceAddress.internal
				|| interfaceAddress.address.startsWith("169.254.")
				|| seenAddresses.has(interfaceAddress.address)
			) {
				continue;
			}

			seenAddresses.add(interfaceAddress.address);
			addresses.push({ address: interfaceAddress.address, interfaceName });
		}
	}

	return addresses;
}

function formatCommandOutput({ stdout, stderr }: { stdout: string; stderr: string }): string {
	return stdout.trim() || stderr.trim();
}

function extractDashboardUrl({ output }: { output: string }): string | undefined {
	return DASHBOARD_URL_PATTERN.exec(output)?.[1];
}

function createBrowserCommand({ url }: { url: string }): BrowserCommand {
	switch (platform()) {
		case "win32":
			return {
				executable: "rundll32.exe",
				args: ["url.dll,FileProtocolHandler", url],
			};
		case "darwin":
			return { executable: "open", args: [url] };
		default:
			return { executable: "xdg-open", args: [url] };
	}
}

export function createAspireLifecycleExtension({
	getLanIpv4Addresses,
}: {
	getLanIpv4Addresses: () => readonly LanIpv4Address[];
}): (pi: ExtensionAPI) => void {
	return function aspireLifecycleExtension(pi: ExtensionAPI): void {
		let activeCommand: LifecycleCommand | undefined;

		for (const command of LIFECYCLE_COMMANDS) {
			const configuration = COMMAND_CONFIGURATIONS[command];

			pi.registerCommand(command, {
				description: configuration.description,
				handler: async (args, ctx) => {
					if (args.trim()) {
						ctx.ui.notify(`Usage: /${command}`, "error");
						return;
					}

					if (activeCommand) {
						ctx.ui.notify(`Aspire ${activeCommand} is already in progress`, "warning");
						return;
					}

					let webHost: string | undefined;

					if (configuration.requiresWebHost) {
						const lanAddresses = getLanIpv4Addresses();

						if (lanAddresses.length === 0) {
							ctx.ui.notify("No LAN IPv4 address was found on this device", "error");
							return;
						}

						if (lanAddresses.length === 1) {
							webHost = lanAddresses[0]?.address;
						} else {
							const addressOptions = lanAddresses.map(
								({ address, interfaceName }) => `${address} (${interfaceName})`,
							);
							const selectedAddress = await ctx.ui.select(
								"Select the LAN address for mobile access",
								addressOptions,
							);
							const selectedIndex = selectedAddress
								? addressOptions.indexOf(selectedAddress)
								: -1;

							if (selectedIndex === -1) {
								ctx.ui.notify("Mobile start cancelled", "info");
								return;
							}

							webHost = lanAddresses[selectedIndex]?.address;
						}

						if (!webHost) {
							ctx.ui.notify("The selected LAN IPv4 address is unavailable", "error");
							return;
						}
					}

					activeCommand = command;
					ctx.ui.setStatus(STATUS_KEY, configuration.inProgressMessage);

					try {
						const aspireArguments = [...configuration.args];
						const mobileUrl = webHost ? `http://${webHost}:${MOBILE_WEB_PORT}` : undefined;

						if (webHost) {
							aspireArguments.push(
								"--",
								"--web-host",
								webHost,
								"--web-port",
								String(MOBILE_WEB_PORT),
							);
						}

						const result = await pi.exec("aspire", aspireArguments, { cwd: ctx.cwd });
						const output = formatCommandOutput(result);

						if (result.code !== 0) {
							ctx.ui.notify(
								`Failed to ${configuration.action} Aspire: ${output || `exit code ${result.code}`}`,
								"error",
							);
							return;
						}

						let successDetails = output;
						let browserFailure: string | undefined;

						if (configuration.action === "start") {
							successDetails = mobileUrl ? `Mobile URL: ${mobileUrl}` : "";
							const dashboardUrl = extractDashboardUrl({ output });

							if (!dashboardUrl) {
								browserFailure = "dashboard URL was not returned by Aspire";
							} else {
								const browserCommand = createBrowserCommand({ url: dashboardUrl });

								try {
									const browserResult = await pi.exec(
										browserCommand.executable,
										[...browserCommand.args],
										{ cwd: ctx.cwd },
									);

									if (browserResult.code === 0) {
										successDetails = [successDetails, "Dashboard opened in default browser"]
											.filter(Boolean)
											.join("\n");
									} else {
										browserFailure = `exit code ${browserResult.code}`;
									}
								} catch (cause: unknown) {
									browserFailure = cause instanceof Error ? cause.message : String(cause);
								}
							}
						}

						ctx.ui.notify(
							successDetails
								? `${configuration.successMessage}\n${successDetails}`
								: configuration.successMessage,
							"info",
						);

						if (browserFailure) {
							ctx.ui.notify(`Failed to open Aspire dashboard: ${browserFailure}`, "warning");
						}
					} catch (cause: unknown) {
						const details = cause instanceof Error ? cause.message : String(cause);
						ctx.ui.notify(`Failed to ${configuration.action} Aspire: ${details}`, "error");
					} finally {
						activeCommand = undefined;
						ctx.ui.setStatus(STATUS_KEY, undefined);
					}
				},
			});
		}
	};
}

export default createAspireLifecycleExtension({
	getLanIpv4Addresses: findLanIpv4Addresses,
});
