import type { ExtensionAPI } from "@earendil-works/pi-coding-agent";
import defaultBranch from "./default-branch.ts";
import worktree from "./worktree.ts";

type GitExtension = (pi: ExtensionAPI) => void | Promise<void>;

const extensions: GitExtension[] = [
	defaultBranch,
	worktree,
];

export default async function gitExtensions(pi: ExtensionAPI): Promise<void> {
	for (const register of extensions) {
		await register(pi);
	}
}
