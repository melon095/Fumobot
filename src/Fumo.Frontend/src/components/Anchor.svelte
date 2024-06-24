<script lang="ts">
	export let href: string;
	export let className: string | null = '';
</script>

<a {href} class="text-gray-800 hover:text-gray-600 font-semibold {className}"><slot /></a>
