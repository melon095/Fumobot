<script lang="ts">
	import type { PageData } from './$types';

	export let data: PageData;

	$: message = data.props.message;
</script>

<div class="flex flex-col items-center h-screen">
	<h1 class="text-4xl font-bold text-red-600 mb-4">Error</h1>
	<p class="text-xl text-gray-700">{message}</p>
	<a href="/" class="mt-6 text-blue-500 hover:underline">Go back to home</a>
</div>
