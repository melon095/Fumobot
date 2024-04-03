import type { UserInfo } from '$lib/types';
import { userStore } from '../stores/user';

export const load = async ({ fetch }) => {
	const url = '/api/Account/User';
	try {
		const response = await fetch(url, {
			credentials: 'include'
		});

		if (!response.ok || response.status === 401) {
			return;
		}

		const data = (await response.json()) as UserInfo;

		userStore.set(data);
	} catch (error) {
		console.error(error);
	}
};

export const prerender = true;
export const trailingSlash = 'always';
