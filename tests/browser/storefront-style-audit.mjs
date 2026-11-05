// Shared browser assertion: blue text actions need a complete visible frame.
export async function assertFramedActions(page) {
    const result = await page.evaluate(() => {
        const actions = [...document.querySelectorAll('a[href],button.text-button,summary')].filter(node => {
            if (node.classList.contains('skip-link') || node.getAttribute('aria-hidden') === 'true' || !node.getClientRects().length) return false;
            const color = getComputedStyle(node).color.match(/[\d.]+/g)?.map(Number) || [];
            return color[2] >= 120 && color[2] > color[0] + 35 && color[2] > color[1] + 35;
        });
        return {
            blueActions: actions.length,
            unframed: actions.filter(node => {
                const css = getComputedStyle(node);
                return ['Top', 'Right', 'Bottom', 'Left'].some(side =>
                    Number.parseFloat(css[`border${side}Width`]) < 1 || ['none', 'hidden'].includes(css[`border${side}Style`]) || css[`border${side}Color`] === 'rgba(0, 0, 0, 0)');
            }).map(node => ({ label: (node.textContent || node.getAttribute('aria-label') || '').trim(), tag: node.tagName }))
        };
    });
    if (result.unframed.length) throw new Error(`Unframed blue actions: ${JSON.stringify(result.unframed)}`);
    return result;
}
