<script lang="ts">
    import {parse} from 'marked';

    export let markdown: string;

    const sanitizeHtml = (html: string) => ({__html: html});

    // Only Promise<string> if extension is async.
    $: markedHtml = (parse(markdown) as string);
</script>

{@html markedHtml}