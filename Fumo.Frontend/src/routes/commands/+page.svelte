<script lang="ts">
    import { Table, TableBody, TableBodyCell, TableBodyRow, TableHead, TableHeadCell } from 'flowbite-svelte';
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

        const index = commands.findIndex(c => c.name == command);

        if (index == -1) return;

        details = await fetchDetails(command);
        openRow = index;
    });
</script>

<style>
    :global(#cmd-tbl) {
        max-width: 62rem;
        margin: 0 auto;
        padding: 0 1rem;
        padding-bottom: 1rem;
    }
</style>

<Table id="cmd-tbl" class="shadow-md">
    <TableHead>
        <TableHeadCell>Name</TableHeadCell>
        <TableHeadCell>Description</TableHeadCell>
        <TableHeadCell>Cooldown</TableHeadCell>
    </TableHead>
    <TableBody tableBodyClass="divide-y">
        {#each commands as command, i}
        <TableBodyRow on:click={() => toggleRow(i)} class="no-underline select-none cursor-pointer hover:bg-blue-100">
                <TableBodyCell>{command.name}</TableBodyCell>
                <TableBodyCell>{command.description}</TableBodyCell>
                <TableBodyCell>{command.cooldown}</TableBodyCell>
            </TableBodyRow>

            {#if openRow == i}
                <TableBodyRow id="drawer-row">
                    <TableBodyCell colspan="3">
                        <div class="overflow-auto whitespace-pre-wrap pt-0 pb-0 pl-6 pr-6 overflow-x-auto text-left border border-solid border-gray-300">
                            <div class="flex flex-col text-black">
                                <p>Regex: {details?.regex}</p>
                                <p>Permissions: {details?.permission}</p>
                    
                                {#if details?.description}
                                    <div><Markdown markdown={details?.description}/></div>
                                {/if}
                            </div>
                        </div>
                    </TableBodyCell>
                </TableBodyRow> 
            {/if}
        {/each}
    </TableBody>
</Table>
