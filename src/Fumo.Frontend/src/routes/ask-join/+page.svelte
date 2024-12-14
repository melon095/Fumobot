<script lang="ts">
	import { goto } from '$app/navigation';
</script>

<div class="flex items-center justify-center min-h-screen bg-gray-100">
	<div class="bg-white p-8 rounded shadow-lg w-full max-w-md">
		<h2 class="text-xl font-bold mb-4">Click to Join</h2>
		<p class="mb-6">Click the button below to join the bot to your channel :)</p>
		<div class="flex justify-end">
			<button
				class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 focus:outline-none mr-2"
				on:click={() => goto('/Account/Join')}
			>
				Yes
			</button>
			<button
				class="bg-gray-300 text-gray-700 px-4 py-2 rounded hover:bg-gray-400 focus:outline-none"
				on:click={() => goto('/')}
			>
				No
			</button>
		</div>
	</div>
</div>
