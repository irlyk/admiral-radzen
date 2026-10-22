window.admiralDocs = {
    readCulture: () => localStorage.getItem('admiral-docs-culture') === 'en' ? 'en' : 'ru',
    setCulture: value => {
        if (value === 'en' || value === 'ru') localStorage.setItem('admiral-docs-culture', value);
    },
    isDark: () => localStorage.getItem('admiral-docs-theme') === 'admiral-dark',
    setTheme: value => {
        if (value !== 'admiral' && value !== 'admiral-dark') return;
        localStorage.setItem('admiral-docs-theme', value);
        document.documentElement.dataset.admiralTheme = value;
        const theme = document.getElementById('admiral-theme');
        if (theme) theme.href = new URL(`_content/Radzen.Admiral.Themes/css/${value}-base.css`, document.baseURI).href;
    }
};
window.admiralDocs.setTheme(window.admiralDocs.isDark() ? 'admiral-dark' : 'admiral');
