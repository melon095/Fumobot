<script lang="ts">
	export let href: string;
	export let style: string | null = '';
</script>

<a {href} class="text-gray-800 hover:text-gray-600 font-semibold {style}"><slot /></a>
