import type { UserInfo } from '$lib/types';
import { writable } from 'svelte/store';

export const userStore = writable<UserInfo | null>(null);
