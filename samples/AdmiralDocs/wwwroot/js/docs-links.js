// Shared smoke pages can contain root-relative links. Keep those links inside
// this static host's base path, including links to pages outside the pilot.
document.addEventListener('DOMContentLoaded', () => {
    const root = document.getElementById('app');
    if (!root) return;
    const normalize = () => {
        root.querySelectorAll('#main-content a[href^="/components/"], #main-content a[href="/tables"]').forEach(link => {
            const path = link.getAttribute('href');
            if (path) link.setAttribute('href', new URL(path.slice(1), document.baseURI).pathname);
        });
    };
    const observer = new MutationObserver(normalize);
    observer.observe(root, { childList: true, subtree: true });
    normalize();
});
