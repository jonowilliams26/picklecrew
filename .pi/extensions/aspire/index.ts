import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";
import { platform } from "node:os";

type LifecycleCommand = "start" | "stop";

type BrowserCommand = Readonly<{
	executable: string;
	args: readonly string[];
}>;

type CommandConfiguration = Readonly<{
	args: readonly string[];
	inProgressMessage: string;
	successMessage: string;
}>;

const STATUS_KEY = "aspire-lifecycle";
const DASHBOARD_URL_PATTERN = /"dashboardUrl"\s*:\s*"(https?:\/\/[^"\r\n]+)"/i;
const LIFECYCLE_COMMANDS = ["start", "stop"] as const satisfies readonly LifecycleCommand[];
const COMMAND_CONFIGURATIONS: Readonly<Record<LifecycleCommand, CommandConfiguration>> = {
	start: {
		args: ["start", "--isolated", "--format", "Json", "--non-interactive", "--nologo"],
		inProgressMessage: "Starting Aspire...",
		successMessage: "Aspire application started",
	},
	stop: {
		args: ["stop", "--non-interactive", "--nologo"],
		inProgressMessage: "Stopping Aspire...",
		successMessage: "Aspire application stopped",
	},
};

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

export default function aspireLifecycleExtension(pi: ExtensionAPI): void {
	let activeCommand: LifecycleCommand | undefined;

	for (const command of LIFECYCLE_COMMANDS) {
		const configuration = COMMAND_CONFIGURATIONS[command];

		pi.registerCommand(command, {
			description: `${command === "start" ? "Start" : "Stop"} the Aspire application`,
			handler: async (args, ctx) => {
				if (args.trim()) {
					ctx.ui.notify(`Usage: /${command}`, "error");
					return;
				}

				if (activeCommand) {
					ctx.ui.notify(`Aspire ${activeCommand} is already in progress`, "warning");
					return;
				}

				activeCommand = command;
				ctx.ui.setStatus(STATUS_KEY, configuration.inProgressMessage);

				try {
					const result = await pi.exec("aspire", [...configuration.args], { cwd: ctx.cwd });
					const output = formatCommandOutput(result);

					if (result.code !== 0) {
						ctx.ui.notify(
							`Failed to ${command} Aspire: ${output || `exit code ${result.code}`}`,
							"error",
						);
						return;
					}

					let successDetails = output;
					let browserFailure: string | undefined;

					if (command === "start") {
						successDetails = "";
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
									successDetails = "Dashboard opened in default browser";
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
					ctx.ui.notify(`Failed to ${command} Aspire: ${details}`, "error");
				} finally {
					activeCommand = undefined;
					ctx.ui.setStatus(STATUS_KEY, undefined);
				}
			},
		});
	}
}
