import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";

type LifecycleCommand = "start" | "stop";

type CommandConfiguration = Readonly<{
	args: readonly string[];
	inProgressMessage: string;
	successMessage: string;
}>;

const STATUS_KEY = "aspire-lifecycle";
const LIFECYCLE_COMMANDS = ["start", "stop"] as const satisfies readonly LifecycleCommand[];
const COMMAND_CONFIGURATIONS: Readonly<Record<LifecycleCommand, CommandConfiguration>> = {
	start: {
		args: ["start", "--isolated"],
		inProgressMessage: "Starting Aspire...",
		successMessage: "Aspire application started",
	},
	stop: {
		args: ["stop"],
		inProgressMessage: "Stopping Aspire...",
		successMessage: "Aspire application stopped",
	},
};

function formatCommandOutput({ stdout, stderr }: { stdout: string; stderr: string }): string {
	return stdout.trim() || stderr.trim();
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

					ctx.ui.notify(
						output ? `${configuration.successMessage}\n${output}` : configuration.successMessage,
						"info",
					);
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
