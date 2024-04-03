<script>
	import { userStore } from '../stores/user';
	import Anchor from './Anchor.svelte';
</script>

<header class="bg-white shadow-md">
	<div class="container mx-auto flex justify-between items-center py-4 px-6">
		<div class="flex items-center space-x-4">
			<Anchor href="/commands/">List Commands</Anchor>
		</div>
		<div class="flex items-center space-x-4">
			{#if $userStore != null}
				<div>
					<Anchor href="/account/">My Account</Anchor>
					<Anchor href="/api/Account/Logout/">Logout</Anchor>
				</div>
			{:else}
				<Anchor href="/api/Account/Login/">Login</Anchor>
			{/if}
		</div>
	</div>
</header>
