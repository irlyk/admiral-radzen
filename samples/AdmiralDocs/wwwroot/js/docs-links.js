// Shared smoke pages can contain root-relative links. Keep those links inside
// this static host's base path, including the integration guide links.
document.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('app');
    if (!root) return;
    const normalize = () => {
        root.querySelectorAll('#main-content a[href^="/components/"], #main-content a[href="/tables"], #main-content a[href="/icons"], #main-content a[href="/regression/layout"]').forEach(link => {
            const path = link.getAttribute('href');
            if (path) link.setAttribute('href', new URL(path.slice(1), document.baseURI).pathname);
        });
    };
    const observer = new MutationObserver(normalize);
    observer.observe(root, { childList: true, subtree: true });
    normalize();
});
