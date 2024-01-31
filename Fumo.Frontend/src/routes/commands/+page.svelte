<script lang="ts">
	import {
		Table,
		TableBody,
		TableBodyCell,
		TableBodyRow,
		TableHead,
		TableHeadCell
	} from 'flowbite-svelte';
	import { onMount } from 'svelte';
	import { pushState } from '$app/navigation';
	import { page } from '$app/stores';
	import Markdown from '$lib/markdown.svelte';
	import type { PageData } from './$types';
	import type { CommandModal } from '$lib/types';

	export let data: PageData;

	const commands = data.props.commands;

	const detailCache = new Map<string, CommandModal>();
	let openRow = -1;
	let details: CommandModal | undefined;
	let searchTerm = '';

	async function toggleRow(i: number) {
		if (openRow == i) {
			openRow = -1;
			details = undefined;
			return;
		}

		const query = new URLSearchParams(window.location.search);
		query.set('c', commands[i].name);

		pushState(`?${query}`, $page.state);

		details = await fetchDetails(commands[i].name);
		openRow = i;
	}

	async function fetchDetails(name: string) {
		if (detailCache.has(name)) {
			return detailCache.get(name);
		}

		const res = await fetch(`/api/commands/${name}`);
		const json = await res.json();

		detailCache.set(name, json);

		return json;
	}

	onMount(async () => {
		// check if there is a "c" query param, if so set the row
		const query = new URLSearchParams(window.location.search);
		const command = query.get('c');

		if (!command) return;

		const index = commands.findIndex((c) => c.name == command);

		if (index == -1) return;

		details = await fetchDetails(command);
		openRow = index;
	});

	$: filteredCommands = !searchTerm
		? commands
		: commands.filter((c) => c.name.toLowerCase().includes(searchTerm.toLowerCase()));
</script>

<!-- Note: I know TableSearch exists. :) -->

<section id="cmd-sct">
	<div class="relative overflow-x-auto shadow-md">
		<div class="p-4">
			<label for="cmd-search" class="sr-only">Search</label>
			<div class="relative mt-1">
				<div class="absolute inset-y-0 start-0 flex items-center ps-3 pointer-events-none">
					<slot name="svgSearch">
						<svg
							class="w-5 h-5 text-gray-500 dark:text-gray-400"
							fill="currentColor"
							viewBox="0 0 20 20"
							xmlns="http://www.w3.org/2000/svg"
						>
							<path
								fill-rule="evenodd"
								d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"
								clip-rule="evenodd"
							/>
						</svg>
					</slot>
				</div>
				<input
					bind:value={searchTerm}
					type="text"
					placeholder="Search by name"
					class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-80 p-2.5 ps-10"
				/>
			</div>
		</div>
	</div>

	<Table class="shadow-md">
		<TableHead>
			<TableHeadCell>Name</TableHeadCell>
			<TableHeadCell>Description</TableHeadCell>
			<TableHeadCell>Cooldown</TableHeadCell>
		</TableHead>
		<TableBody tableBodyClass="divide-y">
			{#each filteredCommands as command, i}
				<TableBodyRow
					on:click={() => toggleRow(i)}
					class="no-underline select-none cursor-pointer hover:bg-blue-100"
				>
					<TableBodyCell>{command.name}</TableBodyCell>
					<TableBodyCell>{command.description}</TableBodyCell>
					<TableBodyCell>{command.cooldown} Seconds</TableBodyCell>
				</TableBodyRow>

				{#if openRow == i}
					<TableBodyRow id="drawer-row">
						<TableBodyCell colspan="3">
							<div
								class="overflow-auto whitespace-pre-wrap pt-6 pb-2 pl-6 pr-6 overflow-x-auto text-left border border-solid border-gray-300"
							>
								<div class="flex flex-col text-black">
									<p>Regex: {details?.regex}</p>
									<p>Permissions: {details?.permission}</p>

									{#if details?.description}
										<div><Markdown markdown={details?.description} /></div>
									{/if}
								</div>
							</div>
						</TableBodyCell>
					</TableBodyRow>
				{/if}
			{/each}
		</TableBody>
	</Table>
</section>

<style>
	:global(#cmd-sct) {
		max-width: 62rem;
		margin: 0 auto;
		padding: 0 1rem;
		padding-bottom: 1rem;
	}
</style>
