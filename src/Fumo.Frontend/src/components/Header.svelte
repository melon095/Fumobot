<script>
	import Anchor from './Anchor.svelte';
</script>

<header class="bg-white shadow-md">
	<div class="container mx-auto flex justify-between items-center py-4 px-6">
		<div class="flex items-center space-x-4">
			<Anchor href="/commands/">List Commands</Anchor>
		</div>
		<div>
			<Anchor href="/ask-join/" className="bg-blue-500  text-white font-bold py-2 px-4 rounded"
				>Join Bot</Anchor
			>
		</div>
	</div>
</header>
