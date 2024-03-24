import type { Command } from '$lib/types';
import { error } from '@sveltejs/kit';
import type { PageLoad } from './$types';

export const ssr = false;
export const load: PageLoad = async ({ fetch }) => {
	const resp = await fetch(`/api/commands`);
	if (!resp.ok) {
		error(404, {
			message: 'Not found'
		});
	}

	const commands: Command[] = await resp.json();

	return {
		props: {
			commands
		}
	};
};
